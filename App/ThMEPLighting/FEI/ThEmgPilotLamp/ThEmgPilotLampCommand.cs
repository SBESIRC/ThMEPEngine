using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class ThEmgPilotLampCommand : IAcadCommand, IDisposable
    {
        private bool _isHostFirst = false;
        public ThEmgPilotLampCommand() 
        {
            //这里根据用户选项
            this._isHostFirst= ThEmgLightService.Instance.IsHostingLight;
        }
        public void Dispose(){}

        public void Execute()
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
                frameLst = frameLst.Where(c=>c.Area>10).Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    //清除区域内的 已经生成的指示灯
                    CreateClearEmgLamp.ClearEFIExitPilotLamp(pline.Key, originTransformer);

                    //Ⅰ类线  车道线到出口线  -  图层：出口路径
                    //Ⅱ类线  计算或调整后的车道中心线  图层：主要疏散路径
                    //Ⅲ类线  计算或调整后的辅助疏散路径线 图层：辅助疏散路径
                    GetPrimitivesService primitivesService = new GetPrimitivesService(originTransformer);
                    //获取疏散路径(Ⅰ类线)
                    var exitLines = primitivesService.GetMainEvacuate(pline.Key, ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                    if ((exitLines == null || exitLines.Count < 1))
                    {
                        //没有疏散路径，不进行处理
                        continue;
                    }
                    //获取车道线信息（Ⅱ类线）
                    var mainLines = primitivesService.GetMainEvacuate(pline.Key, ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);

                    //获取次要疏散路径（Ⅲ类线-壁装）
                    var assitLines = primitivesService.GetMainEvacuate(pline.Key, ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                    //获取次要疏散路径（Ⅲ类线-壁装）
                    var assitHostLines = primitivesService.GetMainEvacuate(pline.Key, ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);

                    //获取出口块信息
                    var enterBlcok = primitivesService.GetEvacuationExitBlock(pline.Key);

                    List<Curve> laneLines = new List<Curve>();
                    mainLines.ForEach(c => laneLines.Add(c));
                    assitLines.ForEach(c => laneLines.Add(c));
                    assitHostLines.ForEach(c => laneLines.Add(c));
                    //step1 根据线，出口信息计算，所有拐点到最近出口的位置
                    EmgPilotLampLineNode lampLineNode = new EmgPilotLampLineNode(laneLines, exitLines, enterBlcok);

                    //获取墙柱信息
                    List<Polyline> columns = new List<Polyline>();
                    List<Polyline> walls = new List<Polyline>();
                    if(!ThEmgLightService.Instance.IsHostingLight)
                        primitivesService.GetStructureInfo(pline.Key.Buffer(40)[0] as Polyline, out columns, out walls);
                    //根据这些线信息，拐点到出口的数据进行计算布置的点信息
                    IndicatorLight indicator = new IndicatorLight();
                    assitLines.ForEach(c => indicator.assistLines.Add(c));
                    exitLines.ForEach(c => indicator.exitLines.Add(c));
                    indicator.allLines.AddRange(lampLineNode.dijkstraLines);
                    indicator.allNodes.AddRange(lampLineNode.allNodes);
                    indicator.allNodeRoutes.AddRange(lampLineNode.cacheNodeRoutes);
                    mainLines.ForEach(c => indicator.mainLines.Add(c));
                    assitHostLines.ForEach(c => indicator.assistHostLines.Add(c));
                    EmgLampIndicatorLight emgLampIndicator = new EmgLampIndicatorLight(pline.Key,columns, walls, indicator);

                    //根据计算出的灯具信息，在相应的位置放置相应的灯具
                    var res = emgLampIndicator.CalcLayout(this._isHostFirst);
                    foreach (var item in res)
                    {
                        if (item == null)
                            continue;
                        Point3d sp = item.pointInOutSide;
                        Point3d ep = sp + item.direction.MultiplyBy(10);
                        var createPt = item.pointInOutSide;
                        originTransformer.Reset(ref createPt);
                        Vector3d createDir = item.directionSide.CrossProduct(Vector3d.ZAxis);
                        if (createDir.DotProduct(item.direction) < 0)
                            createDir = createDir.Negate();
                        string blockName = ThMEPLightingCommon.PILOTLAMP_WALL_ONEWAY_SINGLESIDE;
                        if (item.isHoisting)
                        {
                            //吊装
                            if (item.isTwoSide)
                            {
                                blockName = ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_DOUBLESIDE;
                                switch (item.endType)
                                {
                                    case 140://壁装 E/N
                                    case 141://吊装 E/N
                                        blockName = ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_DOUBLESIDE;
                                        break;
                                    default:
                                        blockName = ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_DOUBLESIDE;
                                        break;
                                }
                            }
                            else 
                            {
                                blockName = ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_SINGLESIDE;
                                switch (item.endType)
                                {
                                    case 140://壁装 E/N
                                    case 141://吊装 E/N
                                        blockName = ThMEPLightingCommon.PILOTLAMP_HOST_TWOWAY_SINGLESIDE;
                                        break;
                                    default:
                                        blockName = ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_SINGLESIDE;
                                        break;
                                }
                            }
                        }
                        else 
                        {
                            //壁装
                            createPt = createPt + item.directionSide.MultiplyBy(250 / 2);
                            switch (item.endType) 
                            {
                                case 140://壁装 E/N
                                case 141://吊装 E/N
                                    blockName = ThMEPLightingCommon.PILOTLAMP_WALL_TWOWAY_SINGLESIDE;
                                    break;
                                default:
                                    blockName = ThMEPLightingCommon.PILOTLAMP_WALL_ONEWAY_SINGLESIDE;
                                    break;
                            }
                        }
                        CreateClearEmgLamp.LoadBlockToDocument(acdb.Database);
                        CreateClearEmgLamp.CreatePilotLamp(acdb.Database, createPt, createDir, blockName,item.isHoisting, new Dictionary<string, string>());
                    }
                }
            }
        }
        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer,Point3d sp,Point3d ep) 
        {
            Line line = new Line(sp, ep);
            Vector3d dir = line.LineDirection();
            SPointArrow(acdb, originTransformer, sp, dir);
        }
        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp, Vector3d dir)
        {
            Vector3d normal = new Vector3d(0, 0, 1);
            Point3d tempPt = sp + dir.MultiplyBy(100);
            Vector3d x = -dir.RotateBy(Math.PI / 6, normal);
            Point3d tempEp = tempPt + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            x = -dir.RotateBy(-Math.PI / 6, normal);
            tempEp = tempPt + x.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);
        }
        void PointToView(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp)
        {
            Vector3d x = new Vector3d(1, 0, 0);
            Vector3d y = new Vector3d(0, 1, 0);
            Point3d tempPt = sp - x.MultiplyBy(100);
            Point3d tempEp = sp + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            tempPt = sp - y.MultiplyBy(100);
            tempEp = sp + y.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);

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
            return frameLst.Cast<Polyline>().ToList();
        }
    }
}
