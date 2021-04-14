using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using ThMEPHVAC.Model;

namespace ThMEPHAVC.CAD
{
    public enum AnalysisResultType
    {
        OK = 0,
        Wrong_AcuteAngle = 1,
        Wrong_Empty = 2,
        Wrong_NotVertical = 3
    }
    public class ThFanInletOutletAnalysisEngine
    {
        //public FanAnalysisModel FanModel { get; set; }
        ThDbModelFan FanModel { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> InletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> InletStartEdge { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> OutletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> OutletStartEdge { get; set; }
        public AnalysisResultType InletAnalysisResult { get; set; }
        public AnalysisResultType OutletAnalysisResult { get; set; }
        public List<Point3d> InletAcuteAnglePositions { get; set; }
        public List<Point3d> OutletAcuteAnglePositions { get; set; }
        public List<Point3d> InletTeeCPPositions { get; set; }
        public List<double> ICPAngle { get; set; }
        public List<Point3d> OutletTeeCPPositions { get; set; }
        public List<double> OCPAngle { get; set; }
        public ThFanInletOutletAnalysisEngine(ThDbModelFan fanmodel)
        {
            FanModel = fanmodel;
            ThDuctEdge<ThDuctVertex> tempinletfirstedge = null;
            ThDuctEdge<ThDuctVertex> tempoutletfirstedge = null;
            InletAcuteAnglePositions = new List<Point3d>();
            OutletAcuteAnglePositions = new List<Point3d>();
            InletTeeCPPositions = new List<Point3d>();
            OutletTeeCPPositions = new List<Point3d>();
            ICPAngle = new List<double>();
            OCPAngle = new List<double>();
            InletCenterLineGraph = CreateLineGraph(fanmodel.FanInletBasePoint, ref tempinletfirstedge);
            InletStartEdge = tempinletfirstedge;
            OutletCenterLineGraph = CreateLineGraph(fanmodel.FanOutletBasePoint, ref tempoutletfirstedge);
            OutletStartEdge = tempoutletfirstedge;
        }

        private AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> CreateLineGraph(Point3d basepoint, ref ThDuctEdge<ThDuctVertex> startedge)
        {
            var inletgraphengine = new ThDuctGraphEngine();
            inletgraphengine.BuildGraph(FanModel.InAndOutLines, basepoint);
            if (!inletgraphengine.GraphStartVertex.IsNull())
            {
                startedge = inletgraphengine.Graph.OutEdges(inletgraphengine.GraphStartVertex).First();
            }
            return inletgraphengine.Graph;
        }
        public void InletAnalysis(DBObjectCollection bypass_lines)
        {
            //进口处无连线
            if (InletCenterLineGraph.Edges.Count() == 0)
            {
                InletAnalysisResult = AnalysisResultType.Wrong_Empty;
                return;
            }
            //进口处有连线
            else
            {
                if (InletStartEdge.IsNull())
                {
                    InletAnalysisResult = AnalysisResultType.Wrong_Empty;
                    return;
                }
                foreach (var edge in InletCenterLineGraph.Edges)
                {
                    if (InletCenterLineGraph.OutDegree(edge.Target) == 1)
                    {
                        var cornerpoint = edge.Target.Position;
                        var inletpoint = edge.Source.Position;
                        var outletpoint = InletCenterLineGraph.OutEdges(edge.Target).First().Target.Position;

                        var left2d = new Vector2d(inletpoint.X - cornerpoint.X, inletpoint.Y - cornerpoint.Y);
                        var right2d = new Vector2d(outletpoint.X - cornerpoint.X, outletpoint.Y - cornerpoint.Y);

                        if ((0.5 * Math.PI) - left2d.GetAngleTo(right2d) > 0.01)
                        {
                            InletAcuteAnglePositions.Add(edge.Target.Position);
                        }
                    }
                    else if (InletCenterLineGraph.OutDegree(edge.Target) == 2)
                    {
                        var p1 = InletCenterLineGraph.OutEdges(edge.Target).First().Target;
                        var p2 = InletCenterLineGraph.OutEdges(edge.Target).First().Source;
                        var v = ThServiceTee.is_bypass(p1.Position, p2.Position, bypass_lines) ?
                                InletCenterLineGraph.OutEdges(edge.Target).First() :
                                InletCenterLineGraph.OutEdges(edge.Target).Last();
                        Vector2d vec = new Vector2d(v.Source.Position.X - v.Target.Position.X,
                                                    v.Source.Position.Y - v.Target.Position.Y);
                        ICPAngle.Add(vec.Angle);
                        InletTeeCPPositions.Add(edge.Target.Position);
                    }
                }
                if (InletAcuteAnglePositions.Count != 0)
                {
                    InletAnalysisResult = AnalysisResultType.Wrong_AcuteAngle;
                    return;
                }
                Vector2d startvector = new Vector2d(InletStartEdge.Target.Position.X - InletStartEdge.Source.Position.X, InletStartEdge.Target.Position.Y - InletStartEdge.Source.Position.Y);
                var startinletlineangle = startvector.Angle * 180 / Math.PI;
                var faninletangle = FanModel.FanInlet.Angle;

                if (FanModel.IntakeForm.Contains("上进") || FanModel.IntakeForm.Contains("下进"))
                {
                    if (ApproximateEqualTo(Math.Abs(startinletlineangle - faninletangle), 0, 1) ||
                        ApproximateEqualTo(Math.Abs(startinletlineangle - faninletangle), 90, 1) ||
                        ApproximateEqualTo(Math.Abs(startinletlineangle - faninletangle), 180, 1) ||
                        ApproximateEqualTo(Math.Abs(startinletlineangle - faninletangle), 270, 1))
                    {
                        InletAnalysisResult = AnalysisResultType.OK;
                        return;
                    }
                    else
                    {
                        InletAnalysisResult = AnalysisResultType.Wrong_NotVertical;
                        return;
                    }
                }
                //进口处有连线且为直进或侧进
                else
                {
                    if (ApproximateEqualTo(startinletlineangle, faninletangle, 1))
                    {
                        InletAnalysisResult = AnalysisResultType.OK;
                        return;
                    }
                    else
                    {
                        InletAnalysisResult = AnalysisResultType.Wrong_NotVertical;
                        return;
                    }
                }
            }
        }

        public void OutletAnalysis(DBObjectCollection bypass_lines)
        {
            //出口处无连线
            if (OutletCenterLineGraph.Edges.Count() == 0)
            {
                OutletAnalysisResult = AnalysisResultType.Wrong_Empty;
                return;
            }
            //出口处有连线
            else
            {
                if (OutletStartEdge.IsNull())
                {
                    OutletAnalysisResult = AnalysisResultType.Wrong_Empty;
                    return;
                }

                foreach (var edge in OutletCenterLineGraph.Edges)
                {
                    if (OutletCenterLineGraph.OutDegree(edge.Target) == 1)
                    {
                        var cornerpoint = edge.Target.Position;
                        var inletpoint = edge.Source.Position;
                        var outletpoint = OutletCenterLineGraph.OutEdges(edge.Target).First().Target.Position;

                        var left2d = new Vector2d(inletpoint.X - cornerpoint.X, inletpoint.Y - cornerpoint.Y);
                        var right2d = new Vector2d(outletpoint.X - cornerpoint.X, outletpoint.Y - cornerpoint.Y);

                        if ((0.5 * Math.PI) - left2d.GetAngleTo(right2d) > 0.01)
                        {
                            OutletAcuteAnglePositions.Add(edge.Target.Position);
                        }
                    }
                    else if (OutletCenterLineGraph.OutDegree(edge.Target) == 2)
                    {
                        var p1 = OutletCenterLineGraph.OutEdges(edge.Target).First().Target;
                        var p2 = OutletCenterLineGraph.OutEdges(edge.Target).First().Source;
                        var v = ThServiceTee.is_bypass(p1.Position, p2.Position, bypass_lines) ?
                                OutletCenterLineGraph.OutEdges(edge.Target).First() :
                                OutletCenterLineGraph.OutEdges(edge.Target).Last();
                        Vector2d vec = new Vector2d(v.Source.Position.X - v.Target.Position.X,
                                                    v.Source.Position.Y - v.Target.Position.Y);
                        OCPAngle.Add(vec.Angle);
                        OutletTeeCPPositions.Add(edge.Target.Position);
                    }
                }
                if (OutletAcuteAnglePositions.Count != 0)
                {
                    OutletAnalysisResult = AnalysisResultType.Wrong_AcuteAngle;
                    return;
                }

                Vector2d startvector = new Vector2d(OutletStartEdge.Target.Position.X - OutletStartEdge.Source.Position.X, OutletStartEdge.Target.Position.Y - OutletStartEdge.Source.Position.Y);
                var startOutletlineangle = startvector.Angle * 180 / Math.PI;
                var fanoutletangle = FanModel.FanOutlet.Angle;

                if (FanModel.IntakeForm.Contains("上出") || FanModel.IntakeForm.Contains("下出"))
                {
                    if (ApproximateEqualTo(startOutletlineangle, fanoutletangle, 1) ||
                        ApproximateEqualTo(startOutletlineangle, fanoutletangle + 90, 1) ||
                        ApproximateEqualTo(startOutletlineangle, fanoutletangle + 180, 1) ||
                        ApproximateEqualTo(startOutletlineangle, fanoutletangle + 270, 1))
                    {
                        OutletAnalysisResult = AnalysisResultType.OK;
                        return;
                    }
                    else
                    {
                        OutletAnalysisResult = AnalysisResultType.Wrong_NotVertical;
                        return;
                    }
                }
                //出口处有连线且为直出
                else
                {
                    if (ApproximateEqualTo(startOutletlineangle, fanoutletangle, 1))
                    {
                        OutletAnalysisResult = AnalysisResultType.OK;
                        return;
                    }
                    else
                    {
                        OutletAnalysisResult = AnalysisResultType.Wrong_NotVertical;
                        return;
                    }
                }
            }
        }
        public bool HasInletTee()
        {
            return (InletTeeCPPositions.Count == 0) ? false : true;
        }
        public bool HasOutletTee()
        {
            return (OutletTeeCPPositions.Count == 0) ? false : true;
        }
        private bool ApproximateEqualTo(double valuea, double valueb, double tolerance)
        {
            return Math.Abs(valuea - valueb) < tolerance || Math.Abs(Math.Abs(valuea - valueb) - 360) < tolerance;
        }
    }
}
