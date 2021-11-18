using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using DotNetARX;
using NetTopologySuite.Geometries;

using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Sensorlayout;
using NetTopologySuite.Algorithm;

namespace ThMEPElectrical.AlarmSensorLayout.Test
{
    class TestCmd
    {
        //public BeamSensorLayout.BeamSensorLayout sensorLayout;

        public BeamSensorOpt sensorOpt;
        public List<ObjectId> lineId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> pointId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> UCS_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> blind_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> detect_List { get; set; } = new List<ObjectId>();
        public Point3d center { get; set; }
        public double angle { get; set; }

        [CommandMethod("TIANHUACAD", "THASLT", CommandFlags.Modal)]
        public void THASLT()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //选择区域
                Active.Editor.WriteLine("\n请选择所有MPolygon形式的可布置区域");
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new ObjectIdCollection();
                objs = result.Value.GetObjectIds().ToObjectIdCollection();
                foreach (ObjectId objId in objs)
                {
                    var frame = acadDatabase.Element<MPolygon>(objId);
                    Polygon polygon = frame.ToNTSPolygon();
                    Polyline shell = polygon.Shell.ToDbPolyline();
                    shell.Layer = "AI-可布区域";
                    acadDatabase.ModelSpace.Add(shell);
                    foreach (var hole in polygon.Holes)
                    {
                        var dbhole = hole.ToDbPolyline();
                        dbhole.Layer = "AI-可布置区域洞";
                        acadDatabase.ModelSpace.Add(dbhole);
                    }
                }
                ////选择点
                //var per = Active.Editor.GetEntity("请选择Mpolygon");
                //if (per.Status != PromptStatus.OK)
                //{
                //    return;
                //}
            }
        }

        [CommandMethod("TIANHUACAD", "THASLR", CommandFlags.Modal)]
        public void THASLR()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
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

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frame = frameList.OrderByDescending(x => x.Area).First();

                var holeList = getPoly(frame, "AI-房间框线", transformer, true);

                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);

                var wallList = getPoly(frame, "AI-墙", transformer, false);

                var columnList = getPoly(frame, "AI-柱", transformer, false);

                var layoutHoleList = getPoly(frame, "AI-可布置区域洞", transformer, false);

                List<MPolygon> layouts = new List<MPolygon>();
                foreach (var layout in layoutList)
                {
                    Polygon polygon = layout.ToNTSPolygon();
                    foreach (var layouthole in layoutHoleList)
                        polygon = polygon.Difference(layouthole.ToNTSPolygon()) as Polygon;
                    layouts.Add(polygon.ToDbMPolygon());
                }

                //var layoutList = getMPoly(frame, "AI-可布区域", transformer, false);//AI-可布区域

                var rst = Active.Editor.GetDouble(new PromptDoubleOptions("Input protection radius:"));
                if (rst.Status != PromptStatus.OK)
                    return;
                var radius = rst.Value;

                var layoutCmd = new AlarmSensorLayoutCmd();

                //区域分割
                SpaceDivider spaceDivider = new SpaceDivider();
                spaceDivider.Compute(frame, layouts);
                InputArea input_Area = new InputArea(frame, layouts, holeList, wallList, columnList, null, null, spaceDivider.UCSs);
                //输入参数
                var equipmentParameter = new EquipmentParameter(radius, BlindType.CoverArea);
                //初始化布点引擎
                sensorOpt = new BeamSensorOpt(input_Area, equipmentParameter);
                sensorOpt.Calculate();
                //输出参数
                var blinds = sensorOpt.Blinds;
                var layoutPoints = sensorOpt.PlacePoints;
                ShowPoints(layoutPoints);
                ShowBlind(blinds);

                //layoutCmd.frame = frame;
                //layoutCmd.holeList = holeList;
                //layoutCmd.layoutList = layouts;
                //layoutCmd.wallList = wallList;
                //layoutCmd.protectRadius = radius;
                //layoutCmd.equipmentType = BlindType.VisibleArea;
                //layoutCmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THASLM", CommandFlags.Modal)]
        public void THASLM()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
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

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frame = frameList.OrderByDescending(x => x.Area).First();

                var holeList = getPoly(frame, "AI-房间框线", transformer, true);

                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);

                var wallList = getPoly(frame, "AI-墙", transformer, false);

                var columnList = getPoly(frame, "AI-柱", transformer, false);

                var layoutHoleList = getPoly(frame, "AI-可布置区域洞", transformer, false);

                List<MPolygon> layouts = new List<MPolygon>();
                foreach (var layout in layoutList)
                {
                    Polygon polygon = layout.ToNTSPolygon();
                    foreach (var layouthole in layoutHoleList)
                        polygon = polygon.Difference(layouthole.ToNTSPolygon()) as Polygon;
                    layouts.Add(polygon.ToDbMPolygon());
                }

                //区域分割
                SpaceDivider spaceDivider = new SpaceDivider();
                spaceDivider.Compute(frame, layouts);
                //输入参数
                InputArea input_Area = new InputArea(frame, layouts, holeList, wallList, columnList, null, null, spaceDivider.UCSs);
                var equipmentParameter = new EquipmentParameter(6700, BlindType.CoverArea);

                //初始化布点引擎
                sensorOpt = new BeamSensorOpt(input_Area, equipmentParameter);

                var dbroom = sensorOpt.room.ToDbMPolygon();
                dbroom.ColorIndex = 5;
                acadDatabase.ModelSpace.Add(dbroom);

                foreach (var hole in holeList)
                {
                    var dbhole = hole.ToNTSPolygon().ToDbMPolygon();
                    dbhole.ColorIndex = 4;
                    acadDatabase.ModelSpace.Add(dbhole);
                }
            }
        }

        private void ShowPoints(List<Point3d> layoutPoints)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var p in layoutPoints)
                {
                    var circle = new Circle(p, Vector3d.ZAxis, 100);
                    circle.ColorIndex = 4;
                    var id = acadDatabase.ModelSpace.Add(circle);
                    pointId_list.Add(id);
                }
            }
        }

        private void ShowBlind(List<Polyline> blinds)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var blind in blinds)
                {
                    blind.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(blind);
                }
            }
        }

        private static List<Polyline> getPoly(Polyline frame, string sLayer, ThMEPOriginTransformer transformer, bool onlyContains)
        {

            var layoutArea = ExtractPolyline(frame, sLayer, transformer, onlyContains);
            var layoutList = layoutArea.Select(x => x.Value).ToList();

            return layoutList;

        }
        private static Dictionary<Polyline, Polyline> ExtractPolyline(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer, bool onlyContain)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<Polyline>()
                      .Where(o => o.Layer == LayerName);

                List<Polyline> lineList = line.Select(x => x.WashClone() as Polyline).ToList();

                var plInFrame = new Dictionary<Polyline, Polyline>();

                foreach (Polyline pl in lineList)
                {
                    if (pl != null)
                    {
                        var plTrans = pl.Clone() as Polyline;

                        transformer.Transform(plTrans);
                        plInFrame.Add(pl, plTrans);
                    }
                }
                if (onlyContain == false)
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value) || bufferFrame.Intersects(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }


                return plInFrame;
            }
        }

        private static List<MPolygon> getMPoly(Polyline frame, string sLayer, ThMEPOriginTransformer transformer, bool onlyContains)
        {

            var layoutArea = ExtractMPolygon(frame, sLayer, transformer, onlyContains);
            var layoutList = layoutArea.Select(x => x.Value).ToList();

            return layoutList;

        }
        private static Dictionary<MPolygon, MPolygon> ExtractMPolygon(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer, bool onlyContain)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<MPolygon>()
                      .Where(o => o.Layer == LayerName);

                List<MPolygon> lineList = line.Select(x => x.Clone() as MPolygon).ToList();

                var mplInFrame = new Dictionary<MPolygon, MPolygon>();

                foreach (MPolygon mpl in lineList)
                {
                    if (mpl != null)
                    {
                        var mplTrans = mpl.Clone() as MPolygon;

                        transformer.Transform(mplTrans);
                        mplInFrame.Add(mpl, mplTrans);
                    }
                }
                if (onlyContain == false)
                {
                    mplInFrame = mplInFrame.Where(o => bufferFrame.Contains(o.Value.Shell()) || bufferFrame.Intersects(o.Value.Shell())).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    mplInFrame = mplInFrame.Where(o => bufferFrame.Contains(o.Value.Shell())).ToDictionary(x => x.Key, x => x.Value);
                }


                return mplInFrame;
            }
        }

        private static Polyline processFrame(Polyline frame, ThMEPOriginTransformer transformer)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            transformer.Transform(frameClone);
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;
        }

        [CommandMethod("TIANHUACAD", "THAreaGridLayoutTest", CommandFlags.Modal)]
        public void THAreaGridLayoutTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = FireAlarm.ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { FireAlarm.ThFaCommon.BlkName_Smoke, FireAlarm.ThFaCommon.BlkName_Heat, FireAlarm.ThFaCommon.BlkName_Smoke_ExplosionProf, FireAlarm.ThFaCommon.BlkName_Heat_ExplosionProf };
                var avoidBlkName = FireAlarm.ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var pts = FireAlarm.Service.ThFireAlarmUtils.GetFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                //保护半径
                var rst = Active.Editor.GetDouble(new PromptDoubleOptions("Input protection radius:"));
                if (rst.Status != PromptStatus.OK)
                    return;
                var radius = rst.Value;

                //提取梁（对可布区域有影响。默认为true）
                var _referBeam = true;
                //图块比例默认100
                var _scale = 100;
                //提取数据
                var geos = FireAlarm.Service.ThFireAlarmUtils.GetSmokeData(pts, extractBlkList, _referBeam,100);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = FireAlarm.Service.ThFireAlarmUtils.TransformToOrig(pts, geos);
                ////不转回原点
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

                var dataQuery = new FireAlarmSmokeHeat.Data.ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.AnalysisHoles();

                //墙，柱，可布区域，避让分配到房间框线
                dataQuery.ClassifyData();

                //扩大避让区域防止最终块重叠
                var priorityExtend = FireAlarmSmokeHeat.Service.ThFaAreaLayoutParamterCalculationService.GetPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.ExtendPriority(priorityExtend);

                //debug 打图纸用
                foreach (var frame in dataQuery.FrameList)
                {
                    FireAlarm.Service.DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                    FireAlarm.Service.DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                    FireAlarm.Service.DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
                }

                for (int i = 0; i < dataQuery.FrameList.Count; i++)
                {
                    var frame = dataQuery.FrameList[i];

                    var layoutCmd = new AlarmSensorLayoutCmd();
                    layoutCmd.frame = frame;
                    layoutCmd.holeList = dataQuery.FrameHoleList[frame];
                    layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
                    layoutCmd.wallList = dataQuery.FrameWallList[frame];
                    layoutCmd.columns = dataQuery.FrameColumnList[frame];
                    layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
                    layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
                    layoutCmd.protectRadius = radius;
                    layoutCmd.equipmentType = BlindType.CoverArea;

                    layoutCmd.Execute();
                }
            }
        }
    }
}