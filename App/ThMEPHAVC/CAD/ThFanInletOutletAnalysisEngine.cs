using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuickGraph;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHAVC.CAD
{
    //public class FanAnalysisModel
    //{
    //    public Point3d FanInletBasePoint { get; set; }
    //    public Point3d FanOutletBasePoint { get; set; }
    //    public double RotateAngle { get; set; }
    //    public string InAndOutForm { get; set; }
    //    public DBObjectCollection InAndOutLines { get; set; }
    //}
    public class ThFanInletOutletAnalysisEngine
    {
        //public FanAnalysisModel FanModel { get; set; }
        ThDbModelFan FanModel { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> InletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> InletStartEdge { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> OutletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> OutletStartEdge { get; set; }
        public string InletAnalysisResult { get; set; }
        public string OutletAnalysisResult { get; set; }
        public ThFanInletOutletAnalysisEngine(ThDbModelFan fanmodel)
        {
            FanModel = fanmodel;
            ThDuctEdge<ThDuctVertex> tempinletfirstedge = null;
            ThDuctEdge<ThDuctVertex> tempoutletfirstedge = null;
            InletCenterLineGraph = CreateLineGraph(fanmodel.FanInletBasePoint, ref tempinletfirstedge);
            InletStartEdge = tempinletfirstedge;
            OutletCenterLineGraph = CreateLineGraph(fanmodel.FanOutletBasePoint, ref tempoutletfirstedge);
            OutletStartEdge = tempoutletfirstedge;

            //test
            //using (AcadDatabase acadDatabase = AcadDatabase.Active())
            //{
            //    Line infirst = new Line()
            //    {
            //        StartPoint = InletStartEdge.Source.Position,
            //        EndPoint = InletStartEdge.Target.Position,
            //        ColorIndex = 1
            //    };
            //    Line outfirst = new Line()
            //    {
            //        StartPoint = OutletStartEdge.Source.Position,
            //        EndPoint = OutletStartEdge.Target.Position,
            //        ColorIndex = 1
            //    };
            //    acadDatabase.ModelSpace.Add(infirst);
            //    acadDatabase.ModelSpace.Add(outfirst);
            //}
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

        public void InletAnalysis()
        {
            //进口处无连线
            if (InletCenterLineGraph.Edges.Count() == 0)
            {
                if (FanModel.IntakeForm.Contains("上进") || FanModel.IntakeForm.Contains("下进"))
                {
                    InletAnalysisResult = "InletOK_WithoutCenterLine";
                    return;
                }
                //非上进或下进，且进口处没有连线
                else
                {
                    InletAnalysisResult = "WrongInlet_WithoutCenterLine";
                    return;
                }
            }
            //进口处有连线
            else
            {
                if (InletStartEdge.IsNull())
                {
                    InletAnalysisResult = "WrongInlet_Empty";
                    return;
                }
                foreach (var edge in InletCenterLineGraph.Edges)
                {
                    if (InletCenterLineGraph.OutDegree(edge.Target) == 1)
                    {
                        var leftvector = edge.Target.Position.GetVectorTo(InletCenterLineGraph.OutEdges(edge.Target).FirstOrDefault().Target.Position);
                        var rightvector = edge.Target.Position.GetVectorTo(edge.Source.Position);

                        if (leftvector.DotProduct(rightvector) / (leftvector.Length * rightvector.Length) < 0)
                        {
                            InletAnalysisResult = "WrongInlet_AcuteAngle";
                            return;
                        }
                    }
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
                        InletAnalysisResult = "InletOK";
                        return;
                    }
                    else
                    {
                        InletAnalysisResult = "WrongInlet_NotVertical";
                        return;
                    }
                }
                //进口处有连线且为直进或侧进
                else
                {
                    if (ApproximateEqualTo(startinletlineangle, faninletangle, 1))
                    {
                        InletAnalysisResult = "InletOK";
                        return;
                    }
                    else
                    {
                        InletAnalysisResult = "WrongInlet_NotVertical";
                        return;
                    }
                }
            }
        }

        public void OutletAnalysis()
        {
            //出口处无连线
            if (OutletCenterLineGraph.Edges.Count() == 0)
            {
                if (FanModel.IntakeForm.Contains("上出") || FanModel.IntakeForm.Contains("下出"))
                {
                    OutletAnalysisResult = "OutletOK_WithoutCenterLine";
                    return;
                }
                //直出，且出口处没有连线
                else
                {
                    OutletAnalysisResult = "WrongOutlet_WithoutCenterLine";
                    return;
                }
            }
            //出口处有连线
            else
            {
                if (OutletStartEdge.IsNull())
                {
                    OutletAnalysisResult = "WrongOutlet_Empty";
                    return;
                }

                foreach (var edge in OutletCenterLineGraph.Edges)
                {
                    if (OutletCenterLineGraph.OutDegree(edge.Target) == 1)
                    {
                        var leftvector = edge.Target.Position.GetVectorTo(OutletCenterLineGraph.OutEdges(edge.Target).FirstOrDefault().Target.Position);
                        var rightvector = edge.Target.Position.GetVectorTo(edge.Source.Position);

                        if (leftvector.DotProduct(rightvector) / (leftvector.Length * rightvector.Length) < 0)
                        {
                            OutletAnalysisResult = "WrongOutlet_AcuteAngle";
                            return;
                        }
                    }
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
                        OutletAnalysisResult = "OutletOK";
                        return;
                    }
                    else
                    {
                        OutletAnalysisResult = "WrongOutlet_NotVertical";
                        return;
                    }
                }
                //出口处有连线且为直出
                else
                {
                    if (ApproximateEqualTo(startOutletlineangle, fanoutletangle, 1))
                    {
                        OutletAnalysisResult = "OutletOK";
                        return;
                    }
                    else
                    {
                        OutletAnalysisResult = "WrongOutlet_NotVertical";
                        return;
                    }
                }
            }
        }


        private bool ApproximateEqualTo(double valuea, double valueb, double tolerance)
        {
            return Math.Abs(valuea - valueb) < tolerance;
        }
    }
}
