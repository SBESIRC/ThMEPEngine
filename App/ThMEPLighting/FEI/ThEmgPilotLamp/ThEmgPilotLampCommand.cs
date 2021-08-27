using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.Colors;
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
            this.AddPolyLineIds = new List<ObjectId>();
        }
        public void Dispose(){}

        public List<ObjectId> AddPolyLineIds;
        private ThMEPOriginTransformer _originTransformer;
        private Dictionary<Polyline, List<Polyline>> _holeInfo;
        private double _cloudLineBufferDis = 800;

        public void InitData(ThMEPOriginTransformer originTransformer, Dictionary<Polyline, List<Polyline>> outPolylines) 
        {
            _originTransformer = originTransformer;
            _holeInfo = outPolylines;
        }
        public void Execute()
        {
            this.AddPolyLineIds.Clear();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                EmgPilotLampUtil.CreateLayer(ThMEPLightingCommon.REVCLOUD_LAYER, Color.FromColorIndex(ColorMethod.ByLayer,1));
                foreach (var pline in _holeInfo)
                {
                    //清除区域内的 已经生成的指示灯
                    CreateClearEmgLamp.ClearEFIExitPilotLamp(pline.Key, _originTransformer);
                    CreateClearEmgLamp.ClearExtractRevCloud(pline.Key, ThMEPLightingCommon.REVCLOUD_LAYER,_originTransformer,ThMEPLightingCommon.EMGPILOTREVCLOUD_CORLOR_INDEX);

                    //Ⅰ类线  车道线到出口线  -  图层：出口路径
                    //Ⅱ类线  计算或调整后的车道中心线  图层：主要疏散路径
                    //Ⅲ类线  计算或调整后的辅助疏散路径线 图层：辅助疏散路径
                    GetPrimitivesService primitivesService = new GetPrimitivesService(_originTransformer);
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
                    var res = emgLampIndicator.CalcLayout(this._isHostFirst);

                    //计算车道线中间的吊装云线表达
                    var tempHostLines = new List<Curve>();
                    exitLines.ForEach(c => tempHostLines.Add((Curve)c.Clone()));
                    assitHostLines.ForEach(c => tempHostLines.Add((Curve)c.Clone()));
                    var tempLaneLines = new List<Curve>();
                    mainLines.ForEach(c => tempLaneLines.Add((Curve)c.Clone()));
                    assitLines.ForEach(c => tempLaneLines.Add((Curve)c.Clone()));
                    var emgLaneLineMark = new EmgLaneLineMark(tempLaneLines, tempHostLines);
                    var markLines = emgLaneLineMark.MarkLines(_cloudLineBufferDis, res.Where(c=>c.isHoisting).Select(c=>c.linePoint).ToList());
                    var objs = new DBObjectCollection();
                    tempHostLines.ForEach(x => objs.Add(x));
                    tempLaneLines.ForEach(x => objs.Add(x));

                    foreach (var item in markLines)
                    {
                        var pl = (Curve)item.Clone();
                        pl.Layer = ThMEPLightingCommon.REVCLOUD_LAYER;
                        pl.ColorIndex = ThMEPLightingCommon.EMGPILOTREVCLOUD_CORLOR_INDEX;
                        _originTransformer.Reset(pl);
                        var plId = acdb.ModelSpace.Add(pl);
                        if (null != plId && plId.IsValid)
                            AddPolyLineIds.Add(plId);
                    }

                    //根据计算出的灯具信息，在相应的位置放置相应的灯具
                    
                    foreach (var item in res)
                    {
                        if (item == null)
                            continue;
                        Point3d sp = item.pointInOutSide;
                        Point3d ep = sp + item.direction.MultiplyBy(10);
                        var createPt = item.pointInOutSide;
                        _originTransformer.Reset(ref createPt);
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
                            createPt = createPt + item.directionSide.MultiplyBy((2.5* ThEmgLightService.Instance.BlockScale) / 2);
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
                        CreateClearEmgLamp.CreatePilotLamp(acdb.Database, createPt, createDir, blockName,item.isHoisting, new Dictionary<string, string>(), ThEmgLightService.Instance.BlockScale);
                    }
                }
            }
        }
    }
}
