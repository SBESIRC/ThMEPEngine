using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.Broadcast;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThBroadcastCmds
    {

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
                var laneLineEngine = new ThLaneRecognitionEngine();
                laneLineEngine.Recognize(acdb.Database, new Point3dCollection());
                // 暂时假设车道线绘制符合要求
                var lanes = laneLineEngine.Spaces.Select(o => o.Boundary).Cast<Line>().ToList();
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

                    //获取构建信息
                    var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acdb.Database, frame.Vertices());

                    //获取柱
                    var columns = allStructure.ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                    var objs = new DBObjectCollection();
                    columns.ForEach(x => objs.Add(x));
                    ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();

                    //获取剪力墙
                    var walls = allStructure.ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                    objs = new DBObjectCollection();
                    walls.ForEach(x => objs.Add(x));
                    thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();

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
    }
}
