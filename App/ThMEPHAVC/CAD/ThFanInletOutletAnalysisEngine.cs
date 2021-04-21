using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    public enum AnalysisResultType
    {
        OK = 0,
        Wrong_AcuteAngle = 1,
        Wrong_Empty = 2,
        Wrong_NotVertical = 3
    }
    public enum TeeType
    {
        TEE_COLLINEAR_WITH_INNER = 0,
        TEE_VERTICAL_WITH_OTHERS = 1,
        TEE_ON_THE_LEFT_OF_INNER = 2,
        TEE_ON_THE_RIGHT_OF_INNER = 3
    }
    public struct TeeInfo
    {
        public Vector3d dir { get; set; }    // 根据dir判断是否翻转
        public Vector2d angle { get; set; }
        public TeeType tee_type { get; set; } // 旁通管与进出风管的关系
        public Point3d position { get; set; }

    }
    public class ThFanInletOutletAnalysisEngine
    {
        ThDbModelFan FanModel { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> InletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> InletStartEdge { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> OutletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> OutletStartEdge { get; set; }
        public AnalysisResultType InletAnalysisResult { get; set; }
        public AnalysisResultType OutletAnalysisResult { get; set; }
        public List<Point3d> InletAcuteAnglePositions { get; set; }
        public List<Point3d> OutletAcuteAnglePositions { get; set; }
        public List<TeeInfo> InTeesInfo { get; set; }
        public List<TeeInfo> OutTeesInfo { get; set; }
        public Line LastBypass { get; set; }
        public Line MaxBypass { get; set; } // 用于第一种类型的插管方向
        public Vector2d Inner_fan_dir_vec { get; set; }
        public bool Have_in_inner_fan;
        public bool Have_out_inner_fan;
        public ThFanInletOutletAnalysisEngine(ThDbModelFan fanmodel, Duct_InParam info)
        {
            MaxBypass = new Line();
            LastBypass = new Line();
            FanModel = fanmodel;
            ThDuctEdge<ThDuctVertex> tempinletfirstedge = null;
            ThDuctEdge<ThDuctVertex> tempoutletfirstedge = null;
            InletAcuteAnglePositions = new List<Point3d>();
            OutletAcuteAnglePositions = new List<Point3d>();
            InTeesInfo = new List<TeeInfo>();
            OutTeesInfo = new List<TeeInfo>();
            Inner_fan_dir_vec = new Vector2d(0, 0);
            Have_in_inner_fan = false;
            Have_out_inner_fan = false;

            Point3d in_search_point = Point3d.Origin;
            Point3d out_search_point = Point3d.Origin;
            Update_search_point(fanmodel, info, ref in_search_point, ref out_search_point);

            InletCenterLineGraph = CreateLineGraph(in_search_point, ref tempinletfirstedge);
            InletStartEdge = tempinletfirstedge;
            OutletCenterLineGraph = CreateLineGraph(out_search_point, ref tempoutletfirstedge);
            OutletStartEdge = tempoutletfirstedge;
        }

        private void Update_search_point(ThDbModelFan fanmodel, Duct_InParam info, ref Point3d in_search_point, ref Point3d out_search_point)
        {
            in_search_point = fanmodel.FanInletBasePoint;
            out_search_point = fanmodel.FanOutletBasePoint;
            
            update_proc("上进", info, ref in_search_point, fanmodel.InAndOutLines);
            update_proc("下进", info, ref in_search_point, fanmodel.InAndOutLines);
            update_proc("上出", info, ref out_search_point, fanmodel.InAndOutLines);
            update_proc("下出", info, ref out_search_point, fanmodel.InAndOutLines);
        }

        private void update_proc(string special_name, Duct_InParam info, ref Point3d search_point, DBObjectCollection liens)
        {
            if (FanModel.IntakeForm.Contains(special_name))
            {
                double shrink_dis = Get_shrink_dis(info, false);
                if (shrink_dis < 0)
                    return;
                Line start_line = Get_start_line(search_point, liens);
                if (start_line.Length == 0)
                    return;
                Line new_line = Shrink_start_line(start_line, ref search_point, shrink_dis);
                Exclude_first_line(liens, new_line, start_line);
                if (special_name.Contains("进"))
                    Have_in_inner_fan = true;
                else if (special_name.Contains("出"))
                    Have_out_inner_fan = true;
            }
        }

        private double Get_shrink_dis(Duct_InParam info, bool is_in)
        {
            string duct_size = is_in ? info.in_duct_info : info.out_duct_info;
            string[] str = duct_size.Split('x');
            if (str.Length != 2)
                return -1;
            return Double.Parse(str[1]) * 0.5;
        }

        private Line Get_start_line(Point3d point, DBObjectCollection lines)
        {
            var SpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            var poly = new Polyline();
            poly.CreatePolygon(point.ToPoint2D(), 4, 10);
            var results = SpatialIndex.SelectCrossingPolygon(poly);
            if (results.Count != 1)
            {
                return new Line();
            }
            return results[0] as Line;
        }

        private Line Shrink_start_line(Line line, ref Point3d start_point, double dis)
        {
            Point2d sp = line.StartPoint.ToPoint2D();
            Point2d ep = line.EndPoint.ToPoint2D();
            Point2d new_sp;
            Point2d new_ep;

            if (Math.Abs(sp.X - start_point.X) < 1.5 && Math.Abs(sp.Y - start_point.Y) < 1.5)
            {
                new_sp = sp;
                new_ep = ep;
            }
            else
            {
                new_sp = ep;
                new_ep = sp;
            }
            Vector2d dir_vec = new_ep.GetAsVector() - new_sp.GetAsVector();
            Inner_fan_dir_vec = dir_vec.GetNormal();
            Vector2d vec = Inner_fan_dir_vec * dis + new_sp.GetAsVector();
            start_point = new Point3d(vec.X, vec.Y, 0);
            return new Line(start_point, new Point3d(new_ep.X, new_ep.Y, 0));
        }
        private void Exclude_first_line(DBObjectCollection lines, Line new_line, Line exclude_line)
        {
            lines.Remove(exclude_line);
            lines.Add(new_line);
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
                        TeeInfo info = Create_draw_tee_param(bypass_lines, edge, InletCenterLineGraph);
                        InTeesInfo.Add(info);
                    }
                    Get_bypass_info(edge, bypass_lines);
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
                        TeeInfo info = Create_draw_tee_param(bypass_lines, edge, OutletCenterLineGraph);
                        OutTeesInfo.Add(info);
                    }
                    Get_bypass_info(edge, bypass_lines);
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

        private void Get_bypass_info(ThDuctEdge<ThDuctVertex> edge, DBObjectCollection bypass_lines)
        {
            Point3d src = edge.Source.Position;
            Point3d dst = edge.Target.Position;
            if (ThServiceTee.Is_bypass(src, dst, bypass_lines))
            {
                LastBypass.EndPoint = dst;
                LastBypass.StartPoint = src;
                if (LastBypass.Length > MaxBypass.Length)
                {
                    MaxBypass.EndPoint = dst;
                    MaxBypass.StartPoint = src;
                }
            }
        }

        private TeeInfo Create_draw_tee_param(DBObjectCollection bypass_lines,
                                              ThDuctEdge<ThDuctVertex> edge,
                                              AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> line_graph)
        {
            ThDuctEdge<ThDuctVertex> bypass;
            ThDuctEdge<ThDuctVertex> outter;
            Point3d tee_cp = edge.Target.Position;
            var p1 = line_graph.OutEdges(edge.Target).First().Target;
            var p2 = line_graph.OutEdges(edge.Target).First().Source;
            Tolerance tor = new Tolerance(1.5, 1.5);
            if (ThServiceTee.Is_bypass(p1.Position, p2.Position, bypass_lines))
            {
                bypass = line_graph.OutEdges(edge.Target).First();
                outter = line_graph.OutEdges(edge.Target).Last();
            }
            else
            {
                bypass = line_graph.OutEdges(edge.Target).Last();
                outter = line_graph.OutEdges(edge.Target).First();
            }
            Point3d bypass_tar = bypass.Target.Position;
            if (bypass.Target.Position.IsEqualTo(tee_cp, tor))
                bypass_tar = bypass.Source.Position;
            Point3d outter_tar = outter.Target.Position;
            if (bypass.Target.Position.IsEqualTo(tee_cp, tor))
                outter_tar = outter.Source.Position;

            Vector3d v1 = bypass_tar.GetAsVector() - tee_cp.GetAsVector();
            Vector3d v2 = outter_tar.GetAsVector() - tee_cp.GetAsVector();
            Vector3d v3 = tee_cp.GetAsVector() - edge.Source.Position.GetAsVector();
            Vector3d u_bypass_vec = v1.GetNormal();
            Vector3d u_outter_vec = v2.GetNormal();
            Vector3d u_inner_vec = v3.GetNormal();
            Vector3d dir = u_bypass_vec.CrossProduct(u_outter_vec);
            Vector2d branch_dir = new Vector2d(v1.X, v1.Y);
            TeeType type;
            double zero_tor = 1e-3;
            Vector3d in_bypass_dir = u_inner_vec.CrossProduct(u_bypass_vec);
            if (Math.Abs(u_inner_vec.DotProduct(u_outter_vec)) < zero_tor &&
                Math.Abs(u_inner_vec.DotProduct(u_bypass_vec)) < zero_tor)
            {
                if (in_bypass_dir.Z > 0)
                    type = TeeType.TEE_ON_THE_LEFT_OF_INNER;
                else
                    type = TeeType.TEE_ON_THE_RIGHT_OF_INNER;
            }
            else
            {
                if (Math.Abs(u_inner_vec.DotProduct(u_bypass_vec)) < zero_tor)
                    type = TeeType.TEE_VERTICAL_WITH_OTHERS;
                else
                    type = TeeType.TEE_COLLINEAR_WITH_INNER;
            }
            return new TeeInfo { tee_type = type, dir = dir, angle = branch_dir, position = edge.Target.Position };
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

        public bool HasInletTee()
        {
            return (InTeesInfo.Count == 0) ? false : true;
        }
        public bool HasOutletTee()
        {
            return (OutTeesInfo.Count == 0) ? false : true;
        }
        private bool ApproximateEqualTo(double valuea, double valueb, double tolerance)
        {
            return Math.Abs(valuea - valueb) < tolerance || Math.Abs(Math.Abs(valuea - valueb) - 360) < tolerance;
        }
    }
}
