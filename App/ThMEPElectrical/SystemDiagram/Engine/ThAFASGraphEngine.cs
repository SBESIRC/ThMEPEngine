using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAFASGraphEngine
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public List<AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>>> Graphs { get; set; }
        public ThAFASVertex GraphStartVertex { get; set; }
        public AcadDatabase acadDatabase { get; set; }

        private List<Entity> DataCollection { get; set; }
        private List<Entity> CacheDataCollection { get; set; }

        //起点终点规则
        private Func<Entity, bool> BreakpointRules = (e) =>
        {
            if (e is BlockReference blk)
            {
                if (blk.Name == "E-BFAS540" || blk.Name == "E-BFAS010")
                    return true;
            }
            if (e is Line line)
            {
                if (e.Layer.Contains("CMTB"))
                    return true;
            }
            return false;
        };


        public ThAFASGraphEngine(List<Entity> Datas)
        {
            Graphs = new List<AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>>>();
            DataCollection = Datas;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(DataCollection.ToCollection());
            CacheDataCollection = new List<Entity>();
        }

        public void SetDataBase(AcadDatabase adb)
        {
            this.acadDatabase = adb;
        }

        public void InitGraph()
        {
            List<Entity> StartingSet = DataCollection.Where(BreakpointRules).ToList();
            //单独处理起始部分，进行广度遍历,以找到下一个点为终止
            Polyline polyline = new Polyline();
            foreach (Entity StartEntity in StartingSet)
            {
                //是配电箱 SI/FAS
                if (StartEntity is BlockReference blockObj)
                {
                    polyline = Buffer(blockObj);
                }
                //是桥架 *CMTB
                else if (StartEntity is Line line)
                {
                    Buffer(line);
                }
                var results = SpatialIndex.SelectCrossingPolygon(polyline);
                results.Remove(StartEntity);
                if (results.Count == 0)
                {
                    continue;
                }
                foreach (var result in results)
                {
                    if (result is BlockReference blkref)
                    {
                        if (CacheDataCollection.Contains(blkref))
                            continue;
                        if (BreakpointRules(blkref))
                        {
                            continue;
                        }
                        CacheDataCollection.Add(blkref);
                        Graphs.Add(CreatGraph(StartEntity, blkref, null));
                    }
                    else if (result is Curve curve)
                    {
                        if (BreakpointRules(curve))
                        {
                            continue;
                        }
                        bool IsStartPoint = false;
                        if (StartEntity is BlockReference blk)
                        {
                            IsStartPoint = curve.EndPoint.DistanceTo(blk.Position) < curve.StartPoint.DistanceTo(blk.Position) ? false : true;
                        }
                        if (StartEntity is Line line)
                        {
                            IsStartPoint = curve.StartPoint.DistanceTo(line.GetClosestPointTo(curve.StartPoint, false)) < curve.EndPoint.DistanceTo(line.GetClosestPointTo(curve.StartPoint, false)) ? true : false;
                        }
                        Point3d searchpoint = IsStartPoint ? curve.EndPoint : curve.StartPoint;
                        FindGraph(StartEntity, curve, searchpoint);
                    }
                }
            }
        }

        public void FindGraph(Entity startEntity, Curve LastCurve, Point3d searchpoint)
        {
            var square = searchpoint.CreateSquare(ThAutoFireAlarmSystemCommon.ConnectionTolerance);
            var results = SpatialIndex.SelectCrossingPolygon(square);
            results.Remove(LastCurve);
            if (results.Count == 0)
                return;
            else
            {
                foreach (Entity NextEntity in results)
                {
                    if (BreakpointRules(NextEntity))
                    {
                        continue;
                    }
                    if (NextEntity is BlockReference blkref)
                    {
                        if (CacheDataCollection.Contains(blkref))
                            continue;
                        CacheDataCollection.Add(blkref);
                        Graphs.Add(CreatGraph(startEntity, blkref, LastCurve));
                    }
                    if (NextEntity is Curve NextCurve)
                    {
                        //两边相接
                        if (searchpoint.DistanceTo(NextCurve.StartPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            FindGraph(startEntity, NextCurve, NextCurve.EndPoint);
                        else if (searchpoint.DistanceTo(NextCurve.EndPoint) < ThAutoFireAlarmSystemCommon.ConnectionTolerance)
                            FindGraph(startEntity, NextCurve, NextCurve.StartPoint);
                        //两边垂直
                        else if (NextCurve is Line NextLine && LastCurve is Line LastLine)
                        {
                            var mainVec = LastLine.StartPoint.GetVectorTo(LastLine.EndPoint);
                            var branchVec = NextLine.StartPoint.GetVectorTo(NextLine.EndPoint);
                            var ang = mainVec.GetAngleTo(branchVec);
                            if (ang > Math.PI)
                            {
                                ang -= Math.PI;
                            }
                            //误差一度内认为近似垂直
                            if (Math.Abs(ang / Math.PI * 180 - 90) < 1)
                            {
                                square = Buffer(NextLine);
                                var Secondresults = SpatialIndex.SelectCrossingPolygon(square);
                                Secondresults.Remove(LastLine);
                                Secondresults.Remove(NextLine);
                                if (Secondresults.Count == 0)
                                    return;
                                else
                                {
                                    foreach (var secondEntity in Secondresults)
                                    {
                                        if (secondEntity is BlockReference Secondblkref)
                                        {
                                            if (CacheDataCollection.Contains(Secondblkref))
                                                continue;
                                            if (BreakpointRules(Secondblkref))
                                            {
                                                continue;
                                            }
                                            CacheDataCollection.Add(Secondblkref);
                                            Graphs.Add(CreatGraph(startEntity, Secondblkref, NextLine));
                                        }
                                        else if (secondEntity is Curve Secondcurve)
                                        {
                                            if (BreakpointRules(Secondcurve))
                                            {
                                                continue;
                                            }
                                            bool IsStartPoint = false;
                                            IsStartPoint = Secondcurve.StartPoint.DistanceTo(LastLine.GetClosestPointTo(Secondcurve.StartPoint, false)) < Secondcurve.EndPoint.DistanceTo(LastLine.GetClosestPointTo(Secondcurve.StartPoint, false)) ? true : false;
                                            searchpoint = IsStartPoint ? Secondcurve.StartPoint : Secondcurve.EndPoint;
                                            FindGraph(startEntity, Secondcurve, searchpoint);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> CreatGraph(Entity startEntity,BlockReference block, Entity SourceEntity)
        {
            var newGraph = new AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>>(false);
            var Source = new ThAFASVertex() { VertexElement = startEntity, IsStartVertexOfGraph = true };
            newGraph.AddVertex(Source);
            BuildGraph(ref newGraph, new List<Curve>(), SourceEntity, block);
            return newGraph;
        }

        public void BuildGraph(ref AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> Graph, List<Curve> edge,Entity SourceEntity,BlockReference TargetEntity)
        {
            var Target = new ThAFASVertex() { VertexElement = TargetEntity, IsStartVertexOfGraph = false };
            Graph.AddEdge(new ThAFASEdge<ThAFASVertex>(Graph.Vertices.Last(), Target) { Edge= edge });
            Graph.AddVertex(Target);
            List<Curve> Edge = new List<Curve>();

            Polyline Square;
            Square = Buffer(TargetEntity);
            var results = SpatialIndex.SelectCrossingPolygon(Square);
            results.Remove(SourceEntity);
            results.Remove(TargetEntity);
            if (results.Count == 0)
            {
                return;
            }
            foreach (var result in results)
            {
                if(result is BlockReference blk)
                {
                    BuildGraph(ref Graph, Edge, TargetEntity, blk);
                }
                if (result is Curve curve)
                {
                    Point3d searchpoint;
                    //两边相接
                    if (Square.Contains(curve.StartPoint))
                    {
                        searchpoint = curve.EndPoint;
                        BuildGraph(ref Graph,ref edge, curve, searchpoint);
                    }
                    if (Square.Contains(curve.EndPoint))
                    {
                        searchpoint = curve.StartPoint;
                        BuildGraph(ref Graph,ref edge, curve, searchpoint);
                    }
                } 
            }
        }

        public void BuildGraph(ref AdjacencyGraph<ThAFASVertex, ThAFASEdge<ThAFASVertex>> Graph, ref List<Curve> Edge, Curve SourceEntity, Point3d TargetPoint)
        {
            Edge.Add(SourceEntity);
            Polyline Square = TargetPoint.CreateSquare(ThAutoFireAlarmSystemCommon.ConnectionTolerance);
            var results = SpatialIndex.SelectCrossingPolygon(Square);
            results.Remove(SourceEntity);
            if (results.Count == 0)
            {
                return;
            }
            foreach (var result in results)
            {
                if (result is BlockReference blk)
                {
                    BuildGraph(ref Graph, Edge, SourceEntity, blk);
                }
                if (result is Curve curve)
                {
                    Point3d searchpoint;
                    //两边相接
                    if (Square.Contains(curve.StartPoint))
                    {
                        searchpoint = curve.EndPoint;
                        BuildGraph(ref Graph,ref Edge, curve, searchpoint);
                    }
                    if (Square.Contains(curve.EndPoint))
                    {
                        searchpoint = curve.StartPoint;
                        BuildGraph(ref Graph,ref Edge, curve, searchpoint);
                    }
                }
            }
        }

        public Polyline Buffer(Entity entity)
        {
            if (entity is Line line)
            {
                return line.Buffer(ThAutoFireAlarmSystemCommon.ConnectionTolerance);
            }
            else if (entity is BlockReference blk)
            {
                Polyline poly = blk.ToOBB(blk.BlockTransform);
                return poly.Buffer(ThAutoFireAlarmSystemCommon.ConnectionTolerance)[0] as Polyline;
            }
            else
            {
                throw new NotSupportedException();
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
            //要根据实际情况去创建自己的边，没办法根据两个节点去创建边
            //暂时还不清楚我要不要再去抽象一下生成一条线
            //后面再说
            Edge = new List<Curve>() { };
        }
    }
}
