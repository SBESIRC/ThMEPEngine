using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPElectrical.Core;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using NFox.Cad;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.Broadcast;
using System.Linq;
using ThCADExtension;

namespace ThMEPElectrical
{
    public class ThMEPElectricalApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }


        [CommandMethod("TIANHUACAD", "THMainBeamRegion", CommandFlags.Modal)]
        public void ThBeamRegion()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                var polys = packageManager.DoMainBeamProfiles();
                DrawUtils.DrawProfile(polys. Polylines2Curves(), "MainBeamProfiles");
            }
        }

        [CommandMethod("TIANHUACAD", "THFBS", CommandFlags.Modal)]
        public void ThBroadcast()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取车道线
                var laneLineEngine = new ThLaneLineRecognitionEngine();
                laneLineEngine.Recognize(acdb.Database);
                // 暂时假设车道线绘制符合要求
                var lanes = laneLineEngine.Lanes.Cast<Line>().ToList();
                if (lanes.Count == 0)
                {
                    return;
                }

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);

                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateParkingLines(frame, lanes, out List<List<Line>> otherPLines);

                    var columnEngine = new ThColumnRecognitionEngine();
                    columnEngine.Recognize(acdb.Database, frame.Vertices());
                    var columPoly = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                    ColumnService columnService = new ColumnService();
                    columnService.HandleColumns(parkingLines, otherPLines, columPoly, 
                        out Dictionary<List<Line>, List<ColumnModel>> mainColumns, 
                        out Dictionary<List<Line>, List<ColumnModel>> otherColumns);

                    LayoutService layoutService = new LayoutService();
                    var layoutCols = layoutService.LayoutBraodcast(frame, mainColumns, otherColumns);

                    InsertBroadcastService.InsertSprayBlock(layoutCols);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THANGLE", CommandFlags.Modal)]
        public void THANGLE()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entityId = ThPickTool.PickEntity("select");
                var circle = acadDatabase.Element<Circle>(entityId);
                var vec = (circle.Center - Point3d.Origin).GetNormal();
                var plane = new Plane(new Point3d(10e6, 10e6, 0), Vector3d.ZAxis);
                var angle = vec.AngleOnPlane(plane);
            }
        }

        [CommandMethod("TIANHUACAD", "THABBPlace", CommandFlags.Modal)]
        public void ThProfilesPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBMultiPlace", CommandFlags.Modal)]
        public void ThMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMultiWallMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THOBBRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBRect", CommandFlags.Modal)]
        public void ThABBRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamABBRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDL", CommandFlags.Modal)]
        public void THMSABBMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainSecondBeamPlacePoints();
            }
        }
    }
}
