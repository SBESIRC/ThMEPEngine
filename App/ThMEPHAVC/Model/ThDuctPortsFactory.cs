using System;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.Model
{
    public class Line_Info
    {
        public DBObjectCollection geo;
        public DBObjectCollection flg;
        public DBObjectCollection center_line;
        public Line_Info(){ }
        public Line_Info(DBObjectCollection geo_, DBObjectCollection flg_, DBObjectCollection center_line_)
        {
            geo = geo_;
            flg = flg_;
            center_line = center_line_;
        }
    }
    public class ThDuctPortsFactory
    {
        public static Line_Info Create_cross(double i_width, double o_width1, double o_width2, double o_width3)
        {
            if (o_width1 > o_width3)
            {
                double tmp = o_width1;
                o_width1 = o_width3;
                o_width3 = tmp;
            }
            //创建大端的端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(-0.5 * i_width, -50 - o_width3, 0),
                EndPoint = new Point3d(0.5 * i_width, -50 - o_width3, 0),
            };

            //创建主路小端的端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(-0.5 * o_width2, 100 + 0.5 * o_width3, 0),
                EndPoint = new Point3d(0.5 * o_width2, 100 + 0.5 * o_width3, 0),
            };

            //创建主路大端与侧路大端的圆弧过渡段
            Point3d bigEndCircleCenter = new Point3d(-0.5 * (i_width + o_width3), -o_width3, 0);
            Arc bigInnerArc = new Arc(bigEndCircleCenter, 0.5 * o_width3, 0, 0.5 * Math.PI);
            //创建主路大端与侧路小端的圆弧过渡段
            Point3d smallEndCircleCenter = new Point3d(0.5 * (i_width + o_width1), -o_width1, 0);
            Arc smallInnerArc = new Arc(smallEndCircleCenter, 0.5 * o_width1, 0.5 * Math.PI, Math.PI);

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
                StartPoint = new Point3d(-0.5 * (i_width + o_width3) - 50, -0.5 * o_width3, 0),
                EndPoint = new Point3d(-0.5 * (i_width + o_width3) - 50, 0.5 * o_width3, 0),
            };

            //创建侧路大端50mm直管段
            Line sideBigEndOuterPipeLine = new Line()
            {
                StartPoint = sideBigEndLine.StartPoint,
                EndPoint = sideBigEndLine.StartPoint + new Vector3d(50, 0, 0),
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
                BasePoint = new Point3d(-0.5 * o_width2, 0, 0),
                SecondPoint = new Point3d(-0.5 * o_width2, 10, 0)
            };
            Circle sideBigEndAuxiliaryCircle = new Circle()
            {
                Center = bigEndCircleCenter,
                Radius = 1.5 * o_width3
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

            //创建侧路小端的端线
            Line sideSmallEndLine = new Line()
            {
                StartPoint = new Point3d(0.5 * (i_width + o_width1) + 50, -0.5 * o_width1, 0),
                EndPoint = new Point3d(0.5 * (i_width + o_width1) + 50, 0.5 * o_width1, 0),
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
                BasePoint = new Point3d(0.5 * o_width2, 0, 0),
                SecondPoint = new Point3d(0.5 * o_width2, 10, 0)
            };
            Circle sideSmallEndAuxiliaryCircle = new Circle()
            {
                Center = smallEndCircleCenter,
                Radius = 1.5 * o_width1
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
            var cross_flg = new DBObjectCollection() { mainBigEndLine,
                                                       mainSmallEndLine,
                                                       sideBigEndLine,
                                                       sideSmallEndLine };
            var cross_centerline = new DBObjectCollection() { new Line(Point3d.Origin, Get_mid_point(mainBigEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(mainSmallEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(sideBigEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(sideSmallEndLine))};
            return new Line_Info(cross_geo, cross_flg, cross_centerline);
        }
        public static Line_Info Create_r_tee_outlines(double i_width, double o_width1, double o_width2)
        {
            var endlines = Create_r_tee_endline(i_width, o_width1, o_width2);
            return Create_r_tee_geometries(endlines, i_width, o_width1);
        }
        public static Line_Info Create_v_tee_outlines(double i_width, double o_width1, double o_width2)
        {
            var endlines = Create_v_tee_endline(i_width, o_width1, o_width2);
            return Create_v_tee_geometries(endlines, i_width, o_width1, o_width2);
        }
        private static DBObjectCollection Create_v_tee_endline(double i_width, double o_width1, double o_width2)
        {
            //右向弯管
            double xOft = (i_width + o_width1) * 0.5 + 50;
            double yOft = 0.5 * o_width1;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = -(i_width + o_width2) * 0.5 - 50;
            yOft = 0.5 * o_width2;
            //左向弯管
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * i_width;
            double branch_width = o_width1 > o_width2 ? o_width1 : o_width2;
            yOft = -branch_width - 50;
            //输入
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };

            return new DBObjectCollection()
            {
                branchEndLine,
                mainBigEndLine,
                mainSmallEndLine
            };
        }
        private static DBObjectCollection Create_r_tee_endline(double i_width, double o_width1, double o_width2)
        {
            //创建支路端线
            double xOft = (i_width + o_width1) * 0.5 + 50;
            double yOft = 0.5 * o_width1;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * o_width2;
            yOft = 0.5 * o_width1 + 100;
            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };
            xOft = 0.5 * i_width;
            yOft = -o_width1 - 50;
            //创建主路大端端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };

            return new DBObjectCollection()
            {
                branchEndLine,
                mainBigEndLine,
                mainSmallEndLine
            };
        }
        private static Line_Info Create_v_tee_geometries(DBObjectCollection endlines, double i_width, double r_width, double l_width)
        {
            double R1 = 0.5 * (i_width + l_width);
            double R2 = 0.5 * (i_width + r_width);
            Point3d left_circle_cp = new Point3d(-R1, -l_width, 0);
            Point3d right_circle_cp = new Point3d(R2, -r_width, 0);
            Arc left_inner_arc = new Arc(left_circle_cp, 0.5 * l_width, 0, 0.5 * Math.PI);
            Arc right_inner_arc = new Arc(right_circle_cp, 0.5 * r_width, 0.5 * Math.PI, Math.PI);
            Point3d floor_left = new Point3d(-0.5 * i_width, -l_width - 50, 0);
            Point3d floor_right = new Point3d(0.5 *  i_width, -r_width - 50, 0);
            Line left_inner_arc_v_extend = new Line(left_inner_arc.StartPoint, floor_left);
            Line right_inner_arc_v_extend = new Line(right_inner_arc.EndPoint, floor_right);
            Line left_inner_arc_h50_extend = new Line(left_inner_arc.EndPoint, left_inner_arc.EndPoint + new Vector3d(-50, 0, 0));
            Line right_inner_arc_h50_extend = new Line(right_inner_arc.StartPoint, right_inner_arc.StartPoint + new Vector3d(50, 0, 0));
            Vector3d oft = new Vector3d(0, l_width, 0);
            Line left_outter_arc_h50_extend = new Line(left_inner_arc_h50_extend.StartPoint + oft, left_inner_arc_h50_extend.EndPoint + oft);
            oft = new Vector3d(0, r_width, 0);
            Line right_outter_arc_h50_extend = new Line(right_inner_arc_h50_extend.StartPoint + oft, right_inner_arc_h50_extend.EndPoint + oft);

            Arc left_aux_arc = new Arc(left_circle_cp, 1.5 * l_width, 0, 0.5 * Math.PI);
            Arc right_aux_arc = new Arc(right_circle_cp, 1.5 * r_width, 0.5 * Math.PI, Math.PI);
            Point3dCollection intersect_points = new Point3dCollection();
            IntPtr ptr = new IntPtr();
            left_aux_arc.IntersectWith(right_aux_arc, Intersect.OnBothOperands, intersect_points, ptr, ptr);

            Arc left_otter_arc = new Arc();
            Arc right_otter_arc = new Arc();
            if (intersect_points.Count != 0)
            {
                Point3d p = intersect_points[0];
                left_otter_arc.CreateArcSCE(p, left_circle_cp, left_outter_arc_h50_extend.StartPoint);
                right_otter_arc.CreateArcSCE(right_outter_arc_h50_extend.StartPoint, right_circle_cp, p);
            }
            Line endline1 = endlines[0] as Line;
            Line endline2 = endlines[1] as Line;
            Line endline3 = endlines[2] as Line;
            var tee_flg = new DBObjectCollection() { new Line(Point3d.Origin, Get_mid_point(endline1)),
                                                     new Line(Point3d.Origin, Get_mid_point(endline2)),
                                                     new Line(Point3d.Origin, Get_mid_point(endline3))};
            if (Math.Abs(floor_left.Y - floor_right.Y) > 1e-3)
            {
                Line supplement = new Line();
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
                return new Line_Info(outline, endlines, tee_flg);
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
            return new Line_Info(tee_outline, endlines, tee_flg);
        }
        private static Line_Info Create_r_tee_geometries(DBObjectCollection endlines, double i_width, double o_width1)
        {
            double ext = 45;
            Line branchEndLine = endlines[0].Clone() as Line;
            branchEndLine.StartPoint += new Vector3d(0, -ext, 0);
            branchEndLine.EndPoint += new Vector3d(0, ext, 0);
            Line mainBigEndLine = endlines[1].Clone() as Line;
            mainBigEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainBigEndLine.EndPoint += new Vector3d(ext, 0, 0);
            Line mainSmallEndLine = endlines[2].Clone() as Line;
            mainSmallEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainSmallEndLine.EndPoint += new Vector3d(ext, 0, 0);

            //创建支路50mm直管段
            Line branchUpStraightLine = new Line()
            {
                StartPoint = branchEndLine.StartPoint,
                EndPoint = branchEndLine.StartPoint + new Vector3d(-50, 0, 0),
            };
            Line branchBelowStraightLine = new Line()
            {
                StartPoint = branchEndLine.EndPoint,
                EndPoint = branchEndLine.EndPoint + new Vector3d(-50, 0, 0),
            };

            //创建支路下侧圆弧过渡段
            Point3d circleCenter = new Point3d(0.5 * (i_width + o_width1), -o_width1, 0);
            Arc branchInnerArc = new Arc(circleCenter, 0.5 * o_width1, 0.5 * Math.PI, Math.PI);

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
                Radius = 1.5 * o_width1
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
            }

            //创建主路外侧管线
            Line outerStraightLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = mainBigEndLine.EndPoint + new Vector3d(0, 50, 0),
            };
            Line outerObliqueLine = new Line()
            {
                StartPoint = outerStraightLine.EndPoint,
                EndPoint = mainSmallEndLine.EndPoint,
            };

            //创建主路内侧管线
            Line innerUpLine = new Line()
            {
                StartPoint = mainSmallEndLine.StartPoint,
                EndPoint = branchOuterArc.EndPoint,
            };
            Line innerBelowLine = new Line()
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
            var tee_center_line = new DBObjectCollection() {new Line(Point3d.Origin, Get_mid_point(branchEndLine)),
                                                            new Line(Point3d.Origin, Get_mid_point(mainBigEndLine)),
                                                            new Line(Point3d.Origin, Get_mid_point(mainSmallEndLine))};
            return new Line_Info(tee_outline, endlines, tee_center_line);
        }
        public static Line_Info Create_elbow(double angle, double width)
        {
            double extend_len = 50;
            double cos_angle = Math.Cos(angle);
            double sin_angle = Math.Sin(angle);
            Point3d center_point = new Point3d(-0.7 * width, -Math.Abs(0.7 * width * Math.Tan(0.5 * angle)), 0);
            //创建弯头内外侧圆弧
            Arc outerarc = new Arc(center_point, 1.2 * width, 0, angle);
            Arc innerarc = new Arc(center_point, 0.2 * width, 0, angle);
            //创建弯头两端的50mm延申段
            Line outerendextendline = new Line()
            {
                StartPoint = outerarc.EndPoint,
                EndPoint = outerarc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            Line innerendextendline = new Line()
            {
                StartPoint = innerarc.EndPoint,
                EndPoint = innerarc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            Line outerstartextendline = new Line()
            {
                StartPoint = outerarc.StartPoint,
                EndPoint = outerarc.StartPoint + new Vector3d(0, -extend_len, 0),
            };
            Line innerstartextendline = new Line()
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
            Arc center_arc = new Arc(center_point, 0.7 * width, 0, angle);
            Line center_arc_extendline1 = new Line()
            {
                StartPoint = center_arc.EndPoint,
                EndPoint = center_arc.EndPoint + new Vector3d(-extend_len * sin_angle, extend_len * cos_angle, 0),
            };
            Line center_arc_extendline2 = new Line()
            {
                StartPoint = center_arc.StartPoint,
                EndPoint = center_arc.StartPoint + new Vector3d(0, -extend_len, 0),
            };
            var elbow_centerline = new DBObjectCollection() { center_arc,
                                                              center_arc_extendline1,
                                                              center_arc_extendline2};
            var flg1 = new Line(innerstartextendline.EndPoint, outerstartextendline.EndPoint);
            Vector3d dir_vec = (flg1.EndPoint - flg1.StartPoint).GetNormal();
            flg1 = new Line(flg1.StartPoint - dir_vec * extend_len, flg1.EndPoint + dir_vec * extend_len);
            var flg2 = new Line(innerendextendline.EndPoint, outerendextendline.EndPoint);
            dir_vec = (flg2.EndPoint - flg2.StartPoint).GetNormal();
            flg2 = new Line(flg2.StartPoint - dir_vec * extend_len, flg2.EndPoint + dir_vec * extend_len);
            var elbow_flg = new DBObjectCollection() { flg1, flg2 };
            return new Line_Info(elbow_outline, elbow_flg, elbow_centerline);
        }
        public static void Get_duct_geo_flg_center_line( Line l,
                                                         double width,
                                                         double angle,
                                                         Point3d center_point,
                                                         out DBObjectCollection geo,
                                                         out DBObjectCollection flg,
                                                         out DBObjectCollection center_line)
        {
            geo = new DBObjectCollection();
            flg = new DBObjectCollection();
            center_line = new DBObjectCollection();
            var lines = Create_duct(l.Length, width);
            Matrix3d mat = Matrix3d.Displacement(center_point.GetAsVector()) * Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
            var l1 = lines[0] as Line;
            l1.TransformBy(mat);
            geo.Add(l1);
            var l2 = lines[1] as Line;
            l2.TransformBy(mat);
            geo.Add(l2);
            flg.Add(new Line(l1.StartPoint, l2.StartPoint));
            flg.Add(new Line(l1.EndPoint, l2.EndPoint));
            center_line.Add(l);
        }
        public static DBObjectCollection Create_duct(double length, 
                                                     double width)
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

        public static DBObjectCollection Create_reducing_duct_geo(double length, double big_width, double small_width)
        {
            //绘制辅助中心线
            Line center_line = new Line()
            {
                StartPoint = new Point3d(-length * 0.5, 0, 0),
                EndPoint = new Point3d(length * 0.5, 0, 0),
            };
            Point3d p1 = center_line.StartPoint + new Vector3d(0, big_width * 0.5, 0);
            Point3d p2 = center_line.StartPoint + new Vector3d(0, -big_width * 0.5, 0);
            Point3d p3 = center_line.EndPoint + new Vector3d(0, small_width * 0.5, 0);
            Point3d p4 = center_line.EndPoint + new Vector3d(0, -small_width * 0.5, 0);
            Line up_hypotenuse = new Line(p1, p3);
            Line down_hypotenuse = new Line(p2, p4);
            return new DBObjectCollection()
            {
                up_hypotenuse,
                down_hypotenuse
            };
        }
        public static DBObjectCollection Create_reducing_duct_flg(DBObjectCollection reducing_duct_geo)
        {
            Line up_hypotenuse = reducing_duct_geo[0] as Line;
            Line down_hypotenuse = reducing_duct_geo[1] as Line;

            return new DBObjectCollection()
            {
                new Line(up_hypotenuse.StartPoint, down_hypotenuse.StartPoint),
                new Line(up_hypotenuse.EndPoint, down_hypotenuse.EndPoint)
            };
        }
        public static DBObjectCollection Create_port()
        {
            double width = 320 * 0.5;
            double height = 640 * 0.5;
            Point3d corner1 = new Point3d(-width, -height, 0);
            Point3d corner2 = new Point3d( width, -height, 0);
            Point3d corner3 = new Point3d( width,  height, 0);
            Point3d corner4 = new Point3d(-width,  height, 0);

            return new DBObjectCollection()
            {
                new Line(corner1, corner2),
                new Line(corner2, corner3),
                new Line(corner3, corner4),
                new Line(corner4, corner1),
                new Line(corner4, corner2),
            };
        }
        private static Point3d Get_mid_point(Line l)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            return new Point3d((srt_p.X + end_p.X) * 0.5, (srt_p.Y + end_p.Y) * 0.5, 0);
        }
    }
}
