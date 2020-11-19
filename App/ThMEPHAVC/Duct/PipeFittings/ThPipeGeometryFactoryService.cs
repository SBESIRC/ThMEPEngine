using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using DotNetARX;
using AcHelper;

namespace ThMEPHAVC.Duct.PipeFitting
{
    public class ThPipeGeometryFactoryService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThPipeGeometryFactoryService instance = new ThPipeGeometryFactoryService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThPipeGeometryFactoryService() { }
        internal ThPipeGeometryFactoryService() { }
        public static ThPipeGeometryFactoryService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThReducing CreateReducing(ThReducingParameters parameters)
        {
            return new ThReducing(parameters)
            {
                Geometries = CreateReducingGeometries(parameters)
            };
        }

        public ThElbow CreateElbow(ThElbowParameters parameters)
        {
            return new ThElbow(parameters)
            {
                Geometries = CreateElbowGeometries(parameters)
            };
        }

        public ThTee CreateTee(ThTeeParameters parameters)
        {
            return new ThTee(parameters)
            {
                Geometries = CreateTeeGeometries(parameters)
            };
        }

        public ThFourWay CreateFourWay(ThFourWayParameters parameters)
        {
            return new ThFourWay(parameters)
            {
                Geometries = CreateFourWayGeometries(parameters)
            };
        }


        private DBObjectCollection CreateReducingGeometries(ThReducingParameters parameters)
        {
            //创建小端的端线
            Line smallendline = new Line()
            {
                StartPoint = parameters.StartCenterPoint + new Vector3d(0, -0.5 * parameters.SmallEndWidth, 0),
                EndPoint = parameters.StartCenterPoint + new Vector3d(0, 0.5 * parameters.SmallEndWidth, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建两侧侧壁轮廓线
            Line leftsideline = new Line()
            {
                StartPoint = smallendline.StartPoint,
                EndPoint = smallendline.StartPoint + new Vector3d(0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(15 * Math.PI / 180), -0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line rightsideline = new Line()
            {
                StartPoint = smallendline.EndPoint,
                EndPoint = smallendline.EndPoint + new Vector3d(0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth) / Math.Tan(15 * Math.PI / 180), 0.5 * (parameters.BigEndWidth - parameters.SmallEndWidth), 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建大端的端线
            Line bigendline = new Line()
            {
                StartPoint = leftsideline.EndPoint,
                EndPoint = rightsideline.EndPoint,
                Layer = "Auot_DUCT-加压送风管端线",
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

        public DBObjectCollection CreateElbowGeometries(ThElbowParameters parameters)
        {
            var elbowengle = parameters.ElbowDegree * Math.PI / 180;
            //创建弯头内外侧圆弧
            Arc outerarc = new Arc(parameters.CenterPoint, 1.5 * parameters.PipeOpenWidth, 0, elbowengle)
            {
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Arc innerarc = new Arc(parameters.CenterPoint, 0.5 * parameters.PipeOpenWidth, 0, elbowengle)
            {
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            //创建弯头两端的50mm延申段
            Line outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-50 * Math.Sin(elbowengle), 50 * Math.Cos(elbowengle), 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -50, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line innerstartextendline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = innerarc.StartPoint + new Vector3d(0, -50, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建弯头中心线圆弧
            Arc centerarc = new Arc(parameters.CenterPoint, parameters.PipeOpenWidth, 0, elbowengle)
            {
                Layer = "Auot_DUCT-加压送风中心线",
                ColorIndex = 5
            };

            //创建弯头端线
            Line startplaneline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = outerarc.StartPoint,
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            Line endplaneline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = outerarc.EndPoint,
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建两端50mm延申段的端线
            Line startextendsealline = new Line()
            {
                StartPoint = outerstartextendline.EndPoint,
                EndPoint = innerstartextendline.EndPoint,
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            Line endextendsealline = new Line()
            {
                StartPoint = outerendextendline.EndPoint,
                EndPoint = innerendextendline.EndPoint,
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            return new DBObjectCollection()
            {
                outerarc,
                centerarc,
                innerarc,
                startplaneline,
                endplaneline,
                outerendextendline,
                innerendextendline,
                outerstartextendline,
                innerstartextendline,
                startextendsealline,
                endextendsealline
            };
        }

        public DBObjectCollection CreateTeeGeometries(ThTeeParameters parameters)
        {
            //创建支路端线
            Line branchEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter + parameters.BranchDiameter)+50, 0.5*parameters.BranchDiameter, 0),
                EndPoint = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter + parameters.BranchDiameter) +50, -0.5*parameters.BranchDiameter, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5*parameters.MainSmallDiameter, 0.5 * parameters.BranchDiameter + 100,0),
                EndPoint = parameters.CenterPoint + new Vector3d(-0.5*parameters.MainSmallDiameter, 0.5 * parameters.BranchDiameter + 100,0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建主路大端端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = parameters.CenterPoint + new Vector3d(0.5 * parameters.MainBigDiameter, -parameters.BranchDiameter - 50, 0),
                EndPoint = parameters.CenterPoint + new Vector3d(-0.5 * parameters.MainBigDiameter, -parameters.BranchDiameter - 50, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建支路50mm直管段
            Line branchUpStraightLine = new Line()
            {
                StartPoint = branchEndLine.StartPoint,
                EndPoint = branchEndLine.StartPoint + new Vector3d(-50,0,0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line branchBelowStraightLine = new Line()
            {
                StartPoint = branchEndLine.EndPoint,
                EndPoint = branchEndLine.EndPoint + new Vector3d(-50, 0, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建支路下侧圆弧过渡段
            Point3d circleCenter = parameters.CenterPoint + new Vector3d(0.5*(parameters.MainBigDiameter+parameters.BranchDiameter), -parameters.BranchDiameter, 0);
            Arc branchInnerArc = new Arc(circleCenter, 0.5 * parameters.BranchDiameter, 0.5 * Math.PI, Math.PI)
            {
                Layer = "Auot_DUCT-加压送风管",
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
                branchOuterArc.Layer = "Auot_DUCT-加压送风管";
                branchOuterArc.ColorIndex = 1;
            }

            //创建主路外侧管线
            Line outerStraightLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = mainBigEndLine.EndPoint + new Vector3d(0,50,0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line outerObliqueLine = new Line()
            {
                StartPoint = outerStraightLine.EndPoint,
                EndPoint = mainSmallEndLine.EndPoint,
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建主路内侧管线
            Line innerUpLine = new Line()
            {
                StartPoint = mainSmallEndLine.StartPoint,
                EndPoint = branchOuterArc.EndPoint,
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line innerBelowLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = branchInnerArc.EndPoint,
                Layer = "Auot_DUCT-加压送风管",
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

        public DBObjectCollection CreateFourWayGeometries(ThFourWayParameters parameters)
        {
            //创建大端的端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = parameters.FourWayCenter + new Vector3d(-0.5* parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
                EndPoint = parameters.FourWayCenter + new Vector3d(0.5 * parameters.BigEndWidth, -50 - parameters.SideBigEndWidth, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建主路小端的端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = parameters.FourWayCenter + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
                EndPoint = parameters.FourWayCenter + new Vector3d(0.5 * parameters.mainSmallEndWidth, 100 + 0.5 * parameters.SideBigEndWidth, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };


            //创建主路大端与侧路大端的圆弧过渡段
            Point3d bigEndCircleCenter = parameters.FourWayCenter + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth), -parameters.SideBigEndWidth, 0);
            Arc bigInnerArc = new Arc(bigEndCircleCenter, 0.5 * parameters.SideBigEndWidth, 0, 0.5 * Math.PI)
            {
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            //创建主路大端与侧路小端的圆弧过渡段
            Point3d smallEndCircleCenter = parameters.FourWayCenter + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth), -parameters.SideSmallEndWidth, 0);
            Arc smallInnerArc = new Arc(smallEndCircleCenter, 0.5 * parameters.SideSmallEndWidth, 0.5 * Math.PI, Math.PI)
            {
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建主路大端圆弧过渡与大端端线端点连接线
            Line mainBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = bigInnerArc.StartPoint,
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line sideBigEndPipeLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = smallInnerArc.EndPoint,
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建侧路大端的端线
            Line sideBigEndLine = new Line()
            {
                StartPoint = parameters.FourWayCenter + new Vector3d(-0.5 * (parameters.BigEndWidth+ parameters.SideBigEndWidth) - 50,-0.5 * parameters.SideBigEndWidth,0),
                EndPoint = parameters.FourWayCenter + new Vector3d(-0.5 * (parameters.BigEndWidth + parameters.SideBigEndWidth) - 50, 0.5 * parameters.SideBigEndWidth, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建侧路大端50mm直管段
            Line sideBigEndOuterPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.StartPoint,
                EndPoint = sideBigEndLine.StartPoint + new Vector3d(50,0,0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line sideBigEndInnerPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.EndPoint,
                EndPoint = sideBigEndLine.EndPoint + new Vector3d(50, 0, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建侧路大端的外侧圆弧过渡段
            //创建辅助线，确定侧路大端管线与主路小端管线的交点
            Ray sideBigEndAuxiliaryRay = new Ray()
            {
                BasePoint = parameters.FourWayCenter + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 0, 0),
                SecondPoint = parameters.FourWayCenter + new Vector3d(-0.5 * parameters.mainSmallEndWidth, 10, 0)
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
                bigOutterArc.Layer = "Auot_DUCT-加压送风管";
                bigOutterArc.ColorIndex = 1;
                mainSmallToSideBigArc.StartPoint = mainSmallEndLine.StartPoint;
                mainSmallToSideBigArc.EndPoint = bigOutterArc.StartPoint;
            }

            //创建侧路小端的端线
            Line sideSmallEndLine = new Line()
            {
                StartPoint = parameters.FourWayCenter + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, -0.5 * parameters.SideSmallEndWidth, 0),
                EndPoint = parameters.FourWayCenter + new Vector3d(0.5 * (parameters.BigEndWidth + parameters.SideSmallEndWidth) + 50, 0.5 * parameters.SideSmallEndWidth, 0),
                Layer = "Auot_DUCT-加压送风管端线",
                ColorIndex = 2
            };

            //创建侧路小端50mm直管段
            Line sideSmallEndInnerPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.StartPoint,
                EndPoint = sideSmallEndLine.StartPoint + new Vector3d(-50, 0, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };
            Line sideSmallEndOuterPipeLine = new Line()
            {
                StartPoint = sideSmallEndLine.EndPoint,
                EndPoint = sideSmallEndLine.EndPoint + new Vector3d(-50, 0, 0),
                Layer = "Auot_DUCT-加压送风管",
                ColorIndex = 1
            };

            //创建侧路小端的外侧圆弧过渡段
            //创建辅助线，确定侧路小端管线与主路小端管线的交点
            Ray sideSmallEndAuxiliaryRay = new Ray()
            {
                BasePoint = parameters.FourWayCenter + new Vector3d(0.5 * parameters.mainSmallEndWidth, 0, 0),
                SecondPoint = parameters.FourWayCenter + new Vector3d(0.5 * parameters.mainSmallEndWidth, 10, 0)
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
                smallOutterArc.Layer = "Auot_DUCT-加压送风管";
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

    }
}
