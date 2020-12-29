using System;
using QuickGraph;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.Duct;
using TianHua.Publics.BaseCode;
using Linq2Acad;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC.CAD
{
    public class FanOpeningInfo
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double NormalAngle { get; set; }
        public Point3d OpingBasePoint { get; set; }
    }
    public class InOutDuctPositionInfo
    {
        public string WorkingScenario { get; set; }
        public string InnerRoomDuctType { get; set; }
        public string OuterRoomDuctType { get; set; }
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
        public List<ThIfcDistributionElement> DuctReducings { get; set; }
        public List<ThIfcDistributionElement> DuctElbows { get; set; }
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
            DuctReducings = new List<ThIfcDistributionElement>();
            DuctElbows = new List<ThIfcDistributionElement>();
            SetInletElbows();
            SetOutletElbows();
            SetInletDucts();
            SetOutletDucts();
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            DrawDuctInDWG(InletDuctSegments, ductLayer);
            DrawDuctInDWG(OutletDuctSegments, ductLayer);
            DrawDuctInDWG(DuctReducings, ductLayer);
            DrawDuctInDWG(DuctElbows, ductLayer);
        }

        public void SetInletOutletSize(string scenario, string inouttype, string innerromeductinfo, string outerromeductinfo)
        {
            var inOutDuctPositionInfo = ReadWord(ThCADCommon.DuctInOutMapping());
            var ductPositionObjs = FuncJson.Deserialize<List<InOutDuctPositionInfo>>(inOutDuctPositionInfo);

            var innerRomDuctPosition = ductPositionObjs.First(d=>d.WorkingScenario == scenario).InnerRoomDuctType;
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

        public void SetInletDucts()
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            //对于进口为上进的，首先需要画出管口俯视图
            if (FanInOutType.Contains("上进"))
            {
                var DuctParameters = new ThIfcDuctSegmentParameters()
                {
                    Width = InletDuctWidth,
                    Height = InletDuctHeight,
                    Length = 0
                };
                var ductSegment = ductFittingFactoryService.CreateVerticalDuctSegment(DuctParameters);
                ductSegment.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector());
                InletDuctSegments.Add(ductSegment);
            }

            else if(InletOpening.Width != InletDuctWidth)
            {
                //仅对于非上进的风机画变径，对于上进的，不画变径
                double reducingBigEndWidth = Math.Max(InletOpening.Width, InletDuctWidth);
                double reducingSmallEndWidth = Math.Min(InletOpening.Width, InletDuctWidth);

                var ductReducingParameters = new ThIfcDuctReducingParameters()
                {
                    BigEndWidth = reducingBigEndWidth,
                    SmallEndWidth = reducingSmallEndWidth
                };
                var reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters);
                //若风机进口宽度比管道宽度小，即风机进口对应变径的小端
                if (InletOpening.Width < InletDuctWidth)
                {
                    double rotationangle = InletOpening.NormalAngle * Math.PI / 180;
                    reducing.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                }
                //若风机进口宽度比管道宽度大，即风机进口对应变径的大端
                else
                {
                    double rotationangle = InletOpening.NormalAngle * Math.PI / 180 - Math.PI;
                    reducing.Matrix = Matrix3d.Displacement(InletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)) * Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
                }
                DuctReducings.Add(reducing);

                var firstvertex = InletCenterLineGraph.Vertices.Where(v => v.IsStartVertexOfGraph).FirstOrDefault();
                if (!firstvertex.IsNull())
                {
                    InletCenterLineGraph.OutEdges(firstvertex).FirstOrDefault().SourceShrink = reducing.Parameters.ReducingLength;
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
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters);
                ductFittingFactoryService.DuctSegmentHandle(ductSegment.Representation, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                Vector2d edgevector = new Vector2d(ductgraphedge.Target.Position.X - ductgraphedge.Source.Position.X, ductgraphedge.Target.Position.Y - ductgraphedge.Source.Position.Y);
                double rotateangle = edgevector.Angle;
                Point3d centerpoint = new Point3d(0.5 * (ductgraphedge.Source.Position.X + ductgraphedge.Target.Position.X), 0.5 * (ductgraphedge.Source.Position.Y + ductgraphedge.Target.Position.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                InletDuctSegments.Add(ductSegment);
            }
        }

        public void SetOutletDucts()
        {
            var ductFittingFactoryService = new ThHvacDuctFittingFactoryService();

            //对于出口为上出或下出的，首先需要画出管口俯视图
            if (FanInOutType.Contains("上出") || FanInOutType.Contains("下出"))
            {
                var DuctParameters = new ThIfcDuctSegmentParameters()
                {
                    Width = OutletDuctWidth,
                    Height = OutletDuctHeight,
                    Length = 0
                };
                var ductSegment = ductFittingFactoryService.CreateVerticalDuctSegment(DuctParameters);
                ductSegment.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector());
                OutletDuctSegments.Add(ductSegment);
            }

            else if (OutletOpening.Width != OutletDuctWidth)
            {
                //仅对于非上进的风机画变径，对于上进的，不画变径
                double reducingBigEndWidth = Math.Max(OutletOpening.Width, OutletDuctWidth);
                double reducingSmallEndWidth = Math.Min(OutletOpening.Width, OutletDuctWidth);

                var ductReducingParameters = new ThIfcDuctReducingParameters()
                {
                    BigEndWidth = reducingBigEndWidth,
                    SmallEndWidth = reducingSmallEndWidth
                };
                var reducing = ductFittingFactoryService.CreateReducing(ductReducingParameters);
                //若风机出口宽度比管道宽度小，即风机出口对应变径的小端
                if (OutletOpening.Width < OutletDuctWidth)
                {
                    double rotationangle = OutletOpening.NormalAngle * Math.PI / 180;
                    reducing.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                }
                //若风机出口宽度比管道宽度大，即风机出口对应变径的大端
                else
                {
                    double rotationangle = OutletOpening.NormalAngle * Math.PI / 180 - Math.PI;
                    reducing.Matrix = Matrix3d.Displacement(OutletOpening.OpingBasePoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0)) * Matrix3d.Displacement(new Vector3d(-reducing.Parameters.ReducingLength, 0, 0));
                }
                DuctReducings.Add(reducing);

                var firstvertex = OutletCenterLineGraph.Vertices.Where(v => v.IsStartVertexOfGraph).FirstOrDefault();
                if (!firstvertex.IsNull())
                {
                    OutletCenterLineGraph.OutEdges(firstvertex).FirstOrDefault().SourceShrink = reducing.Parameters.ReducingLength;
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
                var ductSegment = ductFittingFactoryService.CreateDuctSegment(DuctParameters);
                ductFittingFactoryService.DuctSegmentHandle(ductSegment.Representation, ductgraphedge.SourceShrink, ductgraphedge.TargetShrink);
                Vector2d edgevector = new Vector2d(ductgraphedge.Target.Position.X - ductgraphedge.Source.Position.X, ductgraphedge.Target.Position.Y - ductgraphedge.Source.Position.Y);
                double rotateangle = edgevector.Angle;
                Point3d centerpoint = new Point3d(0.5 * (ductgraphedge.Source.Position.X + ductgraphedge.Target.Position.X), 0.5 * (ductgraphedge.Source.Position.Y + ductgraphedge.Target.Position.Y), 0);
                ductSegment.Matrix = Matrix3d.Displacement(centerpoint.GetAsVector()) * Matrix3d.Rotation(rotateangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
                OutletDuctSegments.Add(ductSegment);
            }
        }

        public void SetInletElbows()
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
                    DuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
                }
            }
        }

        public void SetOutletElbows()
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
                    DuctElbows.Add(elbow);

                    outedge.SourceShrink = elbow.Parameters.SingleLength;
                    edge.TargetShrink = elbow.Parameters.SingleLength;
                }
            }
        }

        private void DrawDuctInDWG(List<ThIfcDistributionElement> DuctSegments, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var Segment in DuctSegments)
                {
                    foreach (DBObject dbobj in Segment.Representation)
                    {
                        var dbent = dbobj as Entity;
                        dbent.Layer = layer;
                        dbent.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbent);
                    }
                }
            }
        }

        public string ReadWord(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = new StreamReader(_Path, Encoding.UTF8))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;

            }
        }

    }
}
