using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPElectrical.CAD;
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
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;

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
                var packageManager = new PackageManager(Parameter);
                var polys = packageManager.DoMainBeamProfiles();
                DrawUtils.DrawProfile(polys.Polylines2Curves(), "MainBeamProfiles");
            }
        }

        private PlaceParameter Parameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.Parameter == null)
                {
                    ThMEPElectricalService.Instance.Parameter = new PlaceParameter();
                }
                return ThMEPElectricalService.Instance.Parameter;
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
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBMultiPlace", CommandFlags.Modal)]
        public void ThMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMultiWallMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THOBBRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBRect", CommandFlags.Modal)]
        public void ThABBRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamABBRect();
            }
        }

        // 轴网
        [CommandMethod("TIANHUACAD", "THZY", CommandFlags.Modal)]
        public void THDoGridTestProfiles()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoGridTestProfiles();
            }
        }

        // 梁吊顶
        [CommandMethod("TIANHUACAD", "THFDCP", CommandFlags.Modal)]
        public void THNoBeamMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoGridBeamPlacePointsWithUcs();
            }
        }

        // 楼层
        [CommandMethod("TIANHUACAD", "THFDFS", CommandFlags.Modal)]
        public void THNoBeamStorey()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoNoBeamPlacePoints();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDLBack", CommandFlags.Modal)]
        public void THMSABBMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainSecondBeamPlacePoints();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDL", CommandFlags.Modal)]
        public void THMSABBMultiPlaceUcs()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainSecondBeamPlacePointsWithUcs();
            }
        }

        [CommandMethod("TIANHUACAD", "THUCSCOMPASS", CommandFlags.Modal)]
        public void ThUcsCompass()
        {
            while (true)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    Active.Database.ImportCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME);
                    var objId = Active.Database.InsertCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME,
                        null);
                    var ucs2Wcs = Active.Editor.UCS2WCS();
                    var compass = acadDatabase.Element<BlockReference>(objId, true);
                    compass.TransformBy(ucs2Wcs);
                    var jig = new ThCompassDrawJig(Point3d.Origin.TransformBy(ucs2Wcs));
                    jig.AddEntity(compass);
                    PromptResult pr = Active.Editor.Drag(jig);
                    if (pr.Status != PromptStatus.OK)
                    {
                        compass.Erase();
                        break;
                    }
                    jig.TransformEntities();
                }
            }
        }
    }
}
