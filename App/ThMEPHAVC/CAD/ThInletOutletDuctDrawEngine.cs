﻿using System;
using Linq2Acad;
using DotNetARX;
using QuickGraph;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.IO;
using ThMEPHVAC.Duct;
using TianHua.Publics.BaseCode;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.CAD
{
    public class FanOpeningInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double NormalAngle { get; set; }
        public Point3d OpingBasePoint { get; set; }
    }
    public class ThInletOutletDuctDrawEngine
    {
        public FanOpeningInfo InletOpening { get; set; }
        public FanOpeningInfo OutletOpening { get; set; }
        public double InletDuctWidth { get; set; }
        public double OutletDuctWidth { get; set; }
        public double InletDuctHeight { get; set; }
        public double OutletDuctHeight { get; set; }
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
        public ThDuctEdge<ThDuctVertex> FirstInletEdge { get; set; }
        public AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> OutletCenterLineGraph { get; set; }
        public ThDuctEdge<ThDuctVertex> FirstOutletEdge { get; set; }
        public ThInletOutletDuctDrawEngine(ThDbModelFan fanmodel, 
            string innerductinfo, 
            string outerductinfo,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> inletcenterlinegraph,
            AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> outletcenterlinegraph
        )
        {
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
            SetInletOutletSize(fanmodel.FanScenario, fanmodel.IntakeForm, innerductinfo, outerductinfo);

            InletDuctSegments = new List<ThIfcDistributionElement>();
            OutletDuctSegments = new List<ThIfcDistributionElement>();
            InletDuctReducings = new List<ThIfcDistributionElement>();
            OutletDuctReducings = new List<ThIfcDistributionElement>();
            InletDuctElbows = new List<ThIfcDistributionElement>();
            OutletDuctElbows = new List<ThIfcDistributionElement>();
            InletDuctHoses = new List<ThIfcDistributionElement>();
            OutletDuctHoses = new List<ThIfcDistributionElement>();

            SetInletElbows();
            SetOutletElbows();
            SetInOutHoses(fanmodel.FanScenario);
            bool isAxial = fanmodel.Model.IsAXIALModel();
            SetInletDucts(fanmodel.FanScenario, isAxial);
            SetOutletDucts(fanmodel.FanScenario, isAxial);
        }

        public void RunInletDrawEngine(ThDbModelFan fanmodel)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);
            string centerLinerLayer = ThDuctUtils.DuctCenterLineLayerName(modelLayer);
            string ducttextlayer = ThDuctUtils.DuctTextLayerName(modelLayer);
            DrawDuctInDWG(InletDuctSegments, ductLayer, centerLinerLayer, ductLayer, ducttextlayer);
            DrawDuctInDWG(InletDuctReducings, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer);
            DrawDuctInDWG(InletDuctElbows, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer);
            DrawHoseInDWG(InletDuctHoses, modelLayer);
        }

        public void RunOutletDrawEngine(ThDbModelFan fanmodel)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);
            string centerLinerLayer = ThDuctUtils.DuctCenterLineLayerName(modelLayer);
            string ducttextlayer = ThDuctUtils.DuctTextLayerName(modelLayer);
            DrawDuctInDWG(OutletDuctSegments, ductLayer, centerLinerLayer, ductLayer, ducttextlayer);
            DrawDuctInDWG(OutletDuctReducings, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer);
            DrawDuctInDWG(OutletDuctElbows, ductLayer, centerLinerLayer, flangeLinerLayer, ducttextlayer);
            DrawHoseInDWG(OutletDuctHoses, modelLayer);
        }


        private void SetInletOutletSize(string scenario, string inouttype, string innerromeductinfo, string outerromeductinfo)
        {
            var jsonReader = new ThDuctInOutMappingJsonReader();
            var innerRomDuctPosition = jsonReader.Mappings.First(d=>d.WorkingScenario == scenario).InnerRoomDuctType;
            if (innerRomDuctPosition == "进风段")
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
        }

        private void SetInletDucts(string scenario,bool isaxial)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            bool isUpOrDownOpening = FanInOutType.Contains("上进") || FanInOutType.Contains("下进");
            //对于进口为上进的，首先需要画出管口俯视图
            //if (isUpOrDownOpening)
            //{
            //    var DuctParameters = new ThIfcDuctSegmentParameters()
            //    {
            //        Width = Math.Max(InletDuctWidth, InletOpening.Width),
            //        Height = Math.Max(InletDuctHeight, InletOpening.Height),
            //        Length = 0
            //    };
            //    double rotateangle = InletOpening.NormalAngle * Math.PI / 180 + 0.5 * Math.PI;
            //    if (InletCenterLineGraph.Edges.Count() != 0)
            //    {
            //        var firstinletedge = InletCenterLineGraph.Edges.First(e => e.Source.IsStartVertexOfGraph);
            //        var firstcenterlinevector = firstinletedge.Target.Position - firstinletedge.Source.Position;
            //        double firstcenterlineangle = firstcenterlinevector.AngleOnPlane(new Plane(firstinletedge.Target.Position, Vector3d.ZAxis));
            //        rotateangle = firstcenterlineangle < Math.PI ? Vector3d.XAxis.GetAngleTo(firstcenterlinevector) : 2 * Math.PI - Vector3d.XAxis.GetAngleTo(firstcenterlinevector);
            //    }

            //    var ductSegment = ductFittingFactoryService.CreateVerticalDuctSegment(DuctParameters);
            //    ductSegment.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, Point3d.Origin);
            //    InletDuctSegments.Add(ductSegment);
            //}

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
                    //reducing.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)) * Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
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

            foreach (var ductgraphedge in InletCenterLineGraph.Edges)
            {
                var DuctParameters = new ThIfcDuctSegmentParameters()
                {
                    Width = InletDuctWidth,
                    Height = InletDuctHeight,
                    Length = ductgraphedge.EdgeLength
                };
                Vector2d edgevector = new Vector2d(ductgraphedge.Target.Position.X - ductgraphedge.Source.Position.X, ductgraphedge.Target.Position.Y - ductgraphedge.Source.Position.Y);
                double rotateangle = edgevector.Angle;
                bool islongestduct = InletCenterLineGraph.Edges.Max(e => e.EdgeLength) == ductgraphedge.EdgeLength;
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters, rotateangle, isUpOrDownOpening, islongestduct);
                if (isUpOrDownOpening)
                {
                    ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink - 100 - 0.5 * InletOpening.Height, ductgraphedge.TargetShrink);
                }
                else
                {
                    ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                }
                Point3d centerpoint = new Point3d(0.5 * (ductgraphedge.Source.Position.X + ductgraphedge.Target.Position.X), 0.5 * (ductgraphedge.Source.Position.Y + ductgraphedge.Target.Position.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                InletDuctSegments.Add(ductSegment);
            }
        }

        private void SetOutletDucts(string scenario,bool isaxial)
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();
            bool isUpOrDownOpening = FanInOutType.Contains("上出") || FanInOutType.Contains("下出");
            //对于出口为上出或下出的，首先需要画出管口俯视图
            //if (isUpOrDownOpening)
            //{
            //    var DuctParameters = new ThIfcDuctSegmentParameters()
            //    {
            //        Width = Math.Max(OutletDuctWidth, OutletOpening.Width),
            //        Height = Math.Max(OutletDuctHeight, OutletOpening.Height),
            //        Length = 0
            //    };

            //    double rotateangle = OutletOpening.NormalAngle * Math.PI / 180 + 0.5 * Math.PI;
            //    if (OutletCenterLineGraph.Edges.Count() != 0)
            //    {
            //        var firstoutletedge = OutletCenterLineGraph.Edges.First(e => e.Source.IsStartVertexOfGraph);
            //        var firstcenterlinevector = firstoutletedge.Target.Position - firstoutletedge.Source.Position;
            //        double firstcenterlineangle = firstcenterlinevector.AngleOnPlane(new Plane(firstoutletedge.Target.Position, Vector3d.ZAxis));
            //        rotateangle = firstcenterlineangle < Math.PI ? Vector3d.XAxis.GetAngleTo(firstcenterlinevector) : 2 * Math.PI - Vector3d.XAxis.GetAngleTo(firstcenterlinevector);
            //    }

            //    var ductSegment = ductFittingFactoryService.CreateVerticalDuctSegment(DuctParameters);
            //    ductSegment.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, Point3d.Origin);
            //    OutletDuctSegments.Add(ductSegment);
            //}

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
                //var reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters);
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
                    //reducing.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
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
                    //reducing.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)) * Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
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

            foreach (var ductgraphedge in OutletCenterLineGraph.Edges)
            {
                var DuctParameters = new ThIfcDuctSegmentParameters()
                {
                    Width = OutletDuctWidth,
                    Height = OutletDuctHeight,
                    Length = ductgraphedge.EdgeLength
                };
                Vector2d edgevector = new Vector2d(ductgraphedge.Target.Position.X - ductgraphedge.Source.Position.X, ductgraphedge.Target.Position.Y - ductgraphedge.Source.Position.Y);
                double rotateangle = edgevector.Angle;
                bool islongestduct = OutletCenterLineGraph.Edges.Max(e => e.EdgeLength) == ductgraphedge.EdgeLength;
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters, rotateangle, isUpOrDownOpening, islongestduct);
                if (isUpOrDownOpening)
                {
                    ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink - 100 - 0.5 * OutletOpening.Height, ductgraphedge.TargetShrink);
                }
                else
                {
                    ductFittingFactoryService.DuctSegmentHandle(ductSegment, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                }

                Point3d centerpoint = new Point3d(0.5 * (ductgraphedge.Source.Position.X + ductgraphedge.Target.Position.X), 0.5 * (ductgraphedge.Source.Position.Y + ductgraphedge.Target.Position.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                OutletDuctSegments.Add(ductSegment);
            }
        }

        private void SetInletElbows()
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
                    var elbowParameters = new ThIfcDuctElbowParameters()
                    {
                        ElbowDegree = edgeangle,
                        PipeOpenWidth = InletDuctWidth,
                    };
                    var elbow = ductFittingFactoryService.CreateElbow(elbowParameters);
                    elbow.Matrix = Matrix3d.Displacement(edge.Target.Position.GetAsVector()) * Matrix3d.Rotation(bisectoroftwoedge.Angle - elbow.Parameters.BisectorAngle, Vector3d.ZAxis, elbow.Parameters.CornerPoint);
                    InletDuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
                }
            }
        }

        private void SetOutletElbows()
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            foreach (var edge in OutletCenterLineGraph.Edges)
            {
                if (OutletCenterLineGraph.OutDegree(edge.Target) != 1)
                {
                    continue;
                }
                else
                {
                    Vector2d invector = new Vector2d(edge.Source.Position.X - edge.Target.Position.X, edge.Source.Position.Y - edge.Target.Position.Y);
                    var outedge = OutletCenterLineGraph.OutEdges(edge.Target).First();
                    Vector2d outvector = new Vector2d(outedge.Target.Position.X - outedge.Source.Position.X, outedge.Target.Position.Y - outedge.Source.Position.Y);
                    var edgeangle = 180 - Math.Acos(invector.DotProduct(outvector) / (invector.Length * outvector.Length)) * 180 / Math.PI;
                    Vector2d bisectoroftwoedge = invector / invector.Length + outvector / outvector.Length;
                    var elbowParameters = new ThIfcDuctElbowParameters()
                    {
                        ElbowDegree = edgeangle,
                        PipeOpenWidth = OutletDuctWidth,
                    };
                    var elbow = ductFittingFactoryService.CreateElbow(elbowParameters);
                    elbow.Matrix = Matrix3d.Displacement(edge.Target.Position.GetAsVector()) * Matrix3d.Rotation(bisectoroftwoedge.Angle - elbow.Parameters.BisectorAngle, Vector3d.ZAxis, elbow.Parameters.CornerPoint);
                    OutletDuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
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

        private void DrawDuctInDWG(List<ThIfcDistributionElement> DuctSegments, string ductlayer, string centerlinelayer, string flangelayer, string textlayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var Segment in DuctSegments)
                {
                    // 绘制风管
                    var linetypeId = ByLayerLineTypeId();
                    var layerId = CreateLayer(ductlayer);
                    foreach (Curve dbobj in Segment.Representation)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }

                    // 绘制风管中心线
                    linetypeId = CreateDuctCenterlinetype();
                    layerId = CreateDuctCenterlineLayer(centerlinelayer);
                    foreach (Curve dbobj in Segment.Centerline)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }

                    // 绘制法兰线
                    linetypeId = ByLayerLineTypeId();
                    layerId = CreateLayer(flangelayer);
                    foreach (Curve dbobj in Segment.FlangeLine)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }

                    //插入管道信息标注
                    if (!Segment.InformationText.IsNull())
                    {
                        var textlayerId = CreateLayer(textlayer);
                        var textstyleId = CreateDuctTextStyle();
                        Segment.InformationText.LayerId = textlayerId;
                        Segment.InformationText.TextStyleId = textstyleId;
                        Segment.InformationText.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(Segment.InformationText);
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
                acadDatabase.Database.ImportLayer(name, false);
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
                acadDatabase.Database.ImportTextStyle(ThHvacCommon.DUCT_TEXT_STYLE, false);
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
