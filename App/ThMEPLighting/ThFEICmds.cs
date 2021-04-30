using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPLighting.FEI;
using ThMEPLighting.FEI.BFSAlgorithm;
using ThMEPLighting.FEI.EvacuationPath;
using ThMEPLighting.FEI.PrintEntity;
using ThMEPLighting.FEI.ThEmgPilotLamp;

namespace ThMEPLighting
{
    public class ThFEICmds
    {
        [CommandMethod("TIANHUACAD", "THFEI", CommandFlags.Modal)]
        public void ThFEI()
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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                var pt = frameLst.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frameLst = frameLst.Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    GetPrimitivesService primitivesService = new GetPrimitivesService(originTransformer);

                    //获取车道线信息
                    var xLanes = primitivesService.GetLanes(pline.Key, out List<List<Line>> yLines);
                    if (xLanes.Count == 0 && yLines.Count == 0)
                    {
                        continue;
                    }

                    //获取墙柱信息
                    primitivesService.GetStructureInfo(pline.Key, out List<Polyline> columns, out List<Polyline> walls);

                    //计算洞口
                    List<Polyline> holes = new List<Polyline>(pline.Value);
                    holes.AddRange(columns);
                    holes.AddRange(walls);

                    //获取出入口图块
                    var enterBlcok = primitivesService.GetEvacuationExitBlock(pline.Key);

                    //规划路径
                    ExtendLinesService extendLines = new ExtendLinesService();
                    var paths = extendLines.CreateExtendLines(xLanes, yLines, enterBlcok, pline.Key, holes);

                    //打印路径
                    PrintService printService = new PrintService();
                    var allLanes = xLanes.SelectMany(x => x.Select(y => y)).ToList();
                    allLanes.AddRange(yLines.SelectMany(x => x.Select(y => y)));
                    printService.PrintPath(paths, allLanes, originTransformer);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THSSZSDBZ", CommandFlags.Modal)]
        public void ThMEGLBZ()
        {
            var lampLight = new ThEmgPilotLampCommand(false);
            lampLight.Execute();
        }
        [CommandMethod("TIANHUACAD", "THSSZSDDZ", CommandFlags.Modal)]
        public void ThMEGLDZ()
        {
            var lampLight = new ThEmgPilotLampCommand(true);
            lampLight.Execute();
        }


        [CommandMethod("TIANHUACAD", "thtestAS", CommandFlags.Modal)]
        public void test()
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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择起点和终点",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                // 获取起点
                var sResult = Active.Editor.GetSelection(sOptions);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var sp = (acdb.Element<Circle>(sResult.Value.GetObjectIds().First()) as Circle).Center;
                // 获取起点
                var eResult = Active.Editor.GetSelection(sOptions);
                if (eResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var ep = (acdb.Element<Circle>(eResult.Value.GetObjectIds().First()) as Circle).Center;

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    //A*寻路
                    AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(pline.Key, Vector3d.XAxis, ep);
                    aStarRoute.SetObstacle(pline.Value);
                    var res = aStarRoute.Plan(sp);
                    acdb.ModelSpace.Add(res);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "thtestBfs", CommandFlags.Modal)]
        public void testBfs()
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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择起点和终点",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                // 获取起点
                var sResult = Active.Editor.GetSelection(sOptions);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var sp = (acdb.Element<Circle>(sResult.Value.GetObjectIds().First()) as Circle).Center;
                // 获取终点
                PromptSelectionOptions eOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择起点和终点",
                    RejectObjectsOnLockedLayers = true,
                };
                var eResult = Active.Editor.GetSelection(eOptions);
                if (eResult.Status != PromptStatus.OK)
                {
                    return;
                }
                //获取外包框
                List<Line> lineLst = new List<Line>();
                foreach (ObjectId obj in eResult.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Line>(obj);
                    lineLst.Add(frame.Clone() as Line);
                }

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    //BFS寻路
                    BFSPathPlaner pathPlaner = new BFSPathPlaner(400);
                    var closetLine = pathPlaner.FindingClosetLine(sp, lineLst, pline.Key);
                    acdb.ModelSpace.Add(closetLine);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THTESTTOCURVEAS", CommandFlags.Modal)]
        public void testToLine()
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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择起点和终线",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                // 获取起点
                var sResult = Active.Editor.GetSelection(sOptions);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var sp = acdb.Element<Circle>(sResult.Value.GetObjectIds().First()).Center;

                // 获取终线
                var eResult = Active.Editor.GetSelection(sOptions);
                if (eResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var ep = acdb.Element<Line>(eResult.Value.GetObjectIds().First());

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    //A*寻路
                    AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(pline.Key, (ep.EndPoint - ep.StartPoint).GetNormal(), ep);
                    aStarRoute.SetObstacle(pline.Value);
                    var res = aStarRoute.Plan(sp);
                    acdb.ModelSpace.Add(res);
                }
            }
        }


        [CommandMethod("TIANHUACAD", "THTESTDIVMERGE", CommandFlags.Modal)]
        public void THTESTDivMerge()
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

                //获取外包框
                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                ThRegionDivisionService thRegionDivision = new ThRegionDivisionService();
                //thRegionDivision.tol = 0.01;
                var resPolys = thRegionDivision.DivisionRegion(frameLst.First());
                resPolys = thRegionDivision.MergePolygon(resPolys);
                foreach (var item in resPolys)
                {
                    acdb.ModelSpace.Add(item);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THTESTDIV", CommandFlags.Modal)]
        public void THTESTDiv()
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

                //获取外包框
                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                ThRegionDivisionService thRegionDivision = new ThRegionDivisionService();
                var resPolys = thRegionDivision.DivisionRegion(frameLst.First());
                foreach (var item in resPolys)
                {
                    acdb.ModelSpace.Add(item);
                }
            }
        }


        [CommandMethod("TIANHUACAD", "thAstarDiv", CommandFlags.Modal)]
        public void testFindingPathWithDivRegion()
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

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择起点和终点",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                // 获取起点
                var sResult = Active.Editor.GetSelection(sOptions);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var sp = (acdb.Element<Circle>(sResult.Value.GetObjectIds().First()) as Circle).Center;
                // 获取起点
                var eResult = Active.Editor.GetSelection(sOptions);
                if (eResult.Status != PromptStatus.OK)
                {
                    return;
                }
                var ep = (acdb.Element<Circle>(eResult.Value.GetObjectIds().First()) as Circle).Center;

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    ThFindingPathByRegion thFindingPathByRegion = new ThFindingPathByRegion();
                    var res = thFindingPathByRegion.FindingPath(pline.Key, sp, ep, pline.Value);
                    foreach (var item in res)
                    {
                        acdb.ModelSpace.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);
                firFrame = firFrame.DPSimplify(1);

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            //var polygonInfos = NoUserCoordinateWorker.MakeNoUserCoordinateWorker(frameLst);
            //List<Polyline> resPLines = new List<Polyline>();
            //foreach (var pInfo in frameLst)
            //{
            //    resPLines.Add(pInfo.ExternalProfile);
            //    resPLines.AddRange(pInfo.InnerProfiles);
            //}

            return frameLst.Cast<Polyline>().ToList();
        }
    }
}
