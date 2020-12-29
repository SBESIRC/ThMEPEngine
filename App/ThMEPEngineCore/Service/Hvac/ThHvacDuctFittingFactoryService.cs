using System;
using System.Linq;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using System.Collections.Generic;

namespace ThMEPEngineCore.Service.Hvac
{
    public class ThHvacDuctFittingFactoryService
    {
        public ThIfcDuctReducing CreateReducing(ThIfcDuctReducingParameters parameters)
        {
            return new ThIfcDuctReducing(parameters)
            {
                Representation = CreateReducingGeometries(parameters)
            };
        }

        public ThIfcDuctElbow CreateElbow(ThIfcDuctElbowParameters parameters)
        {
            return new ThIfcDuctElbow(parameters)
            {
                Representation = CreateElbowGeometries(parameters)
            };
        }

        public ThIfcDuctTee CreateTee(ThIfcDuctTeeParameters parameters)
        {
            return new ThIfcDuctTee(parameters)
            {
                Representation = CreateTeeGeometries(parameters)
            };
        }

        public ThIfcDuctCross CreateCross(ThIfcDuctCrossParameters parameters)
        {
            return new ThIfcDuctCross(parameters)
            {
                Representation = CreateCrossGeometries(parameters)
            };
        }

        public ThIfcDuctSegment CreateDuctSegment(ThIfcDuctSegmentParameters parameters)
        {
            return new ThIfcDuctSegment(parameters)
            {
                Representation = CreateDuctSegmentGeometries(parameters)
            };
        }

        public ThIfcDuctSegment CreateVerticalDuctSegment(ThIfcDuctSegmentParameters parameters)
        {
            return new ThIfcDuctSegment(parameters)
            {
                Representation = CreateverticalDuctGeometries(parameters)
            };
        }

        public void DuctSegmentHandle(DBObjectCollection originduct, double sourcecutdistance, double targetcutdistance)
        {
            List<string> a = new List<string>();
            var groupedlines = originduct.Cast<Line>().GroupBy(l => Math.Abs(l.Angle) < 0.01 || Math.Abs(l.Angle - Math.PI) < 0.01).ToList();

            foreach (var linegroup in groupedlines)
            {
                //处理水平线
                if (Math.Abs(linegroup.First().Angle) < 0.01 || Math.Abs(linegroup.First().Angle - Math.PI) < 0.01 )
                {
                    foreach (var line in linegroup)
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
                }
                //处理竖直线
                else
                {
                    foreach (var line in linegroup)
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
                }
            }
        }


        private DBObjectCollection CreateReducingGeometries(ThIfcDuctReducingParameters parameters)
        {
            //创建小端的端线
            Line smallendline = new Line()
            {
                StartPoint = parameters.StartCenterPoint + new Vector3d(0, -0.5 * parameters.SmallEndWidth, 0),
                EndPoint = parameters.StartCenterPoint + new Vector3d(0, 0.5 * parameters.SmallEndWidth, 0),
                ColorIndex = 2
            };

            //创建两侧侧壁轮廓线
            Line leftsideline = new Line()
            {
                StartPoint = smallendline.StartPoint,
                EndPoint = smallendline.StartPoint + new Vector3d(0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(15 * Math.PI / 180), -0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0),
                ColorIndex = 1
            };
            Line rightsideline = new Line()
            {
                StartPoint = smallendline.EndPoint,
                EndPoint = smallendline.EndPoint + new Vector3d(0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(15 * Math.PI / 180), 0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0),
                ColorIndex = 1
            };

            //创建大端的端线
            Line bigendline = new Line()
            {
                StartPoint = leftsideline.EndPoint,
                EndPoint = rightsideline.EndPoint,
                ColorIndex = 2
            };

            return new DBObjectCollection()
            {
                    smallendline,
                    leftsideline,
                    rightsideline,
                    bigendline
            };
        }

        private DBObjectCollection CreateElbowGeometries(ThIfcDuctElbowParameters parameters)
        {
            var elbowengle = parameters.ElbowDegree * Math.PI / 180;
            //创建弯头内外侧圆弧
            Arc outerarc = new Arc(parameters.CenterPoint, 1.5 * parameters.PipeOpenWidth, 0, elbowengle)
            {
                ColorIndex = 1
            };
            Arc innerarc = new Arc(parameters.CenterPoint, 0.5 * parameters.PipeOpenWidth, 0, elbowengle)
            {
                ColorIndex = 1
            };
            //创建弯头两端的50mm延申段
            Line outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
                ColorIndex = 1
            };
            Line innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
                ColorIndex = 1
            };
            Line outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -50, 0),
                ColorIndex = 1
            };
            Line innerstartextendline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = innerarc.StartPoint + new Vector3d(0, -50, 0),
                ColorIndex = 1
            };

            //创建弯头中心线圆弧
            Arc centerarc = new Arc(parameters.CenterPoint, parameters.PipeOpenWidth, 0, elbowengle)
            {
                ColorIndex = 5
            };

            //创建弯头端线
            Line startplaneline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = outerarc.StartPoint,
                ColorIndex = 2
            };

            Line endplaneline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = outerarc.EndPoint,
                ColorIndex = 2
            };

            //创建两端50mm延申段的端线
            Line startextendsealline = new Line()
            {
                StartPoint = outerstartextendline.EndPoint,
                EndPoint = innerstartextendline.EndPoint,
                ColorIndex = 2
            };

            Line endextendsealline = new Line()
            {
                StartPoint = outerendextendline.EndPoint,
                EndPoint = innerendextendline.EndPoint,
                ColorIndex = 2
            };

            parameters.SingleLength = endextendsealline.GetPointAtDist(0.5* endextendsealline.Length).DistanceTo(parameters.CornerPoint);

            return new DBObjectCollection()
            {
                outerarc,
                centerarc,
                innerarc,
                //startplaneline,
                //endplaneline,
                outerendextendline,
                innerendextendline,
                outerstartextendline,
                innerstartextendline,
                startextendsealline,
                endextendsealline
            };
        }

        private DBObjectCollection CreateTeeGeometries(ThIfcDuctTeeParameters parameters)
        {
            //创建支路端线
            Line branchEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter + parameters.BranchDiameter)+50, 0.5*parameters.BranchDiameter, 0),
                EndPoint = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter + parameters.BranchDiameter) +50, -0.5*parameters.BranchDiameter, 0),
                ColorIndex = 2
            };

            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5*parameters.MainSmallDiameter, 0.5 * parameters.BranchDiameter + 100,0),
                EndPoint = parameters.CenterPoint + new Vector3d(-0.5*parameters.MainSmallDiameter, 0.5 * parameters.BranchDiameter + 100,0),
                ColorIndex = 2
            };

            //创建主路大端端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5 * parameters.MainBigDiameter, -parameters.BranchDiameter - 50, 0),
                EndPoint = parameters.CenterPoint + new Vector3d(-0.5 * parameters.MainBigDiameter, -parameters.BranchDiameter - 50, 0),
                ColorIndex = 2
            };

            //创建支路50mm直管段
            Line branchUpStraightLine = new Line()
            {
                StartPoint = branchEndLine.StartPoint,
                EndPoint = branchEndLine.StartPoint + new Vector3d(-50,0,0),
                ColorIndex = 1
            };
            Line branchBelowStraightLine = new Line()
            {
                StartPoint = branchEndLine.EndPoint,
                EndPoint = branchEndLine.EndPoint + new Vector3d(-50, 0, 0),
                ColorIndex = 1
            };

            //创建支路下侧圆弧过渡段
            Point3d circleCenter = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter+parameters.BranchDiameter), -parameters.BranchDiameter, 0);
            Arc branchInnerArc = new Arc(circleCenter, 0.5 * parameters.BranchDiameter, 0.5 * Math.PI, Math.PI)
            {
                ColorIndex = 1
            };

            //创建支路上侧圆弧过渡段
            //首先创建主路上端小管道的内侧线作为辅助线以便于后续计算圆弧交点
            Ray branchAuxiliaryRay = new Ray()
            {
                BasePoint = mainSmallEndLine.StartPoint,
                SecondPoint = mainSmallEndLine.StartPoint + new Vector3d(0, -5000, 0)
            };
            Circle branchAuxiliaryCircle = new Circle()
            {
                Center = circleCenter,
                Radius = 1.5*parameters.BranchDiameter
            };
            Point3dCollection Intersectpoints = new Point3dCollection();
            IntPtr ptr = new IntPtr();
            branchAuxiliaryRay.IntersectWith(branchAuxiliaryCircle, Intersect.OnBothOperands, Intersectpoints, ptr, ptr);
            Arc branchOuterArc = new Arc();
            if (Intersectpoints.Count != 0)
            {
                Point3d Intersectpointinarc = Intersectpoints[0];
                foreach (Point3d point in Intersectpoints)
                {
                    if (point.Y > Intersectpointinarc.Y)
                    {
                        Intersectpointinarc = point;
                    }
                }
                branchOuterArc.CreateArcSCE(branchUpStraightLine.EndPoint, circleCenter, Intersectpointinarc);
                branchOuterArc.ColorIndex = 1;
            }

            //创建主路外侧管线
            Line outerStraightLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = mainBigEndLine.EndPoint + new Vector3d(0,50,0),
                ColorIndex = 1
            };
            Line outerObliqueLine = new Line()
            {
                StartPoint = outerStraightLine.EndPoint,
                EndPoint = mainSmallEndLine.EndPoint,
                ColorIndex = 1
            };

            //创建主路内侧管线
            Line innerUpLine = new Line()
            {
                StartPoint = mainSmallEndLine.StartPoint,
                EndPoint = branchOuterArc.EndPoint,
                ColorIndex = 1
            };
            Line innerBelowLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = branchInnerArc.EndPoint,
                ColorIndex = 1
            };


            return new DBObjectCollection()
            {
                branchEndLine,
                mainSmallEndLine,
                mainBigEndLine,
                branchUpStraightLine,
                branchBelowStraightLine,
                branchInnerArc,
                branchOuterArc,
                outerStraightLine,
                outerObliqueLine,
                innerUpLine,
                innerBelowLine
            };
        }

        private DBObjectCollection CreateCrossGeometries(ThIfcDuctCrossParameters parameters)
        {
            //创建大端的端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5* parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
                ColorIndex = 2
            };

            //创建主路小端的端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
                ColorIndex = 2
            };


            //创建主路大端与侧路大端的圆弧过渡段
            Point3d bigEndCircleCenter = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth), -parameters.SideBigEndWidth, 0);
            Arc bigInnerArc = new Arc(bigEndCircleCenter, 0.5 * parameters.SideBigEndWidth, 0, 0.5 * Math.PI)
            {
                ColorIndex = 1
            };
            //创建主路大端与侧路小端的圆弧过渡段
            Point3d smallEndCircleCenter = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth), -parameters.SideSmallEndWidth, 0);
            Arc smallInnerArc = new Arc(smallEndCircleCenter, 0.5 * parameters.SideSmallEndWidth, 0.5 * Math.PI, Math.PI)
            {
                ColorIndex = 1
            };

            //创建主路大端圆弧过渡与大端端线端点连接线
            Line mainBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = bigInnerArc.StartPoint,
                ColorIndex = 1
            };
            Line sideBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = smallInnerArc.EndPoint,
                ColorIndex = 1
            };

            //创建侧路大端的端线
            Line sideBigEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth+ parameters.SideBigEndWidth) - 50,-0.5 * parameters.SideBigEndWidth,0),
                EndPoint = parameters.Center + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth) - 50, 0.5 * parameters.SideBigEndWidth, 0),
                ColorIndex = 2
            };

            //创建侧路大端50mm直管段
            Line sideBigEndOuterPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.StartPoint,
                EndPoint = sideBigEndLine.StartPoint + new Vector3d(50,0,0),
                ColorIndex = 1
            };
            Line sideBigEndInnerPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.EndPoint,
                EndPoint = sideBigEndLine.EndPoint + new Vector3d(50, 0, 0),
                ColorIndex = 1
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
                bigOutterArc.ColorIndex = 1;
                mainSmallToSideBigArc.StartPoint = mainSmallEndLine.StartPoint;
                mainSmallToSideBigArc.EndPoint = bigOutterArc.StartPoint;
            }

            //创建侧路小端的端线
            Line sideSmallEndLine = new Line()
            {
                StartPoint = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, -0.5 * parameters.SideSmallEndWidth, 0),
                EndPoint = parameters.Center + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, 0.5 * parameters.SideSmallEndWidth, 0),
                ColorIndex = 2
            };

            //创建侧路小端50mm直管段
            Line sideSmallEndInnerPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.StartPoint,
                EndPoint = sideSmallEndLine.StartPoint + new Vector3d(-50, 0, 0),
                ColorIndex = 1
            };
            Line sideSmallEndOuterPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.EndPoint,
                EndPoint = sideSmallEndLine.EndPoint + new Vector3d(-50, 0, 0),
                ColorIndex = 1
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
                smallOutterArc.ColorIndex = 1;
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
        private DBObjectCollection CreateverticalDuctGeometries(ThIfcDuctSegmentParameters parameters)
        {
            //绘制管道端线
            Line UpperLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Width / 2.0, parameters.Height / 2.0, 0),
                EndPoint = new Point3d(parameters.Width / 2.0, parameters.Height / 2.0, 0),
                ColorIndex = 1
            };
            Line LowerLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Width / 2.0, -parameters.Height / 2.0, 0),
                EndPoint = new Point3d(parameters.Width / 2.0, -parameters.Height / 2.0, 0),
                ColorIndex = 1
            };
            Line LeftLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Width / 2.0, -parameters.Height / 2.0, 0),
                EndPoint = new Point3d(-parameters.Width / 2.0, parameters.Height / 2.0, 0),
                ColorIndex = 1
            };
            Line RightLine = new Line()
            {
                StartPoint = new Point3d(parameters.Width / 2.0, -parameters.Height / 2.0, 0),
                EndPoint = new Point3d(parameters.Width / 2.0, parameters.Height / 2.0, 0),
                ColorIndex = 1
            };
            return new DBObjectCollection()
            {
                UpperLine,
                LowerLine,
                LeftLine,
                RightLine
            };
        }
        private DBObjectCollection CreateDuctSegmentGeometries(ThIfcDuctSegmentParameters parameters)
        {
            //绘制辅助中心线
            Line auxiliaryCenterLine = new Line()
            {
                StartPoint = new Point3d(-parameters.Length / 2.0, 0, 0),
                EndPoint = new Point3d(parameters.Length / 2.0, 0, 0),
                ColorIndex = 1
            };

            //偏移出管轮廓线
            var ductUpperLineCollection = auxiliaryCenterLine.GetOffsetCurves(0.5 * parameters.Width);
            var ductBelowLineCollection = auxiliaryCenterLine.GetOffsetCurves(-0.5 * parameters.Width);
            Line ductUpperLine = (Line)ductUpperLineCollection[0];
            Line ductBelowLine = (Line)ductBelowLineCollection[0];

            //绘制管道端线
            Line ductUpperEndLine = new Line()
            {
                StartPoint = ductUpperLine.StartPoint,
                EndPoint = ductBelowLine.StartPoint,
                ColorIndex = 1
            };
            Line ductBelowEndLine = new Line()
            {
                StartPoint = ductUpperLine.EndPoint,
                EndPoint = ductBelowLine.EndPoint,
                ColorIndex = 1
            };

            return new DBObjectCollection()
            {
                ductUpperLine,
                ductBelowLine,
                ductUpperEndLine,
                ductBelowEndLine
            };
        }
    }
}
