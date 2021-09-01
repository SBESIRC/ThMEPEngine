using System;
using Linq2Acad;
using DotNetARX;
using QuickGraph;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Duct;
using TianHua.Publics.BaseCode;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    public class FanOpeningInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double NormalAngle { get; set; }
        public Point3d OpingBasePoint { get; set; }

    }
    public struct Duct_InParam
    {
        public string bypass_size;
        public string bypass_pattern;
        public string room_duct_size;
        public string other_duct_size;
        public string scale;
        public string room_elevation;
        public string other_elevation;
        public bool is_io_reverse;
    }
    public class ThInletOutletDuctDrawEngine
    {
        public FanOpeningInfo InletOpening { get; set; }
        public FanOpeningInfo OutletOpening { get; set; }
        public double InletDuctWidth { get; set; }
        public double OutletDuctWidth { get; set; }
        public double InletDuctHeight { get; set; }
        public double OutletDuctHeight { get; set; }
        public double TeeWidth { get; set; }
        public double TeeHeight { get; set; }
        public double Elevation { get; set; }
        public string FanInOutType { get; set; }
        public List<ThIfcDistributionElement> InletDuctSegments { get; set; }
        public List<ThIfcDistributionElement> OutletDuctSegments { get; set; }
        public List<ThIfcDistributionElement> InletDuctReducings { get; set; }
        public List<ThIfcDistributionElement> OutletDuctReducings { get; set; }
        public List<ThIfcDistributionElement> InletDuctElbows { get; set; }
        public List<ThIfcDistributionElement> OutletDuctElbows { get; set; }
        public List<ThIfcDistributionElement> InletDuctHoses { get; set; }
        public List<ThIfcDistributionElement> OutletDuctHoses { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> InletCenterLineGraph { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> OutletCenterLineGraph { get; set; }
        private List<Vector2d> TextVec { get; set; }
        private Duct_InParam in_param;
        public ThInletOutletDuctDrawEngine(ThDbModelFan fan)
        {
            Init(fan);
            SetInOutHoses(fan.scenario);
            string modelLayer = fan.Data.BlockLayer;
            DrawHoseInDWG(InletDuctHoses, modelLayer); 
            DrawHoseInDWG(OutletDuctHoses, modelLayer);
        }
        public ThInletOutletDuctDrawEngine(ThDbModelFan fanmodel,
            Duct_InParam in_param,
            double selected_bypass_len,
            DBObjectCollection bypass_line,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph)
        {
            Init(fanmodel, in_param, inletcenterlinegraph, outletcenterlinegraph);
            SetInletOutletSize(in_param.room_duct_size, in_param.other_duct_size, in_param.bypass_size, in_param.room_elevation);
            SetInletElbows(bypass_line);
            SetOutletElbows(bypass_line);
            SetInOutHoses(fanmodel.scenario);
            bool isAxial = fanmodel.Model.IsAXIALModel();
            double len = selected_bypass_len;
            SetInletDucts(fanmodel.scenario, isAxial, bypass_line, in_param.scale, len, in_param.bypass_pattern == "RBType3");
            SetOutletDucts(fanmodel.scenario, isAxial, bypass_line, in_param.scale, len, in_param.bypass_pattern == "RBType3");
        }
        private void Init(ThDbModelFan fanmodel)
        {
            FanInOutType = fanmodel.IntakeForm;
            InletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanInlet.Width,
                Height = fanmodel.FanInlet.Height,
                NormalAngle = fanmodel.FanInlet.Angle,
                OpingBasePoint = fanmodel.FanInletBasePoint
            };
            OutletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanOutlet.Width,
                Height = fanmodel.FanOutlet.Height,
                NormalAngle = fanmodel.FanOutlet.Angle,
                OpingBasePoint = fanmodel.FanOutletBasePoint
            };
            InletDuctHoses = new List<ThIfcDistributionElement>();
            OutletDuctHoses = new List<ThIfcDistributionElement>();
        }
        private void Init(ThDbModelFan fanmodel, 
                          Duct_InParam in_param, 
                          AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
                          AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph)
        {
            this.in_param = in_param;
            InletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanInlet.Width,
                Height = fanmodel.FanInlet.Height,
                NormalAngle = fanmodel.FanInlet.Angle,
                OpingBasePoint = fanmodel.FanInletBasePoint
            };
            OutletOpening = new FanOpeningInfo()
            {
                Width = fanmodel.FanOutlet.Width,
                Height = fanmodel.FanOutlet.Height,
                NormalAngle = fanmodel.FanOutlet.Angle,
                OpingBasePoint = fanmodel.FanOutletBasePoint
            };
            InletCenterLineGraph = inletcenterlinegraph;
            OutletCenterLineGraph = outletcenterlinegraph;
            FanInOutType = fanmodel.IntakeForm;
            InletDuctSegments = new List<ThIfcDistributionElement>();
            OutletDuctSegments = new List<ThIfcDistributionElement>();
            InletDuctReducings = new List<ThIfcDistributionElement>();
            OutletDuctReducings = new List<ThIfcDistributionElement>();
            InletDuctElbows = new List<ThIfcDistributionElement>();
            OutletDuctElbows = new List<ThIfcDistributionElement>();
            InletDuctHoses = new List<ThIfcDistributionElement>();
            OutletDuctHoses = new List<ThIfcDistributionElement>();
            TextVec = new List<Vector2d>();
        }
        public void RunInletDrawEngine(ThDbModelFan fanmodel, string textSize)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);
            string centerLinerLayer = ThDuctUtils.DuctCenterLineLayerName(modelLayer);
            string ducttextlayer = ThDuctUtils.DuctTextLayerName(modelLayer);
            DrawDuctInDWG(InletDuctSegments, ductLayer, centerLinerLayer, ductLayer, ducttextlayer, textSize);
            DrawDuctInDWG(InletDuctReducings, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer, textSize);
            DrawDuctInDWG(InletDuctElbows, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer, textSize);
            DrawHoseInDWG(InletDuctHoses, modelLayer);
        }

        public void RunOutletDrawEngine(ThDbModelFan fanmodel, string textSize)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);
            string centerLinerLayer = ThDuctUtils.DuctCenterLineLayerName(modelLayer);
            string ducttextlayer = ThDuctUtils.DuctTextLayerName(modelLayer);
            DrawDuctInDWG(OutletDuctSegments, ductLayer, centerLinerLayer, ductLayer, ducttextlayer, textSize);
            DrawDuctInDWG(OutletDuctReducings, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer, textSize);
            DrawDuctInDWG(OutletDuctElbows, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer, textSize);
            DrawHoseInDWG(OutletDuctHoses, modelLayer);
        }

        public void Proc_inner_duct(ThDbModelFan Model, bool enable, Vector2d dir_vec, Duct_InParam pst_param, bool is_in)
        {
            if (!enable)
                return;
            Point3d base_point = Point3d.Origin;
            string text_size;
            if (is_in)
            {
                text_size = pst_param.room_duct_size;
                base_point = Model.FanInletBasePoint;
            }
            else
            {
                text_size = pst_param.other_duct_size;
                base_point = Model.FanOutletBasePoint;
            }
            string modelLayer = Model.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            var Create_service = new ThHvacDuctFittingFactoryService();
            var geo = Create_service.Create_inner_duct(text_size);
            Matrix3d mat = Matrix3d.Displacement(base_point.GetAsVector()) *
                           Matrix3d.Rotation(dir_vec.Angle + Math.PI * 0.5, Vector3d.ZAxis, Point3d.Origin);
            Draw_inner_duct(geo, mat, ductLayer);
        }

        private void Draw_inner_duct(DBObjectCollection segment, Matrix3d mat, string duct_layer)
        {
            using (AcadDatabase data_base = AcadDatabase.Active())
            {
                var linetypeId = ByLayerLineTypeId();
                var layerId = CreateLayer(duct_layer);

                foreach (Curve c in segment)
                {
                    c.ColorIndex = 256;
                    c.LayerId = layerId;
                    c.LinetypeId = linetypeId;
                    c.TransformBy(mat);
                    data_base.ModelSpace.Add(c);
                    c.SetDatabaseDefaults();
                }
            }
        }

        private void SetInletOutletSize(string innerromeductinfo, 
                                        string outerromeductinfo,
                                        string tee_info,
                                        string elevation_info)
        {
            if (!in_param.is_io_reverse)
            {
                InletDuctWidth = innerromeductinfo.Split('x').First().NullToDouble();
                InletDuctHeight = innerromeductinfo.Split('x').Last().NullToDouble();
                OutletDuctWidth = outerromeductinfo.Split('x').First().NullToDouble();
                OutletDuctHeight = outerromeductinfo.Split('x').Last().NullToDouble();
            }
            else
            {
                InletDuctWidth = outerromeductinfo.Split('x').First().NullToDouble();
                InletDuctHeight = outerromeductinfo.Split('x').Last().NullToDouble();
                OutletDuctWidth = innerromeductinfo.Split('x').First().NullToDouble();
                OutletDuctHeight = innerromeductinfo.Split('x').Last().NullToDouble();
            }
            if (!string.IsNullOrEmpty(tee_info))
            {
                TeeWidth = tee_info.Split('x').First().NullToDouble();
                TeeHeight = tee_info.Split('x').Last().NullToDouble();
            }
            if (!string.IsNullOrEmpty(elevation_info))
                Elevation = Double.Parse(elevation_info);
        }

        private ThIfcDuctSegmentParameters Create_duct_param(Point3d tar_srt_pos,
                                                             Point3d tar_end_pos,
                                                             double edge_len,
                                                             double duct_width,
                                                             double duct_height,
                                                             DBObjectCollection bypass_lines)
        {
            bool is_bypass = ThServiceTee.Is_bypass(tar_srt_pos, tar_end_pos, bypass_lines);
            double Width = is_bypass ? TeeWidth : duct_width;
            double Height = is_bypass ? TeeHeight : duct_height;
            return new ThIfcDuctSegmentParameters
            {
                Width = Width,
                Height = Height,
                Length = edge_len
            };
        }

        private void SetInletDucts(string scenario,bool isaxial, DBObjectCollection bypass_line, string textSize, double text_bypass_len, bool is_type3)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            bool isUpOrDownOpening = FanInOutType.Contains("上进") || FanInOutType.Contains("下进");
            if(!isUpOrDownOpening && InletOpening.Width != InletDuctWidth)
            {
                if (InletCenterLineGraph.Edges.Count() == 0)
                {
                    return;
                }
                //仅对于非上进的风机画变径，对于上进的，不画变径
                double reducingBigEndWidth = Math.Max(InletOpening.Width, InletDuctWidth);
                double reducingSmallEndWidth = Math.Min(InletOpening.Width, InletDuctWidth);
                var ductReducingParameters = new ThIfcDuctReducingParameters()
                {
                    BigEndWidth = reducingBigEndWidth,
                    SmallEndWidth = reducingSmallEndWidth
                };
                //var reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters);
                ThIfcDuctReducing reducing = new ThIfcDuctReducing(new ThIfcDuctReducingParameters());
                //若风机进口宽度比管道宽度小，即风机进口对应变径的小端
                if (InletOpening.Width < InletDuctWidth)
                {
                    if (isaxial)
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.small_circle);
                    }
                    else
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.small);
                    }
                    double rotationangle = InletOpening.NormalAngle * Math.PI / 180;
                    //reducing.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                    reducing.Matrix = Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()));
                }
                //若风机进口宽度比管道宽度大，即风机进口对应变径的大端
                else
                {
                    if (isaxial)
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.big_circle);
                    }
                    else
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.big);
                    }
                    double rotationangle = InletOpening.NormalAngle * Math.PI / 180 - Math.PI;
                    reducing.Matrix = Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()));
                }
                if (InletDuctHoses.Count() != 0)
                {
                    double hoselength = InletDuctHoses.Cast<ThIfcDuctHose>().First().Parameters.Length;
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(new Point3d(hoselength * Math.Cos(InletOpening.NormalAngle * Math.PI / 180), hoselength * Math.Sin(InletOpening.NormalAngle * Math.PI / 180), 0).GetAsVector()));
                }
                InletDuctReducings.Add(reducing);

                var firstvertex = InletCenterLineGraph.Vertices.Where(v => v.IsStartVertexOfGraph).FirstOrDefault();
                if (!firstvertex.IsNull())
                {
                    InletCenterLineGraph.OutEdges(firstvertex).FirstOrDefault().SourceShrink = reducing.Parameters.ReducingLength + ThDuctUtils.GetHoseLength(scenario);
                }
            }
            Line max_line = Max_line_exclude_bypass(InletCenterLineGraph, bypass_line);
            double half_len = text_bypass_len * 0.5;
            var tor = new Tolerance(5, 5);
            foreach (var ductgraphedge in InletCenterLineGraph.Edges)
            {
                bool text_enable = false;
                Point3d srt_p = ductgraphedge.Source.Position;
                Point3d end_p = ductgraphedge.Target.Position;
                double edge_len = ductgraphedge.EdgeLength;
                Line cur_line = new Line(srt_p, end_p);
                var DuctParameters = Create_duct_param( srt_p, end_p, edge_len, InletDuctWidth, InletDuctHeight, bypass_line);

                Vector2d edgevector = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
                double rotateangle = edgevector.Angle;
                string s_evel = string.Empty;
                if (ThMEPHVACService.Is_same_line(max_line, cur_line, tor))
                {
                    text_enable = true;
                    s_evel = null;
                }
                bool is_bypass = false;
                if (ThServiceTee.Is_bypass(srt_p, end_p, bypass_line))
                {
                    is_bypass = true;
                }
                // 给最长的非旁通线添加标注
                if (text_enable)
                    TextVec.Add(edgevector.GetNormal());
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters, rotateangle,
                                                                              text_enable, s_evel, textSize, is_bypass && is_type3);
                ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                Point3d centerpoint = new Point3d(0.5 * (srt_p.X + end_p.X), 0.5 * (srt_p.Y + end_p.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

                InletDuctSegments.Add(ductSegment);
            }
        }

        private void SetOutletDucts(string scenario, bool isaxial, DBObjectCollection bypass_line, string textSize, double text_bypass_len, bool is_type3)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();
            bool isUpOrDownOpening = FanInOutType.Contains("上出") || FanInOutType.Contains("下出");
            if(!isUpOrDownOpening && OutletOpening.Width != OutletDuctWidth)
            {
                if (OutletCenterLineGraph.Edges.Count() == 0)
                {
                    return;
                }
                //仅对于非上进的风机画变径，对于上进的，不画变径
                double reducingBigEndWidth = Math.Max(OutletOpening.Width, OutletDuctWidth);
                double reducingSmallEndWidth = Math.Min(OutletOpening.Width, OutletDuctWidth);

                var ductReducingParameters = new ThIfcDuctReducingParameters()
                {
                    BigEndWidth = reducingBigEndWidth,
                    SmallEndWidth = reducingSmallEndWidth
                };
                ThIfcDuctReducing reducing = new ThIfcDuctReducing(new ThIfcDuctReducingParameters());
                //若风机出口宽度比管道宽度小，即风机出口对应变径的小端
                if (OutletOpening.Width < OutletDuctWidth)
                {
                    if (isaxial)
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.small_circle);
                    }
                    else
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.small);
                    }

                    double rotationangle = OutletOpening.NormalAngle * Math.PI / 180;
                    reducing.Matrix = Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()));
                }
                //若风机出口宽度比管道宽度大，即风机出口对应变径的大端
                else
                {
                    if (isaxial)
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.big_circle);
                    }
                    else
                    {
                        reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters, ReducingToFanJoinType.big);
                    }

                    double rotationangle = OutletOpening.NormalAngle * Math.PI / 180 - Math.PI;
                    reducing.Matrix = Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)));
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()));
                }
                if (OutletDuctHoses.Count() != 0)
                {
                    double hoselength = OutletDuctHoses.Cast<ThIfcDuctHose>().First().Parameters.Length;
                    reducing.Matrix = reducing.Matrix.PreMultiplyBy(Matrix3d.Displacement(new Point3d(hoselength * Math.Cos(OutletOpening.NormalAngle * Math.PI / 180), hoselength * Math.Sin(OutletOpening.NormalAngle * Math.PI / 180), 0).GetAsVector()));
                }
                OutletDuctReducings.Add(reducing);

                var firstvertex = OutletCenterLineGraph.Vertices.Where(v => v.IsStartVertexOfGraph).FirstOrDefault();
                if (!firstvertex.IsNull())
                {
                    OutletCenterLineGraph.OutEdges(firstvertex).FirstOrDefault().SourceShrink = reducing.Parameters.ReducingLength + ThDuctUtils.GetHoseLength(scenario);
                }
            }
            string s_evel = string.Empty;
            Line max_line = Max_line_exclude_bypass(OutletCenterLineGraph, bypass_line);
            double half_len = text_bypass_len * 0.5;
            var tor = new Tolerance(5, 5);
            foreach (var ductgraphedge in OutletCenterLineGraph.Edges)
            {
                bool text_enable = false;
                Point3d srt_p = ductgraphedge.Source.Position;
                Point3d end_p = ductgraphedge.Target.Position;
                double edge_len = ductgraphedge.EdgeLength;
                Line cur_line = new Line(srt_p, end_p);
                var DuctParameters = Create_duct_param(srt_p, end_p, edge_len, OutletDuctWidth, OutletDuctHeight, bypass_line);
                // 给最长的非旁通线添加标注
                if (ThMEPHVACService.Is_same_line(max_line, cur_line, tor))
                {
                    text_enable = true;
                    s_evel = Elevation.ToString();
                }
                bool is_bypass = false;
                if (ThServiceTee.Is_bypass(srt_p, end_p, bypass_line))
                {
                    is_bypass = true;
                }
                Vector2d edgevector = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
                double rotateangle = edgevector.Angle;
                if (text_enable)
                    TextVec.Add(edgevector.GetNormal());
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters, rotateangle,
                                                                              text_enable, s_evel, textSize, is_bypass && is_type3);

                ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                Point3d centerpoint = new Point3d(0.5 * (srt_p.X + end_p.X), 0.5 * (srt_p.Y + end_p.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                OutletDuctSegments.Add(ductSegment);
            }
        }

        private Line Max_line_exclude_bypass(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> graph, DBObjectCollection bypass_line)
        {
            double max_len = 0;
            Line max_line = new Line();
            foreach (var ductgraphedge in graph.Edges)
            {
                Point3d srt_p = ductgraphedge.Source.Position;
                Point3d end_p = ductgraphedge.Target.Position;
                if (!ThServiceTee.Is_bypass(srt_p, end_p, bypass_line))
                {
                    Line l = new Line(srt_p, end_p);
                    if (l.Length > max_len)
                    {
                        max_len = l.Length;
                        max_line = l;
                    }
                }
            }
            return max_line;
        }

        private void SetInletElbows(DBObjectCollection bypass_lines)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            foreach (var edge in InletCenterLineGraph.Edges)
            {
                if (InletCenterLineGraph.OutDegree(edge.Target) != 1)
                {
                    continue;
                }
                else
                {
                    Vector2d invector = new Vector2d(edge.Source.Position.X - edge.Target.Position.X , edge.Source.Position.Y - edge.Target.Position.Y);
                    var outedge = InletCenterLineGraph.OutEdges(edge.Target).First();
                    Vector2d outvector = new Vector2d(outedge.Target.Position.X - outedge.Source.Position.X , outedge.Target.Position.Y - outedge.Source.Position.Y);
                    var edgeangle = 180 - Math.Acos(invector.DotProduct(outvector) / (invector.Length * outvector.Length)) * 180 / Math.PI;
                    Vector2d bisectoroftwoedge = invector / invector.Length + outvector / outvector.Length;

                    bool IsBypass = ThServiceTee.Is_bypass( edge.Source.Position, edge.Target.Position, bypass_lines);
                    double width = IsBypass ? TeeWidth : InletDuctWidth;
                    var elbowParameters = new ThIfcDuctElbowParameters()
                    {
                        ElbowDegree = edgeangle,
                        PipeOpenWidth = width
                    };
                    var elbow = ductFittingFactoryService.CreateElbow(elbowParameters);
                    elbow.Matrix = Matrix3d.Displacement(edge.Target.Position.GetAsVector()) * 
                                   Matrix3d.Rotation(bisectoroftwoedge.Angle - elbow.Parameters.BisectorAngle, Vector3d.ZAxis, elbow.Parameters.CornerPoint);
                    InletDuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
                }
            }
        }

        private void SetOutletElbows(DBObjectCollection bypass_lines)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            foreach (var edge in OutletCenterLineGraph.Edges)
            {
                if (OutletCenterLineGraph.OutDegree(edge.Target) == 1)
                {
                    Vector2d invector = new Vector2d(edge.Source.Position.X - edge.Target.Position.X, edge.Source.Position.Y - edge.Target.Position.Y);
                    var outedge = OutletCenterLineGraph.OutEdges(edge.Target).First();
                    Vector2d outvector = new Vector2d(outedge.Target.Position.X - outedge.Source.Position.X, outedge.Target.Position.Y - outedge.Source.Position.Y);
                    var edgeangle = 180 - Math.Acos(invector.DotProduct(outvector) / (invector.Length * outvector.Length)) * 180 / Math.PI;
                    Vector2d bisectoroftwoedge = invector / invector.Length + outvector / outvector.Length;
                    bool IsBypass = ThServiceTee.Is_bypass(edge.Source.Position, edge.Target.Position, bypass_lines);
                    double width = IsBypass ? TeeWidth : OutletDuctWidth;
                    var elbowParameters = new ThIfcDuctElbowParameters()
                    {
                        ElbowDegree = edgeangle,
                        PipeOpenWidth = width
                    };
                    var elbow = ductFittingFactoryService.CreateElbow(elbowParameters);
                    elbow.Matrix = Matrix3d.Displacement(edge.Target.Position.GetAsVector()) * Matrix3d.Rotation(bisectoroftwoedge.Angle - elbow.Parameters.BisectorAngle, Vector3d.ZAxis, elbow.Parameters.CornerPoint);
                    OutletDuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
                }
                else
                {
                    continue;
                }
            }
        }

        private void SetInOutHoses(string scenario)
        {
            if (scenario == "消防补风" || scenario == "消防排烟" || scenario == "消防加压送风")
            {
                return;
            }
            else
            {
                if (!FanInOutType.Contains("上进") && !FanInOutType.Contains("下进"))
                {
                    ThIfcDuctHose inlethose = CreateHose(InletOpening.Width, InletOpening.NormalAngle, scenario);
                    inlethose.Matrix = Matrix3d.Displacement(inlethose.Parameters.InsertPoint.GetVectorTo(InletOpening.OpingBasePoint)) * Matrix3d.Rotation(inlethose.Parameters.RotateAngle, Vector3d.ZAxis, inlethose.Parameters.InsertPoint);
                    InletDuctHoses.Add(inlethose);
                }
                if (!FanInOutType.Contains("下出") && !FanInOutType.Contains("上出"))
                {
                    ThIfcDuctHose outlethose = CreateHose(OutletOpening.Width, OutletOpening.NormalAngle, scenario);
                    outlethose.Matrix = Matrix3d.Displacement(outlethose.Parameters.InsertPoint.GetVectorTo(OutletOpening.OpingBasePoint)) * Matrix3d.Rotation(outlethose.Parameters.RotateAngle, Vector3d.ZAxis, outlethose.Parameters.InsertPoint);
                    OutletDuctHoses.Add(outlethose);
                }
            }
        }
        public void Create_in_out_hoses(string scenario, ThDbModelFan fan)
        {
            if (scenario == "消防补风" || scenario == "消防排烟" || scenario == "消防加压送风")
            {
                return;
            }
            else
            {
                var FanInOutType = fan.IntakeForm;
                var InletOpening = new FanOpeningInfo()
                {
                    Width = fan.FanInlet.Width,
                    Height = fan.FanInlet.Height,
                    NormalAngle = fan.FanInlet.Angle,
                    OpingBasePoint = fan.FanInletBasePoint
                };
                OutletOpening = new FanOpeningInfo()
                {
                    Width = fan.FanOutlet.Width,
                    Height = fan.FanOutlet.Height,
                    NormalAngle = fan.FanOutlet.Angle,
                    OpingBasePoint = fan.FanOutletBasePoint
                };
                if (!FanInOutType.Contains("上进") && !FanInOutType.Contains("下进"))
                {
                    ThIfcDuctHose inlethose = CreateHose(InletOpening.Width, InletOpening.NormalAngle, scenario);
                    inlethose.Matrix = Matrix3d.Displacement(inlethose.Parameters.InsertPoint.GetVectorTo(InletOpening.OpingBasePoint)) * Matrix3d.Rotation(inlethose.Parameters.RotateAngle, Vector3d.ZAxis, inlethose.Parameters.InsertPoint);
                    InletDuctHoses.Add(inlethose);
                }
                if (!FanInOutType.Contains("下出") && !FanInOutType.Contains("上出"))
                {
                    ThIfcDuctHose outlethose = CreateHose(OutletOpening.Width, OutletOpening.NormalAngle, scenario);
                    outlethose.Matrix = Matrix3d.Displacement(outlethose.Parameters.InsertPoint.GetVectorTo(OutletOpening.OpingBasePoint)) * Matrix3d.Rotation(outlethose.Parameters.RotateAngle, Vector3d.ZAxis, outlethose.Parameters.InsertPoint);
                    OutletDuctHoses.Add(outlethose);
                }
            }
        }
        private ThIfcDuctHose CreateHose(double width, double openingnormalangle,string scenario)
        {
            double openingnormalradian = openingnormalangle * Math.PI / 180;
            ThIfcDuctHoseParameters hoseparameters = new ThIfcDuctHoseParameters()
            {
                Width = width,
                Length = ThDuctUtils.GetHoseLength(scenario),
                RotateAngle = openingnormalradian < 0.5 * Math.PI ? 0.5 * Math.PI + openingnormalradian : openingnormalradian + 0.5 * Math.PI,
            };
            var hose = new ThIfcDuctHose(hoseparameters);
            hose.SetHoseInsertPoint();
            return hose;
        }

        private void DrawDuctInDWG(List<ThIfcDistributionElement> DuctSegments, 
                                   string geo_layer, string centerline_layer, string flg_layer, string textlayer, string textSize)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var Segment in DuctSegments)
                {
                    var mat = Segment.Matrix;
                    // 绘制风管
                    ThDuctPortsDrawService.Draw_lines(Segment.Representation, mat, geo_layer, out ObjectIdList geo_ids);
                    ThDuctPortsDrawService.Draw_lines(Segment.FlangeLine, mat, flg_layer, out ObjectIdList flg_ids);
                    ThDuctPortsDrawService.Draw_lines(Segment.Centerline, mat, centerline_layer, out ObjectIdList center_ids);
                    if (Segment.Centerline.Count == 1)
                    {
                        ThMEPHVACService.Get_duct_ports(Segment.Centerline[0] as Line, out List<Point3d> ports, out List<Point3d> ports_ext);
                        ThDuctPortsDrawService.Draw_ports(ports, ports_ext, mat, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                        //var duct_param = ThMEPHVACService.Create_duct_modify_param(Segment.Centerline, in_param.out_duct_size, 0, ObjectId.Null.Handle);
                        //ThDuctPortsRecoder.Create_duct_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, duct_param);
                    }
                    else if (Segment.Centerline.Count == 3)
                    {
                        ThMEPHVACService.Get_elbow_ports(Segment.Centerline, out List<Point3d> ports, out List<Point3d> ports_ext);
                        ThDuctPortsDrawService.Draw_ports(ports, ports_ext, mat, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                        var elbow_param = ThMEPHVACService.Create_special_modify_param("Elbow", mat, ObjectId.Null.Handle, Segment.FlangeLine, Segment.Centerline);
                        ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, elbow_param);
                    }
                    else
                        throw new NotImplementedException();
                    //插入管道信息标注
                    if (!Segment.InformationText.IsNull())
                    {
                        var textlayerId = CreateLayer(textlayer);
                        var textstyleId = CreateDuctTextStyle();
                        Segment.InformationText.LayerId = textlayerId;
                        Segment.InformationText.TextStyleId = textstyleId;
                        string s = Segment.InformationText.TextString;
                        string[] str = s.Split(' ');
                        if (str.Length != 2)
                            continue;
                        double dis = 2000;
                        if (textSize != null)
                        {
                            if (textSize == "1:100")
                                dis = 1300;
                            else if (textSize == "1:50")
                                dis = 700;
                        }
                        DBText t = Segment.InformationText.Clone() as DBText;
                        if (t == null)
                            return;
                        t.TextString = str[0];
                        t.TransformBy(Segment.Matrix);
                        db.ModelSpace.Add(t);
                        Segment.InformationText.TextString = str[1];
                        Vector2d v = TextVec[0];
                        TextVec.RemoveAt(0);
                        double factor = ((v.X < 0) || (Math.Abs(v.X) < 1e-9 && v.Y < 0)) ? -1 : 1;
                        Matrix3d dis_mat = Matrix3d.Displacement(new Vector3d(v.X, v.Y, 0) * factor * dis);
                        Segment.InformationText.TransformBy(dis_mat * Segment.Matrix);
                        db.ModelSpace.Add(Segment.InformationText);

                        Segment.InformationText.SetDatabaseDefaults();
                    }
                }
            }
        }

        private void DrawHoseInDWG(List<ThIfcDistributionElement> hoses, string modellayer)
        {
            foreach (ThIfcDuctHose hose in hoses)
            {
                ThValvesAndHolesInsertEngine.InsertHose(hose, modellayer);
                ThValvesAndHolesInsertEngine.EnableHoseLayer(hose, modellayer);
            }
        }

        private ObjectId CreateDuctCenterlineLayer(string centerlinelayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var obj = acadDatabase.Database.AddLayer(centerlinelayer);
                acadDatabase.Database.SetLayerColor(centerlinelayer, 252);
                acadDatabase.Database.UnOffLayer(centerlinelayer);
                acadDatabase.Database.UnLockLayer(centerlinelayer);
                acadDatabase.Database.UnPrintLayer(centerlinelayer);
                acadDatabase.Database.UnFrozenLayer(centerlinelayer);
                return obj;
            }
        }

        private ObjectId CreateLayer(string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(name);
                acadDatabase.Database.UnOffLayer(name);
                acadDatabase.Database.UnLockLayer(name);
                acadDatabase.Database.UnPrintLayer(name);
                acadDatabase.Database.UnFrozenLayer(name);
                return acadDatabase.Layers.ElementOrDefault(name).ObjectId;
            }
        }

        private ObjectId ByLayerLineTypeId()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return SymbolUtilityServices.GetLinetypeByLayerId(acadDatabase.Database);
            }
        }

        private ObjectId CreateDuctTextStyle()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportTextStyle(ThHvacCommon.DUCT_TEXT_STYLE);
                return acadDatabase.TextStyles.ElementOrDefault(ThHvacCommon.DUCT_TEXT_STYLE).ObjectId;
            }
        }

        private ObjectId CreateDuctCenterlinetype()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLinetype(ThHvacCommon.CENTERLINE_LINETYPE, true);
                return acadDatabase.Linetypes.ElementOrDefault(ThHvacCommon.CENTERLINE_LINETYPE).ObjectId;
            }
        }

    }
}
