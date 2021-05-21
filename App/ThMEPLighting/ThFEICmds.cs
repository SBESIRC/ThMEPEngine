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
using ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding;
using ThMEPLighting.DSFEL;
using ThMEPLighting.DSFEL.Service;
using ThMEPLighting.FEI;
using ThMEPLighting.FEI.BFSAlgorithm;
using ThMEPLighting.FEI.EvacuationPath;
using ThMEPLighting.FEI.PrintEntity;
using ThMEPLighting.FEI.Service;
using ThMEPLighting.FEI.ThEmgPilotLamp;

namespace ThMEPLighting
{
    public class ThFEICmds
    {
        [CommandMethod("TIANHUACAD", "THSSLJ", CommandFlags.Modal)]
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
                    //清除原有构建
                    ClearComponentService.ClearLines(pline.Key, originTransformer);

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

        [CommandMethod("TIANHUACAD", "THDSFEL", CommandFlags.Modal)]
        public void ThDSFEI()
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
                foreach (var pline in plines)
                {
                    DSFELGetPrimitivesService dsFELGetPrimitivesService = new DSFELGetPrimitivesService(originTransformer);
                    //获取房间
                    var rooms = dsFELGetPrimitivesService.GetUsefulRooms(pline);

                    //获取门 
                    var doors = dsFELGetPrimitivesService.GetDoor(pline);

                    //布置
                    LayoutService layoutService = new LayoutService();
                    layoutService.LayoutFELService(rooms, doors);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THSSZSDBZ", CommandFlags.Modal)]
        public void ThMEGLBZ()
        {
            var lampLight = new ThEmgPilotLampCommand();
            lampLight.Execute();
        }

        [CommandMethod("TIANHUACAD", "THTest", CommandFlags.Modal)]
        public void ThTest()
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
                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);

                PromptSelectionOptions sOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var sResult = Active.Editor.GetSelection(sOptions);
                if (sResult.Status != PromptStatus.OK)
                {
                    return;
                }
                Point3d sPt = acdb.Element<Circle>(sResult.Value.GetObjectIds().First()).Center;
                var eResult = Active.Editor.GetSelection(sOptions);
                if (eResult.Status != PromptStatus.OK)
                {
                    return;
                }
                Point3d ePt = acdb.Element<Circle>(eResult.Value.GetObjectIds().First()).Center;

                foreach (var frame in holeInfo)
                {
                    RoutePlanner routePlanner = new RoutePlanner(frame.Key, ePt);
                    routePlanner.SetObstacle(frame.Value);
                    Polyline resPoly = routePlanner.Plan(sPt);
                    acdb.ModelSpace.Add(resPoly);
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
