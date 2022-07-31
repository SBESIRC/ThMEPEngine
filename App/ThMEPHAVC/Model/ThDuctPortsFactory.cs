using System;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class LineGeoInfo
    {
        public DBObjectCollection geo;
        public DBObjectCollection flg;
        public DBObjectCollection centerLines;
        public LineGeoInfo()
        {
            geo = new DBObjectCollection();
            flg = new DBObjectCollection();
            centerLines = new DBObjectCollection();
        }
        public LineGeoInfo(DBObjectCollection geo, 
                           DBObjectCollection flg, 
                           DBObjectCollection centerLines)
        {
            this.geo = geo;
            this.flg = flg;
            this.centerLines = centerLines;
        }
    }
    public class ThDuctPortsFactory
    {
        public static LineGeoInfo CreateCross(double inWidth, double innWidth, double coWidth, double outtWidth)
        {
            var innerWidth = Math.Min(innWidth, outtWidth);
            var outterWidth = Math.Max(innWidth, outtWidth);
            var yOffset = 50 + outterWidth * 0.7;
            //创建大端的端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(-0.5 * inWidth, -yOffset, 0),
                EndPoint = new Point3d(0.5 * inWidth, -yOffset, 0),
            };

            //创建主路小端的端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(-0.5 * coWidth, 100 + 0.5 * outterWidth, 0),
                EndPoint = new Point3d(0.5 * coWidth, 100 + 0.5 * outterWidth, 0),
            };

            //创建主路大端与侧路大端的圆弧过渡段
            Point3d bigEndCircleCenter = new Point3d(-0.5 * (inWidth + outterWidth), -outterWidth, 0);
            Arc bigInnerArc = new Arc(bigEndCircleCenter, 0.5 * outterWidth, 0, 0.5 * Math.PI);
            //创建主路大端与侧路小端的圆弧过渡段
            Point3d smallEndCircleCenter = new Point3d(0.5 * (inWidth + innerWidth), -innerWidth, 0);
            Arc smallInnerArc = new Arc(smallEndCircleCenter, 0.5 * innerWidth, 0.5 * Math.PI, Math.PI);

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

            yOffset = outterWidth * 0.7 - outterWidth / 2 + inWidth / 2 + 50;
            //创建侧路大端的端线
            Line sideOutterEndLine = new Line()
            {
                StartPoint = new Point3d(-yOffset, -0.5 * outterWidth, 0),
                EndPoint = new Point3d(-yOffset, 0.5 * outterWidth, 0),
            };

            //创建侧路大端50mm直管段
            Line sideBigEndOuterPipeLine = new Line()
            {
                StartPoint = sideOutterEndLine.StartPoint,
                EndPoint = sideOutterEndLine.StartPoint + new Vector3d(50, 0, 0),
            };
            Line sideBigEndInnerPipeLine = new Line()
            {
                StartPoint = sideOutterEndLine.EndPoint,
                EndPoint = sideOutterEndLine.EndPoint + new Vector3d(50, 0, 0),
            };

            //创建侧路大端的外侧圆弧过渡段
            //创建辅助线，确定侧路大端管线与主路小端管线的交点
            Ray sideBigEndAuxiliaryRay = new Ray()
            {
                BasePoint = new Point3d(-0.5 * coWidth, 0, 0),
                SecondPoint = new Point3d(-0.5 * coWidth, 10, 0)
            };
            Circle sideBigEndAuxiliaryCircle = new Circle()
            {
                Center = bigEndCircleCenter,
                Radius = 1.5 * outterWidth
            };
            IntPtr ptr = new IntPtr();
            Point3dCollection bigEndIntersects = new Point3dCollection();
            sideBigEndAuxiliaryRay.IntersectWith(sideBigEndAuxiliaryCircle, Intersect.OnBothOperands, bigEndIntersects, ptr, ptr);
            Arc bigOutterArc = new Arc();
            Line mainSmallToSideBigArc = new Line();
            if (bigEndIntersects.Count != 0)
            {
                Point3d intersectpoint = bigEndIntersects[0];
                bigOutterArc.CreateArcSCE(intersectpoint, bigEndCircleCenter, sideBigEndOuterPipeLine.EndPoint);
                mainSmallToSideBigArc.StartPoint = mainSmallEndLine.StartPoint;
                mainSmallToSideBigArc.EndPoint = bigOutterArc.StartPoint;
            }

            yOffset = innerWidth * 0.7 - innerWidth / 2 + inWidth / 2 + 50;
            //创建侧路小端的端线
            Line sideInnerEndLine = new Line()
            {
                StartPoint = new Point3d(yOffset, -0.5 * innerWidth, 0),
                EndPoint = new Point3d(yOffset, 0.5 * innerWidth, 0),
            };

            //创建侧路小端50mm直管段
            Line sideSmallEndInnerPipeLine = new Line()
            {
                StartPoint = sideInnerEndLine.StartPoint,
                EndPoint = sideInnerEndLine.StartPoint + new Vector3d(-50, 0, 0),
            };
            Line sideSmallEndOuterPipeLine = new Line()
            {
                StartPoint = sideInnerEndLine.EndPoint,
                EndPoint = sideInnerEndLine.EndPoint + new Vector3d(-50, 0, 0),
            };

            //创建侧路小端的外侧圆弧过渡段
            //创建辅助线，确定侧路小端管线与主路小端管线的交点
            Ray sideSmallEndAuxiliaryRay = new Ray()
            {
                BasePoint = new Point3d(0.5 * coWidth, 0, 0),
                SecondPoint = new Point3d(0.5 * coWidth, 10, 0)
            };
            Circle sideSmallEndAuxiliaryCircle = new Circle()
            {
                Center = smallEndCircleCenter,
                Radius = 1.5 * innerWidth
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

            var cross_geo = new DBObjectCollection() { bigInnerArc,
                                                       smallInnerArc,
                                                       mainBigEndPipeLine,
                                                       sideBigEndPipeLine,
                                                       sideBigEndOuterPipeLine,
                                                       sideBigEndInnerPipeLine,
                                                       bigOutterArc,
                                                       mainSmallToSideBigArc,
                                                       sideSmallEndOuterPipeLine,
                                                       sideSmallEndInnerPipeLine,
                                                       smallOutterArc,
                                                       mainSmallToSideSmallArc };
            double ext = 45;
            var cross_flg = new DBObjectCollection() { ThMEPHVACService.ExtendLine(mainBigEndLine, ext),
                                                       ThMEPHVACService.ExtendLine(mainSmallEndLine, ext),
                                                       ThMEPHVACService.ExtendLine(sideOutterEndLine, ext),
                                                       ThMEPHVACService.ExtendLine(sideInnerEndLine, ext)};
            var cross_centerline = new DBObjectCollection() { new Line(Point3d.Origin, ThMEPHVACService.GetMidPoint(mainBigEndLine)),
                                                              new Line(Point3d.Origin, ThMEPHVACService.GetMidPoint(mainSmallEndLine)),
                                                              new Line(Point3d.Origin, ThMEPHVACService.GetMidPoint(sideOutterEndLine)),
                                                              new Line(Point3d.Origin, ThMEPHVACService.GetMidPoint(sideInnerEndLine))};
            return new LineGeoInfo(cross_geo, cross_flg, cross_centerline);
        }
        public static LineGeoInfo CreateTee(double main_width, double branch, double other, TeeType type)
        {
            if (type == TeeType.BRANCH_VERTICAL_WITH_OTTER)
                return CreateRTeeOutlines(main_width, branch, other);
            else
                return CreateVTeeOutlines(main_width, branch, other);
        }
        private static LineGeoInfo CreateRTeeOutlines(double main_width, double branch, double other)
        {
            var endlines = CreateRTeeEndline(main_width, branch, other);
            return CreateRTeeGeometries(endlines, main_width, branch);
        }
        private static LineGeoInfo CreateVTeeOutlines(double main_width, double branch, double other)
        {
            var endlines = CreateVTeeEndline(main_width, branch, other);
            return CreateVTeeGeometries(endlines, main_width, branch, other);
        }
        private static DBObjectCollection CreateVTeeEndline(double inW, double branchW, double otherW)
        {
            //右向弯管
            double xOft = branchW * 0.7 - branchW / 2 + inW / 2 + 50 ;//(inW + branchW) * 0.5 + 50;
            double yOft = 0.5 * branchW;
            double ext = 45;
            var branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = -(otherW * 0.7 - otherW / 2 + 50 + inW / 2);// - (inW + otherW) * 0.5 - 50;
            yOft = 0.5 * otherW;
            //左向弯管
            var mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * inW;
            double maxBranch = Math.Max(branchW, otherW);
            yOft = -maxBranch * 0.7 - 50;
            //输入
            var mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };

            return new DBObjectCollection()
            {
                mainBigEndLine,
                branchEndLine,
                mainSmallEndLine
            };
        }
        private static DBObjectCollection CreateRTeeEndline(double inW, double branchW, double otherW)
        {
            //创建支路端线
            double xOft = branchW * 0.7 - branchW / 2 + inW / 2 + 50; // (inW + branchW) * 0.5 + 50;
            double yOft = 0.5 * branchW;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * otherW;
            yOft = branchW * 0.5 + 100;// 0.5 * branchW + 100;
            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };
            xOft = 0.5 * inW;
            yOft = -(branchW * 0.7 + 50);// - branchW - 50;
            //创建主路大端端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };

            return new DBObjectCollection()
            {
                mainBigEndLine,
                branchEndLine,
                mainSmallEndLine
            };
        }
        private static LineGeoInfo CreateVTeeGeometries(DBObjectCollection endlines, double i_width, double r_width, double l_width)
        {
            double R1 = 0.5 * (i_width + l_width);
            double R2 = 0.5 * (i_width + r_width);
            var left_circle_cp = new Point3d(-R1, -l_width, 0);
            var right_circle_cp = new Point3d(R2, -r_width, 0);
            var left_inner_arc = new Arc(left_circle_cp, 0.5 * l_width, 0, 0.5 * Math.PI);
            var right_inner_arc = new Arc(right_circle_cp, 0.5 * r_width, 0.5 * Math.PI, Math.PI);
            var floor_left = new Point3d(-0.5 * i_width, -l_width - 50, 0);
            var floor_right = new Point3d(0.5 * i_width, -r_width - 50, 0);
            var left_inner_arc_v_extend = new Line(left_inner_arc.StartPoint, floor_left);
            var right_inner_arc_v_extend = new Line(right_inner_arc.EndPoint, floor_right);
            var left_inner_arc_h50_extend = new Line(left_inner_arc.EndPoint, left_inner_arc.EndPoint + new Vector3d(-50, 0, 0));
            var right_inner_arc_h50_extend = new Line(right_inner_arc.StartPoint, right_inner_arc.StartPoint + new Vector3d(50, 0, 0));
            var oft = new Vector3d(0, l_width, 0);
            var left_outter_arc_h50_extend = new Line(left_inner_arc_h50_extend.StartPoint + oft, left_inner_arc_h50_extend.EndPoint + oft);
            oft = new Vector3d(0, r_width, 0);
            var right_outter_arc_h50_extend = new Line(right_inner_arc_h50_extend.StartPoint + oft, right_inner_arc_h50_extend.EndPoint + oft);

            var left_aux_arc = new Arc(left_circle_cp, 1.5 * l_width, 0, 0.5 * Math.PI);
            var right_aux_arc = new Arc(right_circle_cp, 1.5 * r_width, 0.5 * Math.PI, Math.PI);
            var intersect_points = new Point3dCollection();
            var ptr = new IntPtr();
            left_aux_arc.IntersectWith(right_aux_arc, Intersect.OnBothOperands, intersect_points, ptr, ptr);

            var left_otter_arc = new Arc();
            var right_otter_arc = new Arc();
            if (intersect_points.Count != 0)
            {
                Point3d p = intersect_points[0];
                left_otter_arc.CreateArcSCE(p, left_circle_cp, left_outter_arc_h50_extend.StartPoint);
                right_otter_arc.CreateArcSCE(right_outter_arc_h50_extend.StartPoint, right_circle_cp, p);
            }
            var mainBigEndLine = endlines[0] as Line;
            var branchEndLine = endlines[1] as Line;
            var mainSmallEndLine = endlines[2] as Line;
            var org_p = Point3d.Origin;
            var tee_center_line = new DBObjectCollection() { new Line(org_p, ThMEPHVACService.GetMidPoint(mainBigEndLine)),
                                                             new Line(org_p, ThMEPHVACService.GetMidPoint(branchEndLine)),
                                                             new Line(org_p, ThMEPHVACService.GetMidPoint(mainSmallEndLine))};
            if (Math.Abs(floor_left.Y - floor_right.Y) > 1e-3)
            {
                Line supplement;
                if (r_width > l_width)
                {
                    Point3d aux_p = new Point3d(floor_left.X, floor_right.Y, 0);
                    supplement = new Line(floor_left, aux_p);
                }
                else
                {
                    Point3d aux_p = new Point3d(floor_right.X, floor_left.Y, 0);
                    supplement = new Line(floor_right, aux_p);
                }
                var outline = new DBObjectCollection() { left_inner_arc,
                                                         right_inner_arc,
                                                         left_inner_arc_v_extend,
                                                         right_inner_arc_v_extend,
                                                         left_inner_arc_h50_extend,
                                                         right_inner_arc_h50_extend,
                                                         left_outter_arc_h50_extend,
                                                         right_outter_arc_h50_extend,
                                                         left_otter_arc,
                                                         right_otter_arc,
                                                         supplement };
                return new LineGeoInfo(outline, endlines, tee_center_line);
            }
            var tee_outline = new DBObjectCollection()
            {
                left_inner_arc,
                right_inner_arc,
                left_inner_arc_v_extend,
                right_inner_arc_v_extend,
                left_inner_arc_h50_extend,
                right_inner_arc_h50_extend,
                left_outter_arc_h50_extend,
                right_outter_arc_h50_extend,
                left_otter_arc,
                right_otter_arc
            };
            return new LineGeoInfo(tee_outline, endlines, tee_center_line);
        }
        private static LineGeoInfo CreateRTeeGeometries(DBObjectCollection endlines, double i_width, double o_width1)
        {
            double ext = 45;
            var mainBigEndLine = endlines[0].Clone() as Line;
            mainBigEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainBigEndLine.EndPoint += new Vector3d(ext, 0, 0);
            var branchEndLine = endlines[1].Clone() as Line;
            branchEndLine.StartPoint += new Vector3d(0, -ext, 0);
            branchEndLine.EndPoint += new Vector3d(0, ext, 0);
            var mainSmallEndLine = endlines[2].Clone() as Line;
            mainSmallEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainSmallEndLine.EndPoint += new Vector3d(ext, 0, 0);

            //创建支路50mm直管段
            var branchUpStraightLine = new Line()
            {
                StartPoint = branchEndLine.StartPoint,
                EndPoint = branchEndLine.StartPoint + new Vector3d(-50, 0, 0),
            };
            var branchBelowStraightLine = new Line()
            {
                StartPoint = branchEndLine.EndPoint,
                EndPoint = branchEndLine.EndPoint + new Vector3d(-50, 0, 0),
            };

            //创建支路下侧圆弧过渡段
            var circleCenter = new Point3d(0.5 * (i_width + o_width1), -o_width1, 0);
            var branchInnerArc = new Arc(circleCenter, 0.5 * o_width1, 0.5 * Math.PI, Math.PI);

            //创建支路上侧圆弧过渡段
            //首先创建主路上端小管道的内侧线作为辅助线以便于后续计算圆弧交点
            var branchAuxiliaryRay = new Ray()
            {
                BasePoint = mainSmallEndLine.StartPoint,
                SecondPoint = mainSmallEndLine.StartPoint + new Vector3d(0, -5000, 0)
            };
            var branchAuxiliaryCircle = new Circle()
            {
                Center = circleCenter,
                Radius = 1.5 * o_width1
            };
            var Intersectpoints = new Point3dCollection();
            var ptr = new IntPtr();
            branchAuxiliaryRay.IntersectWith(branchAuxiliaryCircle, Intersect.OnBothOperands, Intersectpoints, ptr, ptr);
            var branchOuterArc = new Arc();
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
            }

            //创建主路外侧管线
            var outerStraightLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = mainBigEndLine.EndPoint + new Vector3d(0, 50, 0),
            };
            var outerObliqueLine = new Line()
            {
                StartPoint = outerStraightLine.EndPoint,
                EndPoint = mainSmallEndLine.EndPoint,
            };

            //创建主路内侧管线
            var innerUpLine = new Line()
            {
                StartPoint = mainSmallEndLine.StartPoint,
                EndPoint = branchOuterArc.EndPoint,
            };
            var innerBelowLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = branchInnerArc.EndPoint,
            };

            var tee_outline = new DBObjectCollection() { branchUpStraightLine,
                                                         branchBelowStraightLine,
                                                         branchInnerArc,
                                                         branchOuterArc,
                                                         outerStraightLine,
                                                         outerObliqueLine,
                                                         innerUpLine,
                                                         innerBelowLine };
            var org_p = Point3d.Origin;
            var tee_center_line = new DBObjectCollection() {new Line(org_p, ThMEPHVACService.GetMidPoint(mainBigEndLine)),
                                                            new Line(org_p, ThMEPHVACService.GetMidPoint(branchEndLine)),
                                                            new Line(org_p, ThMEPHVACService.GetMidPoint(mainSmallEndLine))};
            return new LineGeoInfo(tee_outline, endlines, tee_center_line);
        }
        public static LineGeoInfo CreateElbow(double angle, double width)
        {
            double extend_len = 50;
            double cos_angle = Math.Cos(angle);
            double sin_angle = Math.Sin(angle);
            var centerPoint = new Point3d(-0.7 * width, -Math.Abs(0.7 * width * Math.Tan(0.5 * angle)), 0);
            //创建弯头内外侧圆弧
            var outerarc = new Arc(centerPoint, 1.2 * width, 0, angle);
            var innerarc = new Arc(centerPoint, 0.2 * width, 0, angle);
            //创建弯头两端的50mm延申段
            var outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            var innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            var outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -extend_len, 0),
            };
            var innerstartextendline = new Line()
            {
                StartPoint = innerarc.StartPoint,
                EndPoint = innerarc.StartPoint + new Vector3d(0, -extend_len, 0),
            };
            var elbow_outline = new DBObjectCollection() { outerarc,
                                                           innerarc,
                                                           outerendextendline,
                                                           innerendextendline,
                                                           outerstartextendline,
                                                           innerstartextendline};
            var center_arc = new Arc(centerPoint, 0.7 * width, 0, angle);
            var center_arc_extendline1 = new Line()
            {
                StartPoint = center_arc.EndPoint,
                EndPoint = center_arc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            var center_arc_extendline2 = new Line()
            {
                StartPoint = center_arc.StartPoint,
                EndPoint = center_arc.StartPoint + new Vector3d(0, -extend_len, 0),
            };
            var elbow_centerline = new DBObjectCollection() { center_arc,
                                                              center_arc_extendline1,
                                                              center_arc_extendline2};
            var flg1 = new Line(innerstartextendline.EndPoint, outerstartextendline.EndPoint);
            var dir_vec = (flg1.EndPoint - flg1.StartPoint).GetNormal();
            var flgExtendLen = 45;
            flg1 = new Line(flg1.StartPoint - dir_vec * flgExtendLen, flg1.EndPoint + dir_vec * flgExtendLen);
            var flg2 = new Line(innerendextendline.EndPoint, outerendextendline.EndPoint);
            dir_vec = (flg2.EndPoint - flg2.StartPoint).GetNormal();
            flg2 = new Line(flg2.StartPoint - dir_vec * flgExtendLen, flg2.EndPoint + dir_vec * flgExtendLen);
            var elbow_flg = new DBObjectCollection() { flg1, flg2 };
            return new LineGeoInfo(elbow_outline, elbow_flg, elbow_centerline);
        }
        public static void GetDuctGeoFlgCenterLine( Line l,
                                                    double width,
                                                    double angle,
                                                    Point3d centerPoint,
                                                    out DBObjectCollection geo,
                                                    out DBObjectCollection flg,
                                                    out DBObjectCollection centerLine)
        {
            geo = new DBObjectCollection();
            flg = new DBObjectCollection();
            centerLine = new DBObjectCollection();
            if (l.Length < 1e-3)
                return;
            var lines = CreateDuct(l.Length, width);
            Matrix3d mat = Matrix3d.Displacement(centerPoint.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
            var l1 = lines[0] as Line;
            l1.TransformBy(mat);
            geo.Add(l1);
            var l2 = lines[1] as Line;
            l2.TransformBy(mat);
            geo.Add(l2);
            flg.Add(new Line(l1.StartPoint, l2.StartPoint));
            flg.Add(new Line(l1.EndPoint, l2.EndPoint));
            centerLine.Add(l);
        }
        public static LineGeoInfo CreateVerticalPipe(SegInfo seg, string ductSize)
        {
            ThMEPHVACService.GetWidthAndHeight(ductSize, out double w, out double h);
            var vec = seg.horizontalVec * w * 0.5;
            // SP是底部，EP是顶部
            var topSp = seg.l.StartPoint - vec;// 水平起点
            var topEp = seg.l.StartPoint + vec;  // 水平终点
            var auxLine = new Line() { StartPoint = topSp, EndPoint = topEp };
            var lVec = ThMEPHVACService.GetLeftVerticalVec(seg.horizontalVec);
            var rVec = ThMEPHVACService.GetRightVerticalVec(seg.horizontalVec);
            var sp1 = auxLine.StartPoint + lVec * 0.5 * h;
            var sp2 = auxLine.StartPoint + rVec * 0.5 * h;
            var ep1 = auxLine.EndPoint + lVec * 0.5 * h;
            var ep2 = auxLine.EndPoint + rVec * 0.5 * h;

            var geo = new DBObjectCollection() { new Line(sp1, ep1), new Line(sp2, ep2) };
            var flg = new DBObjectCollection() { new Line(sp1, sp2), new Line(ep1, ep2) };
            var centerLine = new DBObjectCollection() { seg.l };
            return new LineGeoInfo(geo, flg, centerLine);
        }
        public static LineGeoInfo CreateDuct(Point2d sp, Point2d ep, double width)
        {
            var sp3 = new Point3d(sp.X, sp.Y, 0);
            var ep3 = new Point3d(ep.X, ep.Y, 0);
            return CreateDuct(sp3, ep3, width);
        }
        public static LineGeoInfo CreateDuct(Point3d sp, Point3d ep, double width)
        {
            var aux_line = new Line() { StartPoint = sp, EndPoint = ep };
            var dir_vec = ThMEPHVACService.GetEdgeDirection(aux_line);
            var l_v_vec = ThMEPHVACService.GetLeftVerticalVec(dir_vec);
            var r_v_vec = ThMEPHVACService.GetRightVerticalVec(dir_vec);
            var sp1 = aux_line.StartPoint + l_v_vec * 0.5 * width;
            var sp2 = aux_line.StartPoint + r_v_vec * 0.5 * width;
            var ep1 = aux_line.EndPoint + l_v_vec * 0.5 * width;
            var ep2 = aux_line.EndPoint + r_v_vec * 0.5 * width;

            var geo = new DBObjectCollection() { new Line(sp1, ep1), new Line(sp2, ep2) };
            var flg = new DBObjectCollection() { new Line(sp1, sp2), new Line(ep1, ep2) };
            var center_line = new DBObjectCollection() { aux_line };
            return new LineGeoInfo(geo, flg, center_line);
        }
        public static DBObjectCollection CreateDuct(double length, double width)
        {
            //绘制辅助中心线
            var auxiliaryCenterLine = new Line()
            {
                StartPoint = new Point3d(-length / 2.0, 0, 0),
                EndPoint = new Point3d(length / 2.0, 0, 0),
            };

            //偏移出管轮廓线
            var ductUpperLineCollection = auxiliaryCenterLine.GetOffsetCurves(0.5 * width);
            var ductBelowLineCollection = auxiliaryCenterLine.GetOffsetCurves(-0.5 * width);
            var ductUpperLine = (Line)ductUpperLineCollection[0];
            var ductBelowLine = (Line)ductBelowLineCollection[0];

            return new DBObjectCollection()
            {
                ductUpperLine,
                ductBelowLine,
            };
        }
        public static LineGeoInfo CreateReducing(Line centerLine, double bigWidth, double smallWidth, bool isAxis)
        {
            var geo = CreateReducingGeo(centerLine, bigWidth, smallWidth, isAxis);
            var flg = CreateReducingFlg(geo);
            var center_line = CreateReducingCenter(centerLine);
            return new LineGeoInfo(geo, flg, center_line);
        }
        private static DBObjectCollection CreateReducingGeo(Line centerLine, double bigWidth, double smallWidth, bool isAxis)
        {
            var sp = new Point3d(centerLine.StartPoint.X, centerLine.StartPoint.Y, 0);
            var ep = new Point3d(centerLine.EndPoint.X, centerLine.EndPoint.Y, 0);
            var l = new Line(sp, ep);
            var dir_vec = ThMEPHVACService.GetEdgeDirection(l);
            var left = ThMEPHVACService.GetLeftVerticalVec(dir_vec);
            var right = ThMEPHVACService.GetRightVerticalVec(dir_vec);
            var p1 = l.StartPoint + left * bigWidth * 0.5;
            var p2 = l.StartPoint + right * bigWidth * 0.5;
            var p3 = l.EndPoint + left * smallWidth * 0.5;
            var p4 = l.EndPoint + right * smallWidth * 0.5;
            var upHypotenuse = new Line(p1, p3);
            var downHypotenuse = new Line(p2, p4);
            var innerUpHypotenuse = new Line(l.StartPoint, p3);
            var innerDownHypotenuse = new Line(l.StartPoint, p4);
            return isAxis ? new DBObjectCollection() { upHypotenuse, downHypotenuse, innerUpHypotenuse, innerDownHypotenuse } :
                            new DBObjectCollection() { upHypotenuse, downHypotenuse };
        }
        private static DBObjectCollection CreateReducingFlg(DBObjectCollection reducingDuctGeo)
        {
            double dis = 45;
            var upHypotenuse = reducingDuctGeo[0] as Line;
            var downHypotenuse = reducingDuctGeo[1] as Line;
            var l1 = new Line(upHypotenuse.StartPoint, downHypotenuse.StartPoint);
            var dirVec = ThMEPHVACService.GetEdgeDirection(l1);
            var disVec = dis * dirVec;
            l1 = new Line(l1.StartPoint - disVec, l1.EndPoint + disVec);
            var l2 = new Line(upHypotenuse.EndPoint, downHypotenuse.EndPoint);
            dirVec = ThMEPHVACService.GetEdgeDirection(l2);
            disVec = dis * dirVec;
            l2 = new Line(l2.StartPoint - disVec, l2.EndPoint + disVec);
            return new DBObjectCollection() { l1, l2 };
        }
        private static DBObjectCollection CreateReducingCenter(Line l)
        {
            return new DBObjectCollection() { new Line(l.StartPoint, l.EndPoint) };
        }
    }
}