using System;
using NFox.Cad;
using Linq2Acad;
using QuikGraph;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAFASGraphEngine
    {
        private bool IsJF = false;//是否是大屋面

        private Dictionary<Entity, Entity> GlobleNTSMappingDic;
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public List<Point3d> CrossAlarms { get; set; }
        private ThCADCoreNTSSpatialIndex FireCompartmentIndex { get; set; }

        public Dictionary<Point3d, List<ThAlarmControlWireCircuitModel>> GraphsDic { get; set; }

        private Database Database { get; set; }

        /// <summary>
        /// 全部数据集合
        /// </summary>
        private List<Entity> DataCollection { get; set; }

        /// <summary>
        /// 短路隔离器集合
        /// </summary>
        private List<Entity> SICollection { get; set; }

        /// <summary>
        /// 已捕捉到的数据集合
        /// </summary>
        private List<Entity> CacheDataCollection { get; set; }

        /// <summary>
        /// 已捕捉到的节点的数据集合
        /// </summary>
        private List<Entity> CacheSINodeCollection { get; set; }

        private Dictionary<Entity, List<KeyValuePair<string, string>>> globleBlockAttInfoDic { get; set; }

        //短路隔离器规则
        private Func<Entity, bool> SIRule = (e) =>
        {
            return e is BlockReference blk && blk.Name == "E-BFAS540";
        };
        //配电箱规则
        private Func<Entity, bool> FASRule = (e) =>
        {
            return e is BlockReference blk && blk.Name == "E-BFAS010";
        };
        //桥架规则
        private Func<Entity, bool> CMTBRule = (e) =>
        {
            return e is Curve cur && cur.Layer.Contains("CMTB");
        };

        public ThAFASGraphEngine(Database db, List<Entity> Datas, Dictionary<Entity, List<KeyValuePair<string, string>>> blockAttInfoDic, IEnumerable<Entity> fireCompartments, bool isJF = false)
        {
            GraphsDic = new Dictionary<Point3d, List<ThAlarmControlWireCircuitModel>>();
            CrossAlarms = new List<Point3d>();
            GlobleNTSMappingDic = new Dictionary<Entity, Entity>();
            DataCollection = Datas;
            SICollection = Datas.Where(o=>o is BlockReference blk && blk.AttributeCollection.Cast<AttributeReference>().Any(x => x.TextString == "SI")).ToList();
            Datas.ForEach(e =>
            {
                if (e is BlockReference br)
                {
                    GlobleNTSMappingDic.Add(ThMPolygonTool.CreateMPolygon(db.GetBlockReferenceOBB(br)), br);
                }
                else
                {
                    GlobleNTSMappingDic.Add(e.Clone() as Entity, e);
                }
            });
            SpatialIndex = new ThCADCoreNTSSpatialIndex(GlobleNTSMappingDic.Keys.ToCollection());
            FireCompartmentIndex = new ThCADCoreNTSSpatialIndex(fireCompartments.ToCollection());
            CacheDataCollection = new List<Entity>();
            CacheSINodeCollection = new List<Entity>();
            this.Database = db;
            this.globleBlockAttInfoDic = blockAttInfoDic;
            this.IsJF = isJF;
            SIRule = (e) =>
            {
                return (e is BlockReference blk && blk.Name == "E-BFAS540") || SICollection.Contains(e);
            }; 
        }

        /// <summary>
        /// 初始化图
        /// </summary>
        public void InitGraph()
        {
            //业务逻辑 优先级: 配电箱>桥架>短路隔离器
            List<Entity> StartingSet_1 = DataCollection.Where(FASRule).ToList();
            foreach (Entity StartEntity in StartingSet_1)
            {
                FindGraph(StartEntity);
            }

            List<Entity> StartingSet_2 = DataCollection.Where(CMTBRule).ToList();
            foreach (Entity StartEntity in StartingSet_2)
            {
                FindGraph(StartEntity);
            }

            List<Entity> StartingSet_3 = DataCollection.Where(SIRule).Where(e => !CacheSINodeCollection.Contains(e)).ToList();
            foreach (Entity StartEntity in StartingSet_3)
            {
                FindGraph(StartEntity);
            }
        }


        /// <summary>
        /// 根据节点开始寻图
        /// 实现根据节点，广度遍历该节点所能到达的所有的回路的方法
        /// 把数据填充到GraphsDic中
        /// </summary>
        /// <param name="startingEntity"></param>
        public void FindGraph(Entity startingEntity)
        {
            if (!FASRule(startingEntity) && !CMTBRule(startingEntity) && !SIRule(startingEntity))
            {
                return;
            }
            //是配电箱 FAS / 短路隔离器 SI
            if (startingEntity is BlockReference blockObj)
            {
                Polyline polyline = Buffer(blockObj);
                var results = FindNextEntity(blockObj, polyline);
                foreach (var result in results)
                {
                    //#1碰到桥架或者配电箱，忽略
                    if (CMTBRule(result) || FASRule(result))
                    {
                        continue;
                    }
                    //碰到块
                    if (result is BlockReference blkref)
                    {
                        Point3d center = Database.GetBlockReferenceOBBCenter(blockObj);
                        if (this.GraphsDic.ContainsKey(center))
                            this.GraphsDic[center].AddRange(FirstNavigate(startingEntity, result));
                        else
                            this.GraphsDic.Add(center, FirstNavigate(startingEntity, result));
                    }
                    //碰到线
                    else if (result is Curve findcurve)
                    {
                        //线得搭到块上才可遍历，否则认为线只是跨过块
                        var blockobb = Buffer(blockObj, 0);
                        if (blockobb.Distance(findcurve.StartPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance || blockobb.Distance(findcurve.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                        {
                            Point3d center = Database.GetBlockReferenceOBBCenter(blockObj);
                            if (this.GraphsDic.ContainsKey(center))
                                this.GraphsDic[center].AddRange(FirstNavigate(startingEntity, result));
                            else
                                this.GraphsDic.Add(center, FirstNavigate(startingEntity, result));
                        }
                    }
                }
            }
            //是桥架 *CMTB
            else if (startingEntity is Curve curve)
            {
                Polyline polyline = Buffer(curve);
                var results = FindNextEntity(curve, polyline);
                foreach (var result in results)
                {
                    if (result is BlockReference blkref)
                    {
                        //根据业务，桥架不搭块，直接忽略
                        continue;
                    }
                    else if (result is Curve findcurve)
                    {
                        //#1 碰到其他桥架，忽略
                        if (CMTBRule(findcurve))
                        {
                            continue;
                        }
                        //#2 碰到线，找到最近的点
                        bool IsStart = findcurve.StartPoint.DistanceTo(curve.GetClosestPointTo(findcurve.StartPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance;
                        bool IsEnd = findcurve.EndPoint.DistanceTo(curve.GetClosestPointTo(findcurve.EndPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance;
                        //都不相邻即无关系，都相邻即近似平行，都不符合
                        if (IsStart != IsEnd)
                        {
                            if (IsStart)
                            {
                                Point3d center = findcurve.StartPoint;
                                if (this.GraphsDic.ContainsKey(center))
                                    this.GraphsDic[center].AddRange(FirstNavigate(curve, findcurve));
                                else
                                    this.GraphsDic.Add(center, FirstNavigate(curve, findcurve));
                            }
                            else
                            {
                                Point3d center = findcurve.EndPoint;
                                if (this.GraphsDic.ContainsKey(center))
                                    this.GraphsDic[center].AddRange(FirstNavigate(curve, findcurve));
                                else
                                    this.GraphsDic.Add(center, FirstNavigate(curve, findcurve));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据初始点
        /// 初次寻路
        /// </summary>
        /// <param name="SourceEntity"></param>
        /// <returns></returns>
        public List<ThAlarmControlWireCircuitModel> FirstNavigate(Entity SourceEntity, Entity nextEntity)
        {
            List<ThAlarmControlWireCircuitModel> FindWireCircuitModels = new List<ThAlarmControlWireCircuitModel>();
            var findLoop = FindRootNextElement(SourceEntity, nextEntity);
            foreach (var item in findLoop)
            {
                //此时已经可以确定，这是仅有的一条回路了
                var newWireCircuitModel = CreatWireCircuitModel(SourceEntity);
                List<List<ThAlarmControlWireCircuitModel>> extendNewWireCircuits = new List<List<ThAlarmControlWireCircuitModel>>();
                if (SIRule(SourceEntity))
                {
                    if (!CacheSINodeCollection.Contains(SourceEntity))
                    {
                        CacheSINodeCollection.Add(SourceEntity);
                        CacheDataCollection.Add(SourceEntity);
                    }
                }

                //是短路隔离器
                if (SIRule(item.Key))
                {
                    List<Entity> nextLoops;
                    if (item.Value.Count > 0)
                    {
                        nextLoops = FindNextEntity(item.Value.Last(), Buffer(item.Key));
                    }
                    else
                    {
                        nextLoops = FindNextEntity(SourceEntity, Buffer(item.Key));
                    }
                    nextLoops.Remove(item.Key);

                    foreach (Entity entity in nextLoops)
                    {
                        //这就是自己本身延伸出去的块
                        var NavigateGraph = FirstNavigate(item.Key, entity);
                        if (NavigateGraph.Count > 0)
                        {
                            extendNewWireCircuits.Add(NavigateGraph);
                        }
                    }
                    if (!CacheSINodeCollection.Contains(item.Key))
                    {
                        CacheSINodeCollection.Add(item.Key);
                        CacheDataCollection.Add(item.Key);
                    }
                }
                //正常块
                else if (!CacheDataCollection.Contains(item.Key))
                {
                    newWireCircuitModel.Graph.AddEdgeAndVertex(SourceEntity, item.Key, item.Value);
                    CacheDataCollection.Add(item.Key);
                    if (item.Value.Count > 0)
                    {
                        var extensionGraph = ExtensionGraph(ref newWireCircuitModel, item.Value.Last(), item.Key);
                        if (extensionGraph.Count > 0)
                        {
                            extendNewWireCircuits.AddRange(extensionGraph);
                        }
                    }
                    else
                    {
                        var extensionGraph = ExtensionGraph(ref newWireCircuitModel, SourceEntity, item.Key);
                        if (extensionGraph.Count > 0)
                        {
                            extendNewWireCircuits.AddRange(extensionGraph);
                        }
                    }
                }

                //开始进行业务逻辑,整合回路
                newWireCircuitModel.FillingData(Database, this.globleBlockAttInfoDic, IsJF);
                for (int i = 0; i < extendNewWireCircuits.Count; i++)
                {
                    ThAlarmControlWireCircuitModel wireCircuitModel = extendNewWireCircuits[i][0];
                    wireCircuitModel.FillingData(Database, this.globleBlockAttInfoDic, IsJF);
                    var extendBlockCount = wireCircuitModel.BlockCount;

                    var selfBlockCount = newWireCircuitModel.BlockCount;
                    if (extendBlockCount + selfBlockCount <= FireCompartmentParameter.ShortCircuitIsolatorCount)
                    {
                        if (string.IsNullOrWhiteSpace(wireCircuitModel.WireCircuitName))
                        {
                            newWireCircuitModel += wireCircuitModel;//左右顺序很重要！
                            extendNewWireCircuits[i].Remove(wireCircuitModel);
                        }
                        else if (string.IsNullOrWhiteSpace(newWireCircuitModel.WireCircuitName))
                        {
                            newWireCircuitModel = wireCircuitModel + newWireCircuitModel;
                            extendNewWireCircuits[i].Remove(wireCircuitModel);
                        }
                        else
                        {
                            //两个回路都有名字，不合并
                        }
                    }
                }
                FindWireCircuitModels.Add(newWireCircuitModel);
                for (int i = 0; i < extendNewWireCircuits.Count; i++)
                {
                    FindWireCircuitModels.AddRange(extendNewWireCircuits[i]);
                }
            }
            return FindWireCircuitModels;
        }

        public ThAlarmControlWireCircuitModel CreatWireCircuitModel(Entity rootEntity)
        {
            ThAlarmControlWireCircuitModel newWireCircuitModel = new ThAlarmControlWireCircuitModel();
            newWireCircuitModel.Graph = new AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>>(false);
            var Source = new ThAFASVertex() { VertexElement = rootEntity, IsStartVertexOfGraph = true };
            newWireCircuitModel.Graph.AddVertex(Source);
            return newWireCircuitModel;
        }

        /// <summary>
        /// 扩展图
        /// </summary>
        /// <param name="Graph"></param>
        /// <param name="SourceEntity"></param>
        /// <param name="TargetEntity"></param>
        public List<List<ThAlarmControlWireCircuitModel>> ExtensionGraph(ref ThAlarmControlWireCircuitModel WireCircuitModel, Entity existingElement, BlockReference TargetEntity)
        {
            if (string.IsNullOrEmpty(WireCircuitModel.WireCircuitName))
            {
                DBText wireCircuitName = FindWireCircuitName(TargetEntity);
                if (!wireCircuitName.IsNull())
                {
                    WireCircuitModel.WireCircuitName = wireCircuitName.TextString;
                    WireCircuitModel.TextPoint = wireCircuitName.Position;
                }
                if (WireCircuitModel.TextPoint.Equals(Point3d.Origin) && IsAlarmControlWireCircuitBlock(TargetEntity))
                    WireCircuitModel.TextPoint = TargetEntity.Position.Add(new Vector3d(0, 150, 0));
            }
            List<List<ThAlarmControlWireCircuitModel>> extensiongraphs = new List<List<ThAlarmControlWireCircuitModel>>();
            var nextElement = FindNextElement(existingElement, TargetEntity);
            foreach (var item in nextElement)
            {
                //碰到配电箱
                if(FASRule(item.Key))
                {
                    //Do Not
                }
                //是短路隔离器
                else if (SIRule(item.Key))
                {
                    List<Entity> nextLoops;
                    if (item.Value.Count > 0)
                    {
                        nextLoops = FindNextEntity(item.Value.Last(), Buffer(item.Key));
                    }
                    else
                    {
                        nextLoops = FindNextEntity(TargetEntity, Buffer(item.Key));
                    }
                    nextLoops.Remove(item.Key);
                    foreach (Entity entity in nextLoops)
                    {
                        //这就是自己本身延伸出去的块
                        var NavigateGraph = FirstNavigate(item.Key, entity);
                        if (NavigateGraph.Count > 0)
                        {
                            extensiongraphs.Add(NavigateGraph);
                        }
                    }
                    if (!CacheSINodeCollection.Contains(item.Key))
                    {
                        CacheSINodeCollection.Add(item.Key);
                        CacheDataCollection.Add(item.Key);
                    }
                }
                //正常块
                else if (!CacheDataCollection.Contains(item.Key))
                {
                    WireCircuitModel.Graph.AddEdgeAndVertex(TargetEntity, item.Key, item.Value);
                    CacheDataCollection.Add(item.Key);
                    if (item.Value.Count > 0)
                    {
                        var extensionGraph = ExtensionGraph(ref WireCircuitModel, item.Value.Last(), item.Key);
                        if (extensionGraph.Count > 0)
                        {
                            extensiongraphs.AddRange(extensionGraph);
                        }
                    }
                    else
                    {
                        var extensionGraph = ExtensionGraph(ref WireCircuitModel, TargetEntity, item.Key);
                        if (extensionGraph.Count > 0)
                        {
                            extensiongraphs.AddRange(extensionGraph);
                        }
                    }
                }
            }
            return extensiongraphs;
        }

        /// <summary>
        /// 判断是否是自动报警控制总线经过的块
        /// </summary>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        private bool IsAlarmControlWireCircuitBlock(BlockReference blk)
        {
            return globleBlockAttInfoDic.ContainsKey(blk) && !ThAutoFireAlarmSystemCommon.NotInAlarmControlWireCircuitBlockNames.Contains(blk.Name);
        }

        /// <summary>
        /// 查找下一个路径
        /// </summary>
        /// <param name="sourceElement">已存在的曲线</param>
        /// <param name="space">探针</param>
        /// <returns></returns>
        public BlockReference FindNextPath(ref List<Curve> sharedPath,ref List<Point3d> AlarmPoint, Curve sourceElement, bool IsStartPoint)
        {
            sharedPath.Add(sourceElement);
            var fireCpmpartmentresults = FireCompartmentIndex.SelectFence(sourceElement);
            if(fireCpmpartmentresults.Count>0)
            {
                foreach (Entity fireCpmpartmenBounder in fireCpmpartmentresults.Cast<Entity>())
                {
                    var pts = new Point3dCollection();
                    sourceElement.IntersectWith(fireCpmpartmenBounder, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    AlarmPoint.AddRange(pts.Cast<Point3d>());
                }
            }
            var space = (IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
            var results = SpatialIndex.SelectCrossingPolygon(space);
            results = results.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
            results.Remove(sourceElement);
            switch (results.Count)
            {
                //没有找到任何元素，说明元素进行了跳过，创建长探针，进行搜索，如果还搜索不到，则异常
                case 0:
                    {
                        if (sourceElement is Line sourceline)
                        {
                            Polyline longProbe = new Polyline();
                            if (IsStartPoint)
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.StartPoint, ExtendLine(sourceline.EndPoint, sourceline.StartPoint), ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
                            }
                            else
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.EndPoint, ExtendLine(sourceline.StartPoint, sourceline.EndPoint), ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
                            }
                            var longProbeResults = SpatialIndex.SelectCrossingPolygon(longProbe);
                            longProbeResults = longProbeResults.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
                            longProbeResults.Remove(sourceElement);
                            var longProbeLineResults = longProbeResults.Cast<Entity>().Where(e => e is Line).Cast<Line>().ToList();
                            //长探针只能找到一个符合条件的线。如果遇到多条，只取最符合的一条线
                            var point = IsStartPoint ? sourceline.StartPoint : sourceline.EndPoint;
                            longProbeLineResults = longProbeLineResults.Where(o => ThGeometryTool.IsCollinearEx(sourceline.StartPoint, sourceline.EndPoint, o.StartPoint, o.EndPoint)).OrderBy(o => Math.Min(point.DistanceTo(o.StartPoint), point.DistanceTo(o.EndPoint))).ToList();
                            if (longProbeLineResults.Count > 0)
                            {
                                bool isStartPoint = point.DistanceTo(longProbeLineResults[0].StartPoint) > point.DistanceTo(longProbeLineResults[0].EndPoint);
                                return FindNextPath(ref sharedPath,ref AlarmPoint, longProbeLineResults[0], isStartPoint);
                            }
                        }
                        break;
                    }
                //找到下一个元素，继续寻路或者返回
                case 1:
                    {
                        if (results[0] is BlockReference blk)
                        {
                            return blk;
                        }
                        else if (results[0] is Curve curve)
                        {
                            if (CMTBRule(curve))
                                return null;
                            if (curve.EndPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            {
                                return FindNextPath(ref sharedPath, ref AlarmPoint, curve, true);
                            }
                            else if (curve.StartPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            {
                                return FindNextPath(ref sharedPath, ref AlarmPoint, curve, false);
                            }
                        }
                        break;
                    }
                //遇到多个元素，认为必定有块，如果全是线，则异常
                default:
                    {
                        var blkResults = results.Cast<Entity>().Where(e => e is BlockReference).ToList();
                        if (blkResults.Count() > 0)
                        {
                            return blkResults.First() as BlockReference;
                        }
                        break;
                    }
            }
            return null;
        }

        /// <summary>
        /// 由根节点查找下一个路径
        /// </summary>
        /// <param name="sourceElement">已存在的曲线</param>
        /// <param name="space">探针</param>
        /// <returns></returns>
        public Dictionary<BlockReference, List<Curve>> FindRootNextPath(ref List<Curve> sharedPath, Curve sourceElement, bool IsStartPoint)
        {
            Dictionary<BlockReference, List<Curve>> FindPath = new Dictionary<BlockReference, List<Curve>>();
            if (sharedPath.Contains(sourceElement))
                return FindPath;
            sharedPath.Add(sourceElement);
            var probe = (IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint).CreateSquare(ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
            var probeResults = SpatialIndex.SelectCrossingPolygon(probe);
            probeResults = probeResults.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
            probeResults.Remove(sourceElement);
            switch (probeResults.Count)
            {
                //没有找到任何元素，说明元素进行了跳过，创建长探针，进行搜索，如果还搜索不到，则异常
                case 0:
                    {
                        if (sourceElement is Line sourceline)
                        {
                            Polyline longProbe = new Polyline();
                            if (IsStartPoint)
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.StartPoint, ExtendLine(sourceline.EndPoint, sourceline.StartPoint), ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
                            }
                            else
                            {
                                longProbe = ThDrawTool.ToRectangle(sourceline.EndPoint, ExtendLine(sourceline.StartPoint, sourceline.EndPoint), ThAutoFireAlarmSystemCommon.ConnectionTolerance * 2);
                            }
                            var longProbeResults = SpatialIndex.SelectCrossingPolygon(longProbe);
                            longProbeResults = longProbeResults.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
                            longProbeResults.Remove(sourceElement);
                            var longProbeLineResults = longProbeResults.Cast<Entity>().Where(e => e is Line).Cast<Line>().ToList();
                            //长探针只能找到一个符合条件的线。如果遇到多条，只取最符合的一条线
                            var point = IsStartPoint ? sourceline.StartPoint : sourceline.EndPoint;
                            longProbeLineResults = longProbeLineResults.Where(o => ThGeometryTool.IsCollinearEx(sourceline.StartPoint, sourceline.EndPoint, o.StartPoint, o.EndPoint)).OrderBy(o => Math.Min(point.DistanceTo(o.StartPoint), point.DistanceTo(o.EndPoint))).ToList();
                            if (longProbeLineResults.Count > 0)
                            {
                                bool isStartPoint = point.DistanceTo(longProbeLineResults[0].StartPoint) > point.DistanceTo(longProbeLineResults[0].EndPoint);
                                return FindRootNextPath(ref sharedPath, longProbeLineResults[0], isStartPoint);
                            }
                        }
                        break;
                    }
                //找到下一个元素，返回
                case 1:
                    {
                        if (probeResults[0] is BlockReference blk)
                        {
                            FindPath.Add(blk, sharedPath);
                        }
                        else if (probeResults[0] is Curve curve)
                        {
                            if (CMTBRule(curve))
                                return FindPath;
                            if (curve.EndPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            {
                                return FindRootNextPath(ref sharedPath, curve, true);
                            }
                            else if (curve.StartPoint.DistanceTo(IsStartPoint ? sourceElement.StartPoint : sourceElement.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            {
                                return FindRootNextPath(ref sharedPath, curve, false);
                            }
                            else if (sourceElement is Line sourceline && probeResults[0] is Line targetline)
                            {
                                var mainVec = sourceline.StartPoint.GetVectorTo(sourceline.EndPoint);
                                var branchVec = targetline.StartPoint.GetVectorTo(targetline.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetline);
                                    var square = Buffer(targetline);
                                    var Secondresults = SpatialIndex.SelectCrossingPolygon(square);
                                    Secondresults = Secondresults.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
                                    Secondresults.Remove(sourceline);
                                    Secondresults.Remove(targetline);
                                    if (Secondresults.Count == 0)
                                        break;
                                    else
                                    {
                                        foreach (var secondEntity in Secondresults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.StartPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(ref newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.EndPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(ref newsharedPath, secondCurve, true).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                //遇到多个元素，认为必定有块/出现分支现象
                default:
                    {
                        //遇到块的情况
                        var blkResults = probeResults.Cast<Entity>().Where(e => e is BlockReference).ToList();
                        if (blkResults.Count() > 0)
                        {
                            var blk = blkResults.First() as BlockReference;
                            FindPath.Add(blk, sharedPath);
                        }
                        //遇到分支的情况
                        else if (sourceElement is Line sourceline)
                        {
                            var mainVec = sourceline.StartPoint.GetVectorTo(sourceline.EndPoint);
                            foreach (Line targetline in probeResults.Cast<Entity>().Where(e => e is Line).Cast<Line>())
                            {
                                if (CMTBRule(targetline))
                                    continue;
                                var branchVec = targetline.StartPoint.GetVectorTo(targetline.EndPoint);
                                var ang = mainVec.GetAngleTo(branchVec);
                                if (ang > Math.PI)
                                {
                                    ang -= Math.PI;
                                }
                                //误差一度内认为近似垂直
                                if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                                {
                                    sharedPath.Add(targetline);
                                    var square = Buffer(targetline);
                                    var Secondresults = SpatialIndex.SelectCrossingPolygon(square);
                                    Secondresults = Secondresults.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
                                    Secondresults.Remove(sourceline);
                                    Secondresults.Remove(targetline);
                                    if (Secondresults.Count == 0)
                                        break;
                                    else
                                    {
                                        foreach (var secondEntity in Secondresults)
                                        {
                                            if (secondEntity is Curve secondCurve)
                                            {
                                                if (secondCurve.StartPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.StartPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(ref newsharedPath, secondCurve, false).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                                else if (secondCurve.EndPoint.DistanceTo(targetline.GetClosestPointTo(secondCurve.EndPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                                                {
                                                    var newsharedPath = sharedPath.Clone().ToList();
                                                    FindRootNextPath(ref newsharedPath, secondCurve, true).ForEach(newPath =>
                                                    {
                                                        FindPath.Add(newPath.Key, newPath.Value);
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
            }
            return FindPath;
        }

        /// <summary>
        /// 由一个块找到下个元素(块)
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="sourceElement">源点块</param>
        /// <returns></returns>
        public Dictionary<BlockReference, List<Curve>> FindNextElement(Entity existingElement, BlockReference sourceElement)
        {
            Dictionary<BlockReference, List<Curve>> NextElements = new Dictionary<BlockReference, List<Curve>>();
            var results = SpatialIndex.SelectCrossingPolygon(Buffer(sourceElement));
            results = results.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
            results.Remove(existingElement);
            results.Remove(sourceElement);
            foreach (var result in results)
            {
                if (result is BlockReference blk)
                {
                    //碰到终点
                    if (FASRule(blk) || NextElements.ContainsKey(blk))
                        continue;
                    NextElements.Add(blk, new List<Curve>());
                }
                else if (result is Curve curve)
                {
                    //碰到终点
                    if (CMTBRule(curve))
                        continue;
                    List<Curve> sharedPath = new List<Curve>();
                    List<Point3d> AlarmPoint = new List<Point3d>();
                    var blockobb = Buffer(sourceElement, 0);
                    if (blockobb.Contains(curve.StartPoint) || blockobb.Distance(curve.StartPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                    {
                        var NextBlk = FindNextPath(ref sharedPath, ref AlarmPoint, curve, false);
                        if (!NextBlk.IsNull() && !NextElements.ContainsKey(NextBlk))
                        {
                            if (!SIRule(NextBlk) && !FASRule(NextBlk))
                            {
                                //新逻辑:只要穿防火分区，就打上标记
                                CrossAlarms.AddRange(AlarmPoint);
                            }
                            NextElements.Add(NextBlk, sharedPath);
                        }
                    }
                    else if (blockobb.Contains(curve.EndPoint) || blockobb.Distance(curve.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                    {
                        var NextBlk = FindNextPath(ref sharedPath, ref AlarmPoint, curve, true);
                        if (!NextBlk.IsNull() && !NextElements.ContainsKey(NextBlk))
                        {
                            if (!SIRule(NextBlk) && !FASRule(NextBlk))
                            {
                                //新逻辑:只要穿防火分区，就打上标记
                                CrossAlarms.AddRange(AlarmPoint);
                            }
                            NextElements.Add(NextBlk, sharedPath);
                        }
                    }
                }
            }
            return NextElements;
        }

        /// <summary>
        /// 由根节点找到下个元素(块)
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="sourceElement">源点块</param>
        /// <returns></returns>
        public Dictionary<BlockReference, List<Curve>> FindRootNextElement(Entity rootElement, Entity specifyElement)
        {
            Dictionary<BlockReference, List<Curve>> NextElement = new Dictionary<BlockReference, List<Curve>>();
            //本身就是根节点出发的，如果碰到另一个根节点，直接忽略
            if (FASRule(specifyElement) || CMTBRule(specifyElement))
                return new Dictionary<BlockReference, List<Curve>>() { };
            //如果本身就是个块，则不用继续寻路
            if (specifyElement is BlockReference blk)
                return new Dictionary<BlockReference, List<Curve>>() { { blk, new List<Curve>() } };
            //线需要寻块，且要考虑到一条线延伸多条线的情况
            else if (specifyElement is Curve curve)
            {
                List<Curve> sharedpath = new List<Curve>();
                //配电箱/短路隔离器
                if (rootElement is BlockReference rootblk)
                {
                    //起点连着块
                    if (Buffer(rootblk, 0).Distance(curve.StartPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                    {
                        NextElement = FindRootNextPath(ref sharedpath, curve, false);
                    }
                    //终点连着块
                    else
                    {
                        NextElement = FindRootNextPath(ref sharedpath, curve, true);
                    }
                }
                //桥架
                else if (rootElement is Curve rootcurve)
                {
                    //起点连着桥架
                    if (curve.StartPoint.DistanceTo(rootcurve.GetClosestPointTo(curve.StartPoint, false)) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                    {
                        NextElement = FindRootNextPath(ref sharedpath, curve, false);
                    }
                    //终点连着桥架
                    else
                    {
                        NextElement = FindRootNextPath(ref sharedpath, curve, true);
                    }
                }
            }
            return NextElement;
        }

        /// <summary>
        /// 查找该空间所有的元素
        /// </summary>
        /// <param name="existingElement">已存在的元素</param>
        /// <param name="space">空间</param>
        /// <returns></returns>
        public List<Entity> FindNextEntity(Entity existingEntity, Polyline space)
        {
            var results = SpatialIndex.SelectCrossingPolygon(space);
            results = results.Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(o => !(o is DBText)).ToCollection();
            results.Remove(existingEntity);
            return results.Cast<Entity>().ToList();
        }

        public DBText FindWireCircuitName(BlockReference blk)
        {
            var polyline = Buffer(blk, 0.0);
            var results = SpatialIndex.SelectCrossingPolygon(polyline).Cast<Entity>().Select(o => GlobleNTSMappingDic[o]).Where(e => e is DBText);
            return results.Cast<DBText>().Where(o => polyline.Contains(Database.GetDBTextReferenceOBBCenter(o))).FirstOrDefault();
        }

        /// <summary>
        /// 延伸点至指定长度
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="ep"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Point3d ExtendLine(Point3d sp, Point3d ep, double length = 2000.0)
        {
            var vec = sp.GetVectorTo(ep).GetNormal();
            return ep + vec.MultiplyBy(length);
        }

        public Polyline Buffer(Entity entity, double distance = ThAutoFireAlarmSystemCommon.ConnectionTolerance)
        {
            if (entity is Curve curve)
            {
                if (curve is Line line)
                {
                    return line.Buffer(distance);
                }
                else if (curve is Arc arc)
                {
                    var objs = arc.TessellateArcWithArc(100.0).BufferPL(distance);
                    return objs[0] as Polyline;
                }
                else if (curve is Polyline polyline)
                {
                    var objs = polyline.TessellatePolylineWithArc(100.0).BufferPL(distance);
                    return objs.Cast<Polyline>().OrderByDescending(o => o.Length).First();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (entity is BlockReference blk)
            {
                Polyline rectangle = Database.GetBlockReferenceOBB(blk);
                return rectangle.Buffer(distance)[0] as Polyline;
            }
            else
            {
                //不支持其他的数据类型
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 画穿越防火分区警告标识
        /// </summary>
        public void DrawCrossAlarms()
        {
            using (var dbSwitch = new ThDbWorkingDatabaseSwitch(Database))
            using (AcadDatabase acad = AcadDatabase.Use(Database))
            {
                //用kdTree做过滤重复点位
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                foreach (Point3d pt in CrossAlarms)
                {
                    kdTree.InsertPoint(pt);
                }
                this.CrossAlarms = kdTree.SelectAll().Cast<Point3d>().ToList();
                List<Tuple<string, Database, ObjectId>> tuples = new List<Tuple<string, Database, ObjectId>>();
                this.CrossAlarms.ForEach(o =>
                {
                    var newDBText = new DBText() { Height = 800, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = "×", Position = o, AlignmentPoint = o, Layer = ThAutoFireAlarmSystemCommon.WireCircuitByLayer };
                    acad.ModelSpace.Add(newDBText);
                });
            }
        }

        /// <summary>
        /// 画寻路算法路径，不要删掉，排查问题时很有用
        /// </summary>
        public void DrawGraphs()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(Database))
            {
                int colorindex = 1;
                foreach (var graphs in GraphsDic.Values.ToList())
                {
                    foreach (var graph in graphs)
                    {
                        foreach (var item in graph.Graph.Edges)
                        {
                            item.Edge.ForEach(o =>
                            {
                                o.ColorIndex = colorindex;
                                acadDatabase.ModelSpace.Add(o);
                            });
                        }
                        foreach (var item in graph.Graph.Vertices)
                        {
                            var clone = item.VertexElement.Clone() as Entity;
                            if (clone is BlockReference cloneblk)
                            {
                                cloneblk.ColorIndex = colorindex;
                                acadDatabase.ModelSpace.Add(cloneblk);
                                if (item.IsStartVertexOfGraph)
                                {
                                    DBText dBText = new DBText() { Height = 150, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, ColorIndex = colorindex, TextString = colorindex.ToString(), Position = cloneblk.Position.Add(new Point3d(0, 250, 0).GetAsVector()), AlignmentPoint = cloneblk.Position.Add(new Point3d(0, 250, 0).GetAsVector()) };
                                    acadDatabase.ModelSpace.Add(dBText);
                                }
                            }
                            if (clone is Curve clinecurve)
                            {
                                clinecurve.ColorIndex = colorindex;
                                acadDatabase.ModelSpace.Add(clinecurve);
                                if (item.IsStartVertexOfGraph)
                                {
                                    DBText dBText = new DBText() { Height = 150, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, ColorIndex = colorindex, TextString = colorindex.ToString(), Position = clinecurve.StartPoint.Add(new Point3d(0, 250, 0).GetAsVector()), AlignmentPoint = clinecurve.StartPoint.Add(new Point3d(0, 250, 0).GetAsVector()) };
                                    acadDatabase.ModelSpace.Add(dBText);
                                }
                            }
                        }
                        colorindex++;
                    }
                }
            }
        }
    }

    public class ThAFASVertex : IEquatable<ThAFASVertex>
    {
        /// <summary>
        /// 顶点元素（可能为块，可能为桥架）
        /// </summary>
        public Entity VertexElement { get; set; }
        public bool IsStartVertexOfGraph { get; set; }

        public bool Equals(ThAFASVertex other)
        {
            return other.VertexElement == this.VertexElement;
        }
    }
    public class ThAFASEdge<T> : Edge<T> where T : ThAFASVertex
    {
        /// <summary>
        /// 边
        /// </summary>
        public List<Curve> Edge { get; set; }


        public ThAFASEdge(T source, T target) : base(source, target)
        {
            Edge = new List<Curve>() { };
        }
    }
}
