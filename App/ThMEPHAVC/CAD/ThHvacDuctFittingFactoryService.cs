using System;
using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using Autodesk.AutoCAD.Colors;

namespace ThMEPHVAC.CAD
{
    public enum ReducingToFanJoinType
    {
        //小端与风机相连
        small = 1,
        //大端与风机相连
        big = 2,
        //小端与风机相连且圆转方
        small_circle = 3,
        //大端与风机相连且圆转方
        big_circle = 4
    }
    public class ThHvacDuctFittingFactoryService
    {
        public ThIfcDuctReducing CreateReducing(ThIfcDuctReducingParameters parameters, ReducingToFanJoinType jointype)
        {
            return new ThIfcDuctReducing(parameters)
            {
                Centerline = CreateReducingCenterLine(parameters),
                FlangeLine = CreateReducingFlangeLines(parameters, jointype),
                Representation = CreateReducingGeometries(parameters, jointype)
            };
        }

        public ThIfcDuctElbow CreateElbow(ThIfcDuctElbowParameters parameters)
        {
            return new ThIfcDuctElbow(parameters)
            {
                Centerline = CreateElbowCenterline(parameters),
                FlangeLine = CreateElbowFlangeline(parameters),
                Representation = CreateElbowGeometries(parameters)
            };
        }

        public ThIfcDuctCross CreateCross(ThIfcDuctCrossParameters parameters)
        {
            return new ThIfcDuctCross(parameters)
            {
                Representation = CreateCrossGeometries(parameters)
            };
        }

        public ThIfcDuctSegment CreateDuctSegment(ThIfcDuctSegmentParameters parameters, 
                                                  double ductangle, 
                                                  bool isupordownopening, 
                                                  bool islongestduct,
                                                  string elevation,
                                                  string textSize,
                                                  bool modify_text)
        {
            return new ThIfcDuctSegment(parameters)
            {
                Centerline = CreateDuctSegmentCenterLine(parameters),
                FlangeLine = CreateDuctFlangeGeometries(parameters, isupordownopening),
                Representation = CreateDuctSegmentGeometries(parameters),
                InformationText = CreateDuctInformation(parameters, ductangle, islongestduct, elevation, textSize, modify_text)
            };
        }

        private DBText CreateDuctInformation(ThIfcDuctSegmentParameters parameters, 
                                             double ductangle, 
                                             bool islongestduct, 
                                             string elevation,
                                             string textSize,
                                             bool modify_text)
        {
            if (!islongestduct)
            {
                return null;
            }
            else
            {
                string str;
                if (string.IsNullOrEmpty(elevation))
                {
                    str = $"{parameters.Width}x{parameters.Height} (h+X.XXm)";
                }
                else
                {
                    str = $"{parameters.Width}x{parameters.Height} (h{elevation}m)";
                    //if (modify_text)
                    //    str = $"(h{elevation}m) {parameters.Width}x{parameters.Height}";
                    //else
                    //    str = $"{parameters.Width}x{parameters.Height} (h{elevation}m)";

                }
                double h = 450;
                if (textSize != null)
                { 
                    if (textSize == "1:100")
                        h = 300;
                    else if (textSize == "1:50")
                        h = 150;
                }
                DBText infortext = new DBText()
                {
                    TextString = str,
                    Height = h,
                    WidthFactor = 0.7,
                    Color = Color.FromColorIndex(ColorMethod.ByLayer, (int)ColorIndex.BYLAYER),
                    HorizontalMode = TextHorizontalMode.TextLeft,
                    Oblique = 0,
                };
                if (ductangle > 0.5 * Math.PI && ductangle <= 1.5 * Math.PI)
                {
                    infortext.Rotation = Math.PI;
                }
                return infortext;
            }
        }

        public void DuctSegmentHandle(ThIfcDuctSegment ductsegment, double sourcecutdistance, double targetcutdistance)
        {
            //处理水平线
            foreach (Line line in ductsegment.Representation)
            {
                if (line.StartPoint.X < line.EndPoint.X)
                {
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                }
                else
                {
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                }
            }
            //处理竖直线
            foreach (Line line in ductsegment.FlangeLine)
            {
                if (line.StartPoint.X < 0)
                {
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                }
                else
                {
                    line.EndPoint = line.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                    line.StartPoint = line.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                }
            }
            //处理中心线
            foreach (Line centerline in ductsegment.Centerline)
            {
                if (centerline.StartPoint.X < centerline.EndPoint.X)
                {
                    centerline.StartPoint = centerline.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                    centerline.EndPoint = centerline.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                }
                else
                {
                    centerline.EndPoint = centerline.EndPoint.TransformBy(Matrix3d.Displacement(new Vector3d(sourcecutdistance, 0, 0)));
                    centerline.StartPoint = centerline.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(-targetcutdistance, 0, 0)));
                }

            }
            //设置文字信息的水平竖直位置
            if (!ductsegment.InformationText.IsNull())
            {
                var textbounding = ductsegment.InformationText.GeometricExtents;
                double textlength = textbounding.MaxPoint.X - textbounding.MinPoint.X;
                if (ductsegment.InformationText.Rotation == Math.PI)
                {
                    ductsegment.InformationText.Position = new Point3d(0.5 * textlength + 0.5 * (sourcecutdistance - targetcutdistance), -0.5 * ductsegment.Parameters.Width - 75, 0);
                }
                else
                {
                    ductsegment.InformationText.Position = new Point3d(-0.5 * textlength + 0.5 * (sourcecutdistance - targetcutdistance), 0.5 * ductsegment.Parameters.Width + 75, 0);
                }
            }
        }

        private double GetReducingLength(ThIfcDuctReducingParameters parameters)
        {
            double reducinglength = 0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(20 * Math.PI / 180);
            return reducinglength < 100 ? 100 : reducinglength > 1000 ? 1000 : reducinglength;
        }
        private DBObjectCollection CreateReducingCenterLine(ThIfcDuctReducingParameters parameters)
        {
            return new DBObjectCollection()
            {
                new Line()
                {
                    StartPoint = parameters.StartCenterPoint,
                    EndPoint = parameters.StartCenterPoint + new Vector3d(GetReducingLength(parameters),0,0)
                }
            };
        }
        private DBObjectCollection CreateReducingFlangeLines(ThIfcDuctReducingParameters parameters, ReducingToFanJoinType jointype)
        {

            Line smallendflange = new Line()
            {
                StartPoint = parameters.StartCenterPoint + new Vector3d(0, 0.5 * parameters.SmallEndWidth, 0) + new Vector3d(0, 45, 0),
                EndPoint = parameters.StartCenterPoint + new Vector3d(0, -0.5 * parameters.SmallEndWidth, 0) + new Vector3d(0, -45, 0),
            };
            Line bigendflange = new Line()
            {
                StartPoint = parameters.StartCenterPoint + new Vector3d(parameters.ReducingLength, 0.5 * parameters.BigEndWidth, 0) + new Vector3d(0, 45, 0),
                EndPoint = parameters.StartCenterPoint + new Vector3d(parameters.ReducingLength, -0.5 * parameters.BigEndWidth, 0) + new Vector3d(0, -45, 0),
            };
            var flangelines = new DBObjectCollection();
            switch (jointype)
            {
                case ReducingToFanJoinType.small:
                case ReducingToFanJoinType.small_circle:
                    flangelines.Add(bigendflange);
                    break;
                case ReducingToFanJoinType.big:
                case ReducingToFanJoinType.big_circle:
                    flangelines.Add(smallendflange);
                    break;
            }

            return flangelines;
        }
        private DBObjectCollection CreateReducingGeometries(ThIfcDuctReducingParameters parameters, ReducingToFanJoinType jointype)
        {
            //创建小端的端线
            Line smallendline = new Line()
            {
                StartPoint = parameters.StartCenterPoint + new Vector3d(0, -0.5 * parameters.SmallEndWidth, 0),
                EndPoint = parameters.StartCenterPoint + new Vector3d(0, 0.5 * parameters.SmallEndWidth, 0),
            };

            //创建小端的法兰线
            Line smallendleftflange = new Line()
            {
                StartPoint = smallendline.EndPoint + new Vector3d(0, 45, 0),
                EndPoint = smallendline.StartPoint + new Vector3d(0,-45,0),
            };

            //创建两侧侧壁轮廓线
            double reducinglength = 0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(20 * Math.PI / 180);
            Line leftsideline = new Line();
            Line rightsideline = new Line();
            leftsideline.StartPoint = smallendline.StartPoint;
            leftsideline.EndPoint = smallendline.StartPoint + new Vector3d(GetReducingLength(parameters), -0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0);
            rightsideline.StartPoint = smallendline.EndPoint;
            rightsideline.EndPoint = smallendline.EndPoint + new Vector3d(GetReducingLength(parameters), 0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0);

            //创建大端的端线
            Line bigendline = new Line()
            {
                StartPoint = leftsideline.EndPoint,
                EndPoint = rightsideline.EndPoint,
            };
            //创建大端的法兰线
            Line bigendleftflange = new Line()
            {
                StartPoint = bigendline.EndPoint + new Vector3d(0, 45, 0),
                EndPoint = bigendline.StartPoint + new Vector3d(0, -45, 0),
            };

            var reducinglines = new DBObjectCollection() { leftsideline, rightsideline };
            switch (jointype)
            {
                case ReducingToFanJoinType.small_circle:
                    reducinglines.Add(new Line(parameters.StartCenterPoint, leftsideline.EndPoint));
                    reducinglines.Add(new Line(parameters.StartCenterPoint, rightsideline.EndPoint));
                    break;
                case ReducingToFanJoinType.big_circle:
                    reducinglines.Add(new Line(parameters.StartCenterPoint + new Vector3d(GetReducingLength(parameters),0,0), smallendline.StartPoint));
                    reducinglines.Add(new Line(parameters.StartCenterPoint + new Vector3d(GetReducingLength(parameters),0,0), smallendline.EndPoint));
                    break;
                default:
                    break;
            }

            return reducinglines;
        }
        private DBObjectCollection CreateElbowCenterline(ThIfcDuctElbowParameters parameters)
        {
            var elbowengle = parameters.ElbowDegree * Math.PI / 180;
            var centerarc = new Arc(parameters.CenterPoint, 0.7 * parameters.PipeOpenWidth, 0, elbowengle);
            return new DBObjectCollection()
            {
                centerarc,
                new Line()
                {
                    StartPoint = centerarc.StartPoint,
                    EndPoint = centerarc.StartPoint.TransformBy(Matrix3d.Displacement(new Vector3d(0,-50,0))),
                },
                new Line()
                {
                    StartPoint = centerarc.EndPoint,
                    EndPoint = centerarc.EndPoint.TransformBy(Matrix3d.Rotation(centerarc.EndAngle + 0.5 * Math.PI,Vector3d.ZAxis,centerarc.EndPoint).PostMultiplyBy(Matrix3d.Displacement(new Vector3d(50,0,0)))),
                }
            };
        }
        private DBObjectCollection CreateElbowFlangeline(ThIfcDuctElbowParameters parameters)
        {
            var elbowengle = parameters.ElbowDegree * Math.PI / 180;
            //创建弯头内外侧圆弧
            Arc outerarc = new Arc(parameters.CenterPoint, 1.2 * parameters.PipeOpenWidth, 0, elbowengle);
            Arc innerarc = new Arc(parameters.CenterPoint, 0.2 * parameters.PipeOpenWidth, 0, elbowengle);
            //创建弯头两端的50mm延申段
            Line outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
            };
            Line innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
            };
            Line outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -50, 0),
            };
            Line innerstartextendline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = innerarc.StartPoint + new Vector3d(0, -50, 0),
            };

            //创建两端50mm延申段的端线
            Line startextendsealline = new Line()
            {
                StartPoint = outerstartextendline.EndPoint,
                EndPoint = innerstartextendline.EndPoint,
            };
            Line endextendsealline = new Line()
            {
                StartPoint = outerendextendline.EndPoint,
                EndPoint = innerendextendline.EndPoint,
            };

            Vector3d startsealvector = startextendsealline.StartPoint - startextendsealline.EndPoint;
            Vector3d starttranvector = startsealvector / startsealvector.Length * 45;
            Line startflangeline = new Line()
            {
                StartPoint = startextendsealline.StartPoint.TransformBy(Matrix3d.Displacement(starttranvector)),
                EndPoint = startextendsealline.EndPoint.TransformBy(Matrix3d.Displacement(starttranvector.Negate())),
            };

            Vector3d endsealvector = endextendsealline.StartPoint - endextendsealline.EndPoint;
            Vector3d endtranvector = endsealvector / endsealvector.Length * 45;
            Line endflangeline = new Line()
            {
                StartPoint = endextendsealline.StartPoint.TransformBy(Matrix3d.Displacement(endtranvector)),
                EndPoint = endextendsealline.EndPoint.TransformBy(Matrix3d.Displacement(endtranvector.Negate())),
            };

            return new DBObjectCollection()
            {
                startflangeline,
                endflangeline
            };
        }
        private DBObjectCollection CreateElbowGeometries(ThIfcDuctElbowParameters parameters)
        {
            var elbowengle = parameters.ElbowDegree * Math.PI / 180;
            //创建弯头内外侧圆弧
            Arc outerarc = new Arc(parameters.CenterPoint, 1.2 * parameters.PipeOpenWidth, 0, elbowengle);
            Arc innerarc = new Arc(parameters.CenterPoint, 0.2 * parameters.PipeOpenWidth, 0, elbowengle);
            //创建弯头两端的50mm延申段
            Line outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
            };
            Line innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
            };
            Line outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -50, 0),
            };
            Line innerstartextendline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = innerarc.StartPoint + new Vector3d(0, -50, 0),
            };

            ////创建弯头端线
            //Line startplaneline = new Line()
            //{
            //    StartPoint = innerarc.StartPoint,
            //    EndPoint = outerarc.StartPoint,
            //};

            //Line endplaneline = new Line()
            //{
            //    StartPoint = innerarc.EndPoint,
            //    EndPoint = outerarc.EndPoint,
            //};

            //创建两端50mm延申段的端线
            Line startextendsealline = new Line()
            {
                StartPoint = outerstartextendline.EndPoint,
                EndPoint = innerstartextendline.EndPoint,
            };
            Line endextendsealline = new Line()
            {
                StartPoint = outerendextendline.EndPoint,
                EndPoint = innerendextendline.EndPoint,
            };

            parameters.SingleLength = endextendsealline.GetPointAtDist(0.5* endextendsealline.Length).DistanceTo(parameters.CornerPoint);

            return new DBObjectCollection()
            {
                outerarc,
                innerarc,
                //startplaneline,
                //endplaneline,
                outerendextendline,
                innerendextendline,
                outerstartextendline,
                innerstartextendline,
            };
        }

        private DBObjectCollection CreateCrossGeometries(ThIfcDuctCrossParameters parameters)
        {
            //创建大端的端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5* parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
            };

            //创建主路小端的端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
            };

            //创建主路大端与侧路大端的圆弧过渡段
            Point3d bigEndCircleCenter = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth), -parameters.SideBigEndWidth, 0);
            Arc bigInnerArc = new Arc(bigEndCircleCenter, 0.5 * parameters.SideBigEndWidth, 0, 0.5 * Math.PI);
            //创建主路大端与侧路小端的圆弧过渡段
            Point3d smallEndCircleCenter = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth), -parameters.SideSmallEndWidth, 0);
            Arc smallInnerArc = new Arc(smallEndCircleCenter, 0.5 * parameters.SideSmallEndWidth, 0.5 * Math.PI, Math.PI);

            //创建主路大端圆弧过渡与大端端线端点连接线
            Line mainBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = bigInnerArc.StartPoint,
            };
            Line sideBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = smallInnerArc.EndPoint,
            };

            //创建侧路大端的端线
            Line sideBigEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth+ parameters.SideBigEndWidth) - 50,-0.5 * parameters.SideBigEndWidth,0),
                EndPoint = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth) - 50, 0.5 * parameters.SideBigEndWidth, 0),
            };

            //创建侧路大端50mm直管段
            Line sideBigEndOuterPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.StartPoint,
                EndPoint = sideBigEndLine.StartPoint + new Vector3d(50,0,0),
            };
            Line sideBigEndInnerPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.EndPoint,
                EndPoint = sideBigEndLine.EndPoint + new Vector3d(50, 0, 0),
            };

            //创建侧路大端的外侧圆弧过渡段
            //创建辅助线，确定侧路大端管线与主路小端管线的交点
            Ray sideBigEndAuxiliaryRay = new Ray()
            {
                BasePoint = parameters.Center + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 0, 0),
                SecondPoint = parameters.Center + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 10, 0)
            };
            Circle sideBigEndAuxiliaryCircle = new Circle() 
            {
                Center = bigEndCircleCenter,
                Radius = 1.5 * parameters.SideBigEndWidth
            };
            IntPtr ptr = new IntPtr();
            Point3dCollection bigEndIntersects = new Point3dCollection();
            sideBigEndAuxiliaryRay.IntersectWith(sideBigEndAuxiliaryCircle,Intersect.OnBothOperands, bigEndIntersects,ptr,ptr);
            Arc bigOutterArc = new Arc();
            Line mainSmallToSideBigArc = new Line();
            if (bigEndIntersects.Count != 0)
            {
                Point3d intersectpoint = bigEndIntersects[0];
                bigOutterArc.CreateArcSCE(intersectpoint, bigEndCircleCenter, sideBigEndOuterPipeLine.EndPoint);
                mainSmallToSideBigArc.StartPoint = mainSmallEndLine.StartPoint;
                mainSmallToSideBigArc.EndPoint = bigOutterArc.StartPoint;
            }

            //创建侧路小端的端线
            Line sideSmallEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, -0.5 * parameters.SideSmallEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, 0.5 * parameters.SideSmallEndWidth, 0),
            };

            //创建侧路小端50mm直管段
            Line sideSmallEndInnerPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.StartPoint,
                EndPoint = sideSmallEndLine.StartPoint + new Vector3d(-50, 0, 0),
            };
            Line sideSmallEndOuterPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.EndPoint,
                EndPoint = sideSmallEndLine.EndPoint + new Vector3d(-50, 0, 0),
            };

            //创建侧路小端的外侧圆弧过渡段
            //创建辅助线，确定侧路小端管线与主路小端管线的交点
            Ray sideSmallEndAuxiliaryRay = new Ray()
            {
                BasePoint = parameters.Center + new Vector3d(0.5 * parameters.mainSmallEndWidth, 0, 0),
                SecondPoint = parameters.Center + new Vector3d(0.5 * parameters.mainSmallEndWidth, 10, 0)
            };
            Circle sideSmallEndAuxiliaryCircle = new Circle()
            {
                Center = smallEndCircleCenter,
                Radius = 1.5 * parameters.SideSmallEndWidth
            };
            IntPtr ptr2 = new IntPtr();
            Point3dCollection smallEndIntersects = new Point3dCollection();
            sideSmallEndAuxiliaryRay.IntersectWith(sideSmallEndAuxiliaryCircle, Intersect.OnBothOperands, smallEndIntersects, ptr2, ptr2);
            Arc smallOutterArc = new Arc();
            Line mainSmallToSideSmallArc = new Line();
            if (smallEndIntersects.Count != 0)
            {
                Point3d intersectpoint = smallEndIntersects[0];
                smallOutterArc.CreateArcSCE(sideSmallEndOuterPipeLine.EndPoint, smallEndCircleCenter, intersectpoint);
                mainSmallToSideSmallArc.StartPoint = mainSmallEndLine.EndPoint;
                mainSmallToSideSmallArc.EndPoint = smallOutterArc.EndPoint;
            }

            return new DBObjectCollection()
            {
                mainBigEndLine,
                mainSmallEndLine,
                bigInnerArc,
                smallInnerArc,
                mainBigEndPipeLine,
                sideBigEndPipeLine,
                sideBigEndLine,
                sideBigEndOuterPipeLine,
                sideBigEndInnerPipeLine,
                bigOutterArc,
                mainSmallToSideBigArc,
                sideSmallEndLine,
                sideSmallEndOuterPipeLine,
                sideSmallEndInnerPipeLine,
                smallOutterArc,
                mainSmallToSideSmallArc,
            };

        }
        private DBObjectCollection CreateVerticalDuctGeometries(ThIfcDuctSegmentParameters parameters)
        {
            //绘制管道端线
            Line UpperLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Height / 2.0, parameters.Width / 2.0, 0),
                EndPoint = new Point3d(parameters.Height / 2.0, parameters.Width / 2.0, 0),
            };
            Line LowerLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Height / 2.0, -parameters.Width / 2.0, 0),
                EndPoint = new Point3d(parameters.Height / 2.0, -parameters.Width / 2.0, 0),
            };
            Line LeftLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Height / 2.0, -parameters.Width / 2.0, 0),
                EndPoint = new Point3d(-parameters.Height / 2.0, parameters.Width / 2.0, 0),
            };
            Line RightLine = new Line()
            {
                StartPoint = new Point3d(parameters.Height / 2.0, -parameters.Width / 2.0, 0),
                EndPoint = new Point3d(parameters.Height / 2.0, parameters.Width / 2.0, 0),
            };
            return new DBObjectCollection()
            {
                UpperLine,
                LowerLine,
                LeftLine,
                //RightLine
            };
        }

        private DBObjectCollection CreateDuctSegmentCenterLine(ThIfcDuctSegmentParameters parameters)
        {
            return new DBObjectCollection()
            {
                new Line(new Point3d(-parameters.Length / 2.0,0,0),new Point3d(parameters.Length / 2.0,0,0)),
            };
        }
        private DBObjectCollection CreateDuctFlangeGeometries(ThIfcDuctSegmentParameters parameters, bool isupordownopening)
        {
            Line leftflange = new Line()
            {
                StartPoint = new Point3d(-parameters.Length / 2.0, 0.5 * parameters.Width, 0),
                EndPoint = new Point3d(-parameters.Length / 2.0, -0.5 * parameters.Width, 0),
            };

            Line rightflange = new Line()
            {
                StartPoint = new Point3d(parameters.Length / 2.0, 0.5 * parameters.Width, 0),
                EndPoint = new Point3d(parameters.Length / 2.0, -0.5 * parameters.Width, 0),
            };
            //if (isupordownopening)
            //{
            //    return new DBObjectCollection()
            //    {
            //        rightflange,
            //    };
            //}
            return new DBObjectCollection()
            {
                rightflange,
                leftflange,
            };
        }
        private DBObjectCollection CreateDuctSegmentGeometries(ThIfcDuctSegmentParameters parameters)
        {
            //绘制辅助中心线
            Line auxiliaryCenterLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Length / 2.0, 0, 0),
                EndPoint = new Point3d(parameters.Length / 2.0, 0, 0),
            };

            //偏移出管轮廓线
            var ductUpperLineCollection = auxiliaryCenterLine.GetOffsetCurves(0.5 * parameters.Width);
            var ductBelowLineCollection = auxiliaryCenterLine.GetOffsetCurves(-0.5 * parameters.Width);
            Line ductUpperLine = (Line)ductUpperLineCollection[0];
            Line ductBelowLine = (Line)ductBelowLineCollection[0];

            return new DBObjectCollection()
            {
                ductUpperLine,
                ductBelowLine,
            };
        }
    }
}
