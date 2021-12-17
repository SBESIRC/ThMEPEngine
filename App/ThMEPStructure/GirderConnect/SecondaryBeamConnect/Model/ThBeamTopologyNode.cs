using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Utls;
using ThMEPEngineCore.CAD;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class ThBeamTopologyNode
    {
        public ThBeamTopologyNode(Polyline polyline)
        {
            Boundary = polyline;
            Neighbor = new List<Tuple<Line, ThBeamTopologyNode>>();
            UseBeams = new List<Line>();
            Assists = new List<Line>();
            Edges = new List<BeamEdge>();
            HaveLayoutBackUp=false;
            LayoutLines=new LayoutResult();
        }
        public Polyline Boundary { get; set; }
        public List<Tuple<Line, ThBeamTopologyNode>> Neighbor { get; set; }
        public List<Line> UseBeams { get; set; }
        public List<Line> Assists { get; set; }
        public List<BeamEdge> Edges { get; set; }
        public bool IsWhole { get { return (Edges.Count == UseBeams.Count + Assists.Count) && Edges.All(o => o.BeamType != BeamType.None); } }
        public LayoutResult LayoutLines { get; set; }
        public bool HaveLayoutBackUp { get; set; }
        public LayoutResult SpareLayoutLines { get; set; }

        public void MappingBeam()
        {
            this.Edges = this.Boundary.GetAllSides(UseBeams, Assists);
        }
        public void CalculateSecondaryBeam()
        {
            if(IsWhole)
            {
                LayoutSecondaryBeams(this.Edges);
            }
            else
            {
                //throw new Exception("未正确识别出该梁隔区域!");
                using(Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    var objs = Boundary.Buffer(-10);
                    if(objs.Count == 1)
                    {
                        var polyline = objs[0] as Polyline;
                        polyline.ColorIndex = 2;
                        acad.ModelSpace.Add(polyline);
                    }
                }
            }
        }

        public void SwapLayout()
        {
            if(this.HaveLayoutBackUp)
            {
                var layout = this.LayoutLines;
                this.LayoutLines = this.SpareLayoutLines;
                this.SpareLayoutLines = layout;
            }
        }

        public void Upgrade()
        {
            var layout = this.LayoutLines;
            layout.SecondaryBeamLines = new List<Line>();
            layout.SecondaryBeamLines.Add(new Line(layout.edges[0].TrueSide.GetOnethirdPt(), layout.edges[1].TrueSide.GetTwothirdPt()));
            layout.SecondaryBeamLines.Add(new Line(layout.edges[0].TrueSide.GetTwothirdPt(), layout.edges[1].TrueSide.GetOnethirdPt()));
            this.LayoutLines = layout;
        }

        /// <summary>
        /// 次梁布置
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        private void LayoutSecondaryBeams(List<BeamEdge> edges)
        {
            if (edges.Count == 3)
            {
                //a）if 至少2条主梁边没有相邻次梁连接，则本区格不划分次梁；
                //b）if 只有1条主梁边没有相邻次梁连接，则连接两条有相邻次梁连接的边的中点（或三等分点），作为本区格次梁；
                //c）else 3条边全部有相邻次梁连接，则选取最长中点（或三等分点）连线作为本区格次梁
                if (edges[0].BeamType == BeamType.Scrap)
                {
                    //不布置任何梁
                }
                else
                {
                    //var Needneighbor = this.Neighbor.Where(o => o.Item2.LayoutLines.edges.Intersect(this.Edges).Count() == 1).ToList();
                    var Needneighbor = this.Neighbor.Where(o => o.Item2.LayoutLines.edges.Any(x => x.BeamSide.Equals(o.Item1))).ToList();
                    if (Needneighbor.Count < 2)
                    {
                        //不布置任何梁
                    }
                    else if (Needneighbor.Count == 2)
                    {
                        var pts = Needneighbor.Select(o => o.Item1.GetCenterPt());
                        LayoutResult layoutResult = new LayoutResult();
                        //因为构造三角形区域次梁为最后一步，故不再设置其他属性
                        layoutResult.SecondaryBeamLines.Add(new Line(pts.First(), pts.Last()));
                        layoutResult.vector = pts.First().GetVectorTo(pts.Last());
                        layoutResult.edges.AddRange(this.Edges.Where(o => Needneighbor.Any(x => x.Item1 == o.BeamSide)));
                        this.LayoutLines = layoutResult;
                    }
                    else
                    {
                        var P1 = this.Neighbor[0].Item1.GetCenterPt();
                        var P2 = this.Neighbor[1].Item1.GetCenterPt();
                        var P3 = this.Neighbor[2].Item1.GetCenterPt();
                       
                        List<Line> lines = new List<Line>();
                        lines.Add(new Line(P1, P2));
                        lines.Add(new Line(P2, P3));
                        lines.Add(new Line(P1, P3));
                        if(lines[0].Length < lines[1].Length && lines[0].Length < lines[2].Length)
                        {
                            Needneighbor.RemoveAt(2);
                        }
                        else if(lines[1].Length < lines[0].Length && lines[1].Length < lines[2].Length)
                        {
                            Needneighbor.RemoveAt(0);
                        }
                        else
                        {
                            Needneighbor.RemoveAt(1);
                        }
                        LayoutResult layoutResult = new LayoutResult();
                        var pts = Needneighbor.Select(o => o.Item1.GetCenterPt());
                        layoutResult.SecondaryBeamLines.Add(new Line(pts.First(), pts.Last()));
                        layoutResult.vector = pts.First().GetVectorTo(pts.Last());
                        layoutResult.edges.AddRange(this.Edges.Where(o => Needneighbor.Any(x => x.Item1 == o.BeamSide)));
                        this.LayoutLines = layoutResult;
                    }
                }
            }
            else if (edges.Count == 4)
            {
                ConnectSecondaryBeamType connectType = CalculateQuadrilateral(edges[0].TrueSide, edges[1].TrueSide, edges[2].TrueSide, edges[3].TrueSide, out bool CanAdjustment);
                if (connectType != ConnectSecondaryBeamType.None)
                {
                    if (edges[0].BeamType == BeamType.Scrap)
                    {
                        LayoutResult layoutResult = new LayoutResult();
                        Line centerLine = new Line(edges[1].TrueSide.GetCenterPt(), edges[3].TrueSide.GetCenterPt());
                        Line OnethirdLine = new Line(edges[1].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt());
                        Line TwothirdLine = new Line(edges[1].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt());
                        layoutResult.vector=centerLine.LineDirection();
                        layoutResult.edges.Add(edges[1]);
                        layoutResult.edges.Add(edges[3]);
                        if (connectType == ConnectSecondaryBeamType.SingleConnect)
                        {
                            layoutResult.SecondaryBeamLines.Add(centerLine);
                        }
                        else if (connectType == ConnectSecondaryBeamType.DoubleConnect)
                        {
                            layoutResult.SecondaryBeamLines.Add(OnethirdLine);
                            layoutResult.SecondaryBeamLines.Add(TwothirdLine);
                        }
                        LayoutLines=layoutResult;
                    }
                    else if (edges[0].BeamType == BeamType.Beam)
                    {
                        Line centerLine1 = new Line(edges[0].TrueSide.GetCenterPt(), edges[2].TrueSide.GetCenterPt());
                        Line OnethirdLine1 = new Line(edges[0].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt());
                        Line TwothirdLine1 = new Line(edges[0].TrueSide.GetTwothirdPt(), edges[2].TrueSide.GetOnethirdPt());
                        Line centerLine2 = new Line(edges[1].TrueSide.GetCenterPt(), edges[3].TrueSide.GetCenterPt());
                        Line OnethirdLine2 = new Line(edges[1].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt());
                        Line TwothirdLine2 = new Line(edges[1].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt());
                        LayoutResult layoutResult = new LayoutResult();
                        layoutResult.vector=centerLine1.LineDirection();
                        layoutResult.edges.Add(edges[0]);
                        layoutResult.edges.Add(edges[2]);
                        LayoutResult SparelayoutResult = new LayoutResult();
                        SparelayoutResult.vector=centerLine2.LineDirection();
                        SparelayoutResult.edges.Add(edges[1]);
                        SparelayoutResult.edges.Add(edges[3]);
                        if (connectType == ConnectSecondaryBeamType.SingleConnect)
                        {
                            layoutResult.SecondaryBeamLines.Add(centerLine1);
                            SparelayoutResult.SecondaryBeamLines.Add(centerLine2);
                        }
                        else if (connectType == ConnectSecondaryBeamType.DoubleConnect)
                        {
                            layoutResult.SecondaryBeamLines.Add(OnethirdLine1);
                            layoutResult.SecondaryBeamLines.Add(TwothirdLine1);
                            SparelayoutResult.SecondaryBeamLines.Add(OnethirdLine2);
                            SparelayoutResult.SecondaryBeamLines.Add(TwothirdLine2);
                        }
                        if (CanAdjustment)
                        {
                            HaveLayoutBackUp = true;
                            if (centerLine1.Length >centerLine2.Length)
                            {
                                LayoutLines = layoutResult;
                                SpareLayoutLines = SparelayoutResult;
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                                SpareLayoutLines = layoutResult;
                            }
                        }
                        else
                        {
                            if (centerLine1.Length>centerLine2.Length)
                            {
                                LayoutLines = layoutResult;
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                            }
                        }
                    }
                }
            }
            else if (edges.Count == 5)
            {
                Line newEdgeSide1 = edges[1].TrueSide;
                Line newEdgeSide4 = edges[4].TrueSide;
                //if l5 < l4 /3，忽略l5，按四边形划分次梁；
                //else，延长l3 、l4相交形成外包四边形，按外包四边形区格判断次梁划分形式。
                if (edges[0].BeamType == BeamType.Scrap || (edges[0].BeamType == BeamType.Beam && edges[0].TrueSide.Length < edges[1].TrueSide.Length / 3.0 && edges[0].TrueSide.Length < edges[4].TrueSide.Length / 3.0))
                {
                    Point3dCollection pts = new Point3dCollection();
                    edges[0].TrueSide.IntersectWith(edges[4].TrueSide, Intersect.ExtendBoth, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count == 1)
                    {
                        newEdgeSide1 = new Line(pts[0], edges[1].TrueSide.EndPoint);
                        newEdgeSide4 = new Line(edges[4].TrueSide.StartPoint, pts[0]);
                    }
                }
                ConnectSecondaryBeamType connectType = CalculateQuadrilateral(newEdgeSide1, edges[2].TrueSide, edges[3].TrueSide, newEdgeSide4, out bool CanAdjustment);
                if (connectType != ConnectSecondaryBeamType.None)
                {
                    LayoutResult layoutResult = new LayoutResult();
                    LayoutResult SparelayoutResult = new LayoutResult();
                    layoutResult.vector = edges[1].TrueSide.GetCenterPt().GetVectorTo(edges[3].TrueSide.GetCenterPt());
                    SparelayoutResult.vector = edges[2].TrueSide.GetCenterPt().GetVectorTo(edges[4].TrueSide.GetCenterPt());
                    if (connectType == ConnectSecondaryBeamType.SingleConnect)
                    {
                        Point3d P1 = edges[1].TrueSide.StartPoint;
                        Point3d P2 = edges[1].TrueSide.GetCenterPt();
                        Point3d P3 = edges[3].TrueSide.GetCenterPt();
                        Polyline planA1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[3].TrueSide.GetCenterPt(), edges[4].TrueSide.StartPoint }.CreatePolyline();
                        Polyline planA2 = new Point3dCollection() { edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetCenterPt() }.CreatePolyline();
                        Polyline planB1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[1].TrueSide.GetCenterPt(), edges[3].TrueSide.GetCenterPt(), edges[4].TrueSide.StartPoint }.CreatePolyline();
                        Polyline planB2 = new Point3dCollection() { edges[1].TrueSide.GetCenterPt(), edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetCenterPt() }.CreatePolyline();
                        //Update 逻辑更改：之前是选取两条中的最短边，现在是选取次梁最能够平分梁隔区域的最优边
                        //Line L1 = new Line(P1.DistanceTo(P3) < P2.DistanceTo(P3) ? P1 : P2, P3);
                        Line L1 = new Line(planA1.AreaRatio(planA2) > planB1.AreaRatio(planB2) ? P1 : P2, P3);
                        double areaRatioL1 = Math.Max(planA1.AreaRatio(planA2) , planB1.AreaRatio(planB2));

                        Point3d P4 = edges[4].TrueSide.GetCenterPt();
                        Point3d P5 = edges[4].TrueSide.EndPoint;
                        Point3d P6 = edges[2].TrueSide.GetCenterPt();
                        Polyline planC1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[2].TrueSide.GetCenterPt(), edges[4].TrueSide.GetCenterPt() }.CreatePolyline();
                        Polyline planC2 = new Point3dCollection() { edges[2].TrueSide.GetCenterPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint, edges[4].TrueSide.GetCenterPt() }.CreatePolyline();
                        Polyline planD1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[2].TrueSide.GetCenterPt() }.CreatePolyline();
                        Polyline planD2 = new Point3dCollection() { edges[2].TrueSide.GetCenterPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint, edges[4].TrueSide.EndPoint }.CreatePolyline();
                        //同上
                        //Line L2 = new Line(P4.DistanceTo(P6) < P5.DistanceTo(P6) ? P4 : P5, P6);
                        Line L2 = new Line(planC1.AreaRatio(planC2) > planD1.AreaRatio(planD2) ? P4 : P5, P6);
                        double areaRatioL2 = Math.Max(planC1.AreaRatio(planC2), planD1.AreaRatio(planD2));

                        CanAdjustment = Math.Max(L1.Length, L2.Length) < Math.Min(L1.Length, L2.Length) * SecondaryBeamLayoutConfig.Er;
                        layoutResult.edges.Add(edges[1]);
                        layoutResult.edges.Add(edges[3]);
                        layoutResult.SecondaryBeamLines.Add(L1);
                        SparelayoutResult.edges.Add(edges[2]);
                        SparelayoutResult.edges.Add(edges[4]);
                        SparelayoutResult.SecondaryBeamLines.Add(L2);
                        if (edges[0].BeamType == BeamType.Scrap)
                        {
                            if (edges[0].TrueSide.GetLineAngle(L1) < edges[0].TrueSide.GetLineAngle(L2))
                            {
                                LayoutLines = layoutResult;
                                //if (CanAdjustment)
                                //{
                                //    HaveLayoutBackUp =true;
                                //    SpareLayoutLines =SparelayoutResult;
                                //}
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                                //if (CanAdjustment)
                                //{
                                //    HaveLayoutBackUp =true;
                                //    SpareLayoutLines =layoutResult;
                                //}
                            }
                        }
                        else if (edges[0].BeamType == BeamType.Beam)
                        {
                            if (90 - edges[0].BeamSide.GetLineAngle(edges[1].BeamSide) < SecondaryBeamLayoutConfig.AngleTolerance &&
                                90 - edges[0].BeamSide.GetLineAngle(edges[4].BeamSide) < SecondaryBeamLayoutConfig.AngleTolerance)
                            {
                                Polyline planE1 = new Point3dCollection() { edges[0].TrueSide.GetCenterPt(), edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[2].TrueSide.GetCenterPt() }.CreatePolyline();
                                Polyline planE2 = new Point3dCollection() { edges[2].TrueSide.GetCenterPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint, edges[0].TrueSide.StartPoint, edges[0].TrueSide.GetCenterPt() }.CreatePolyline();
                                Polyline planF1 = new Point3dCollection() { edges[0].TrueSide.GetCenterPt(), edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetCenterPt() }.CreatePolyline();
                                Polyline planF2 = new Point3dCollection() { edges[3].TrueSide.GetCenterPt(), edges[4].TrueSide.StartPoint, edges[4].TrueSide.EndPoint, edges[0].TrueSide.GetCenterPt() }.CreatePolyline();
                                var areaRatioE = planE1.AreaRatio(planE2);
                                var areaRatioF = planF1.AreaRatio(planF2);
                                if (areaRatioE > areaRatioF && areaRatioE > areaRatioL2)
                                {
                                    L2 = new Line(edges[0].TrueSide.GetCenterPt(), edges[2].TrueSide.GetCenterPt());
                                    SparelayoutResult = new LayoutResult();
                                    SparelayoutResult.vector = edges[0].TrueSide.GetCenterPt().GetVectorTo(edges[2].TrueSide.GetCenterPt()); 
                                    SparelayoutResult.edges.Add(edges[0]);
                                    SparelayoutResult.edges.Add(edges[2]);
                                    SparelayoutResult.SecondaryBeamLines.Add(L2);
                                }
                                else if(areaRatioF > areaRatioE && areaRatioF > areaRatioL1)
                                {
                                    L1 = new Line(edges[0].TrueSide.GetCenterPt(), edges[3].TrueSide.GetCenterPt());
                                    layoutResult = new LayoutResult();
                                    layoutResult.vector = edges[0].TrueSide.GetCenterPt().GetVectorTo(edges[3].TrueSide.GetCenterPt());
                                    layoutResult.edges.Add(edges[0]);
                                    layoutResult.edges.Add(edges[3]);
                                    layoutResult.SecondaryBeamLines.Add(L1);
                                }

                            }
                            if (L1.Length > L2.Length)
                            {
                                LayoutLines = layoutResult;
                                if (CanAdjustment)
                                {
                                    HaveLayoutBackUp =true;
                                    SpareLayoutLines =SparelayoutResult;
                                }
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                                if (CanAdjustment)
                                {
                                    HaveLayoutBackUp =true;
                                    SpareLayoutLines =layoutResult;
                                }
                            }
                        }
                    }
                    else if (connectType == ConnectSecondaryBeamType.DoubleConnect)
                    {
                        Point3d P1 = edges[1].TrueSide.StartPoint;
                        Point3d P2 = edges[1].TrueSide.GetOnethirdPt();
                        Point3d P3 = edges[3].TrueSide.GetTwothirdPt();
                        Polyline planA1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[3].TrueSide.GetTwothirdPt(), edges[4].TrueSide.StartPoint }.CreatePolyline();
                        Polyline planA2 = new Point3dCollection() { edges[1].TrueSide.StartPoint, edges[1].TrueSide.GetCenterPt(), edges[3].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt() }.CreatePolyline();
                        Polyline planA3 = new Point3dCollection() { edges[1].TrueSide.GetCenterPt(), edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetOnethirdPt() }.CreatePolyline();

                        Polyline planB1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[1].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt(), edges[4].TrueSide.StartPoint }.CreatePolyline();
                        Polyline planB2 = new Point3dCollection() { edges[1].TrueSide.GetOnethirdPt(), edges[1].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt() }.CreatePolyline();
                        Polyline planB3 = new Point3dCollection() { edges[1].TrueSide.GetTwothirdPt(), edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetOnethirdPt() }.CreatePolyline();

                        Line L1_1 = new Line();
                        Line L1_2 = new Line();
                        if (planA1.AreaRatio(planA2, planA3) > planB1.AreaRatio(planB2, planB3))
                        {
                            L1_1 = new Line(P1, P3);
                            L1_2 = new Line(edges[1].TrueSide.GetCenterPt(), edges[3].TrueSide.GetOnethirdPt());
                        }
                        else
                        {
                            L1_1 = new Line(P2, P3);
                            L1_2 = new Line(edges[1].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt());
                        }
                        double areaRatioL1 = Math.Max(planA1.AreaRatio(planA2, planA3), planB1.AreaRatio(planB2, planB3));

                        Point3d P4 = edges[4].TrueSide.EndPoint;
                        Point3d P5 = edges[4].TrueSide.GetTwothirdPt();
                        Point3d P6 = edges[2].TrueSide.GetOnethirdPt();
                        Polyline planC1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, P6 }.CreatePolyline();
                        Polyline planC2 = new Point3dCollection() { edges[0].TrueSide.StartPoint, P6, edges[2].TrueSide.GetTwothirdPt(), edges[4].TrueSide.GetCenterPt() }.CreatePolyline();
                        Polyline planC3 = new Point3dCollection() { edges[4].TrueSide.GetCenterPt(), edges[2].TrueSide.GetTwothirdPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint }.CreatePolyline();
                        Polyline planD1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, P6, edges[4].TrueSide.GetTwothirdPt() }.CreatePolyline();
                        Polyline planD2 = new Point3dCollection() { edges[4].TrueSide.GetTwothirdPt(), P6, edges[2].TrueSide.GetTwothirdPt(), edges[4].TrueSide.GetOnethirdPt() }.CreatePolyline();
                        Polyline planD3 = new Point3dCollection() { edges[4].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint }.CreatePolyline();

                        Line L2_1 = new Line();
                        Line L2_2 = new Line();
                        if (planC1.AreaRatio(planC2, planC3) > planD1.AreaRatio(planD2, planD3))
                        {
                            L2_1 = new Line(P4, P6);
                            L2_2 = new Line(edges[4].TrueSide.GetCenterPt(), edges[2].TrueSide.GetTwothirdPt());
                        }
                        else
                        {
                            L2_1 = new Line(P5, P6);
                            L2_2 = new Line(edges[4].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt());
                        }
                        double areaRatioL2 = Math.Max(planC1.AreaRatio(planC2, planC3), planD1.AreaRatio(planD2, planD3));

                        double lengthPlan1 = L1_1.Length + L1_2.Length;
                        double lengthPlan2 = L2_1.Length + L2_2.Length;
                        CanAdjustment = Math.Max(lengthPlan1, lengthPlan2) < Math.Min(lengthPlan1, lengthPlan2) * SecondaryBeamLayoutConfig.Er;

                        layoutResult.edges.Add(edges[1]);
                        layoutResult.edges.Add(edges[3]);
                        layoutResult.SecondaryBeamLines.Add(L1_1);
                        layoutResult.SecondaryBeamLines.Add(L1_2);
                        SparelayoutResult.edges.Add(edges[2]);
                        SparelayoutResult.edges.Add(edges[4]);
                        SparelayoutResult.SecondaryBeamLines.Add(L2_1);
                        SparelayoutResult.SecondaryBeamLines.Add(L2_2);
                        if (edges[0].BeamType == BeamType.Scrap)
                        {
                            if (edges[0].TrueSide.GetLineAngle(L1_2) < edges[0].TrueSide.GetLineAngle(L2_2))
                            {
                                LayoutLines = layoutResult;
                                //if (CanAdjustment)
                                //{
                                //    HaveLayoutBackUp =true;
                                //    SpareLayoutLines =SparelayoutResult;
                                //}
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                                //if (CanAdjustment)
                                //{
                                //    HaveLayoutBackUp =true;
                                //    SpareLayoutLines =layoutResult;
                                //}
                            }
                        }
                        else if (edges[0].BeamType == BeamType.Beam)
                        {
                            if (90 - edges[0].BeamSide.GetLineAngle(edges[1].BeamSide) < SecondaryBeamLayoutConfig.AngleTolerance &&
                                90 - edges[0].BeamSide.GetLineAngle(edges[4].BeamSide) < SecondaryBeamLayoutConfig.AngleTolerance)
                            {
                                Polyline planE1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[0].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt(), edges[3].TrueSide.StartPoint, edges[4].TrueSide.StartPoint }.CreatePolyline();
                                Polyline planE2 = new Point3dCollection() { edges[0].TrueSide.GetOnethirdPt(), edges[0].TrueSide.GetTwothirdPt(), edges[2].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt() }.CreatePolyline();
                                Polyline planE3 = new Point3dCollection() { edges[0].TrueSide.GetTwothirdPt(), edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[2].TrueSide.GetOnethirdPt() }.CreatePolyline();

                                Polyline planF1 = new Point3dCollection() { edges[0].TrueSide.StartPoint, edges[0].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt(), edges[4].TrueSide.StartPoint }.CreatePolyline();
                                Polyline planF2 = new Point3dCollection() { edges[0].TrueSide.GetOnethirdPt(), edges[0].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt() }.CreatePolyline();
                                Polyline planF3 = new Point3dCollection() { edges[0].TrueSide.GetTwothirdPt(), edges[1].TrueSide.StartPoint, edges[2].TrueSide.StartPoint, edges[3].TrueSide.StartPoint, edges[3].TrueSide.GetOnethirdPt() }.CreatePolyline();
                                var areaRatioE = planE1.AreaRatio(planE2, planE3);
                                var areaRatioF = planF1.AreaRatio(planF2, planF3);
                                if (areaRatioE > areaRatioF && areaRatioE > areaRatioL2)
                                {
                                    L2_1 = new Line(edges[0].TrueSide.GetOnethirdPt(), edges[2].TrueSide.GetTwothirdPt());
                                    L2_2 = new Line(edges[0].TrueSide.GetTwothirdPt(), edges[2].TrueSide.GetOnethirdPt());
                                    lengthPlan2 = L2_1.Length + L2_2.Length;
                                    SparelayoutResult = new LayoutResult();
                                    SparelayoutResult.vector = edges[0].TrueSide.GetCenterPt().GetVectorTo(edges[2].TrueSide.GetCenterPt());
                                    SparelayoutResult.edges.Add(edges[0]);
                                    SparelayoutResult.edges.Add(edges[2]);
                                    SparelayoutResult.SecondaryBeamLines.Add(L2_1);
                                    SparelayoutResult.SecondaryBeamLines.Add(L2_2);
                                }
                                else if (areaRatioF > areaRatioE && areaRatioF > areaRatioL1)
                                {
                                    L1_1 = new Line(edges[0].TrueSide.GetOnethirdPt(), edges[3].TrueSide.GetTwothirdPt());
                                    L1_2 = new Line(edges[0].TrueSide.GetTwothirdPt(), edges[3].TrueSide.GetOnethirdPt());
                                    lengthPlan1 = L1_1.Length + L1_2.Length;
                                    layoutResult = new LayoutResult();
                                    layoutResult.vector = edges[0].TrueSide.GetCenterPt().GetVectorTo(edges[3].TrueSide.GetCenterPt());
                                    layoutResult.edges.Add(edges[0]);
                                    layoutResult.edges.Add(edges[3]);
                                    layoutResult.SecondaryBeamLines.Add(L1_1);
                                    layoutResult.SecondaryBeamLines.Add(L1_2);
                                }

                            }
                            if (lengthPlan1 > lengthPlan2)
                            {
                                LayoutLines = layoutResult;
                                if (CanAdjustment)
                                {
                                    HaveLayoutBackUp =true;
                                    SpareLayoutLines =SparelayoutResult;
                                }
                            }
                            else
                            {
                                LayoutLines = SparelayoutResult;
                                if (CanAdjustment)
                                {
                                    HaveLayoutBackUp =true;
                                    SpareLayoutLines =layoutResult;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 计算单双次梁布置模式
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="line3"></param>
        /// <param name="line4"></param>
        /// <returns></returns>
        private ConnectSecondaryBeamType CalculateQuadrilateral(Line line1, Line line2, Line line3, Line line4, out bool CanAdjustment)
        {
            CanAdjustment = false;
            ConnectSecondaryBeamType connectType;
            Line L1 = new Line(line1.GetCenterPt(), line3.GetCenterPt());
            Line L2 = new Line(line2.GetCenterPt(), line4.GetCenterPt());
            double d1 = L1.Length;
            double d2 = L2.Length;
            var mind = Math.Min(d1, d2);
            var maxd = Math.Max(d1, d2);
            if (SecondaryBeamLayoutConfig.Er * mind >= maxd)
            {
                CanAdjustment = true;
            }
            //if min（d1、d2） ≤ Da 且 max（d1、d2） ≤ Db，（对应结构主次梁布置产品文档小柱网，对应框架大板无次梁的情况)
            if (mind <= SecondaryBeamLayoutConfig.Da && maxd <= SecondaryBeamLayoutConfig.Db)
            {
                connectType = ConnectSecondaryBeamType.None;
            }
            else if((mind <= SecondaryBeamLayoutConfig.Dc))
            {
                connectType = ConnectSecondaryBeamType.None;
            }
            //if Da < min（d1、d2）< Db ，or min（d1、d2） ≤ Da 且 max（d1、d2）＞ Db ，对地下室顶板（输入条件 - 楼层），将该区格从QGList中移除(对应框架大板无次梁的情况)。对地下室中板（输入条件 - 楼层），划分单次梁连线，
            else if ((mind > SecondaryBeamLayoutConfig.Da && mind < SecondaryBeamLayoutConfig.Db) || (mind > SecondaryBeamLayoutConfig.Dc && mind <= SecondaryBeamLayoutConfig.Da && maxd >= SecondaryBeamLayoutConfig.Db))
            {
                connectType = ConnectSecondaryBeamType.SingleConnect;
            }
            //else，对QGList中的区格进行双次梁划分
            else
            {
                connectType = ConnectSecondaryBeamType.DoubleConnect;
            }
            return connectType;
        }
    }
}
