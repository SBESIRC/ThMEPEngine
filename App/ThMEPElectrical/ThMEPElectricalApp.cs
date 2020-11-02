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
using ThCADCore.NTS;
using System;
using DotNetARX;
using Dreambuild.AutoCAD;

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
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                var polys = packageManager.DoMainBeamProfiles();
                DrawUtils.DrawProfile(polys.Polylines2Curves(), "MainBeamProfiles");
            }
        }

        [CommandMethod("TIANHUACAD", "THPL", CommandFlags.Modal)]
        public void ThParkingline()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
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
                    var objs = new DBObjectCollection();
                    var pLines = acdb.ModelSpace
                        .OfType<Curve>()
                        .Where(o => o.Layer == "AD-SIGN");
                    pLines.ForEach(x => objs.Add(x));

                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    var lanes = thCADCoreNTSSpatialIndex.SelectWindowPolygon(frame).Cast<Curve>().ToList();

                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateParkingLines(frame, lanes);

                    foreach (var line in parkingLines)
                    {
                        acdb.ModelSpace.Add(line.Clone() as Curve);
                    }
                }
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

        [CommandMethod("TIANHUACAD", "THCenter", CommandFlags.Modal)]
        public void THCenter()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entityId = ThPickTool.PickEntity("select");
                var poly = acadDatabase.Element<Polyline>(entityId);
                var regions = RegionTools.CreateRegion(new Curve[] { poly });
                var curves = new List<Curve>();
                foreach (var region in regions)
                {
                    var circle = new Circle(region.GetCentroid().Point3D(), Vector3d.ZAxis, 10);
                    curves.Add(circle);
                }

                DrawUtils.DrawProfile(curves, "curves");
            }
        }


        [CommandMethod("TIANHUACAD", "THABBPlace", CommandFlags.Modal)]
        public void ThProfilesPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBMultiPlace", CommandFlags.Modal)]
        public void ThMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoMultiWallMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THOBBRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoMainBeamRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBRect", CommandFlags.Modal)]
        public void ThABBRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoMainBeamABBRect();
            }
        }

        // 轴网
        [CommandMethod("TIANHUACAD", "THZY", CommandFlags.Modal)]
        public void THDoGridTestProfiles()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoGridTestProfiles();
            }
        }

        // 梁吊顶
        [CommandMethod("TIANHUACAD", "THBeamCeil", CommandFlags.Modal)]
        public void THNoBeamMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoGridBeamPlacePoints();
            }
        }

        // 楼层
        [CommandMethod("TIANHUACAD", "THNoBeamStorey", CommandFlags.Modal)]
        public void THNoBeamStorey()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoNoBeamPlacePoints();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDL", CommandFlags.Modal)]
        public void THMSABBMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(ThMEPElectricalService.Instance.Parameter);
                packageManager.DoMainSecondBeamPlacePoints();
            }
        }
    }
}
