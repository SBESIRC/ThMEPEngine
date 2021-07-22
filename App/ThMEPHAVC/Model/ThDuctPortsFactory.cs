using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPHVAC.Model
{
    public class Line_Info
    {
        public DBObjectCollection geo;
        public DBObjectCollection flg;
        public List<Point3d> ports;
        public List<Point3d> ports_ext;
        public DBObjectCollection center_line;
        public Line_Info(){ }
        public Line_Info(DBObjectCollection geo, 
                         DBObjectCollection flg, 
                         DBObjectCollection center_line,
                         List<Point3d> ports,
                         List<Point3d> ports_ext)
        {
            this.geo = geo;
            this.flg = flg;
            this.ports = ports;
            this.ports_ext = ports_ext;
            this.center_line = center_line;
        }
    }
    public class ThDuctPortsFactory
    {
        public static Line_Info Create_reducing(DBObjectCollection seg_outlines)
        {
            double extend = 45;
            var geo = new DBObjectCollection();
            var flg = new DBObjectCollection();
            var center_line = new DBObjectCollection();
            var pre_up = seg_outlines[0] as Line;
            var pre_down = seg_outlines[1] as Line;
            var latter_up = seg_outlines[2] as Line;
            var latter_down = seg_outlines[3] as Line;
            var pre_up_port = pre_up.EndPoint;
            var pre_down_port = pre_down.EndPoint;
            var latter_up_port = (latter_up.Length - 1 < 1e-3) ? latter_up.EndPoint : latter_up.StartPoint;
            var latter_down_port = (latter_down.Length - 1 < 1e-3) ? latter_down.EndPoint : latter_down.StartPoint;
            geo.Add(new Line(pre_up_port, latter_up_port));
            geo.Add(new Line(pre_down_port, latter_down_port));
            Vector3d dir_vec = (pre_up_port - pre_down_port).GetNormal();
            flg.Add(new Line(pre_up_port + dir_vec * extend, pre_down_port - dir_vec * extend));
            dir_vec = (latter_up_port - latter_down_port).GetNormal();
            flg.Add(new Line(latter_up_port + dir_vec * extend, latter_down_port - dir_vec * extend));
            center_line.Add(new Line(ThDuctPortsService.Get_mid_point(pre_up_port, pre_down_port),
                                     ThDuctPortsService.Get_mid_point(latter_up_port, latter_down_port)));
            ThDuctPortsService.Get_ports(center_line[0] as Line, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(geo, flg, center_line, ports, ports_ext);
        }
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
            double ext = 45;
            var cross_flg = new DBObjectCollection() { ThDuctPortsService.Extend_line(mainBigEndLine, ext),
                                                       ThDuctPortsService.Extend_line(mainSmallEndLine, ext),
                                                       ThDuctPortsService.Extend_line(sideBigEndLine, ext),
                                                       ThDuctPortsService.Extend_line(sideSmallEndLine, ext)};
            var cross_centerline = new DBObjectCollection() { new Line(Point3d.Origin, Get_mid_point(mainBigEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(mainSmallEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(sideBigEndLine)),
                                                              new Line(Point3d.Origin, Get_mid_point(sideSmallEndLine))};

            ThDuctPortsService.Get_ports(cross_centerline.Cast<Line>().ToList(), out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(cross_geo, cross_flg, cross_centerline, ports, ports_ext);
        }
        public static Line_Info Create_tee(double main_width, double branch, double other, Tee_Type type)
        {
            if (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER)
                return Create_r_tee_outlines(main_width, branch, other);
            else
                return Create_v_tee_outlines(main_width, branch, other);
        }
        private static Line_Info Create_r_tee_outlines(double main_width, double branch, double other)
        {
            var endlines = Create_r_tee_endline(main_width, branch, other);
            return Create_r_tee_geometries(endlines, main_width, branch);
        }
        private static Line_Info Create_v_tee_outlines(double main_width, double branch, double other)
        {
            var endlines = Create_v_tee_endline(main_width, branch, other);
            return Create_v_tee_geometries(endlines, main_width, branch, other);
        }
        private static DBObjectCollection Create_v_tee_endline(double main_width, double branch, double other)
        {
            //右向弯管
            double xOft = (main_width + branch) * 0.5 + 50;
            double yOft = 0.5 * branch;
            double ext = 45;
            var branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = -(main_width + other) * 0.5 - 50;
            yOft = 0.5 * other;
            //左向弯管
            var mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * main_width;
            double branch_width = branch > other ? branch : other;
            yOft = -branch_width - 50;
            //输入
            var mainBigEndLine = new Line()
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
        private static DBObjectCollection Create_r_tee_endline(double main_width, double branch, double other)
        {
            //创建支路端线
            double xOft = (main_width + branch) * 0.5 + 50;
            double yOft = 0.5 * branch;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * other;
            yOft = 0.5 * branch + 100;
            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };
            xOft = 0.5 * main_width;
            yOft = -branch - 50;
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
            var left_circle_cp = new Point3d(-R1, -l_width, 0);
            var right_circle_cp = new Point3d(R2, -r_width, 0);
            var left_inner_arc = new Arc(left_circle_cp, 0.5 * l_width, 0, 0.5 * Math.PI);
            var right_inner_arc = new Arc(right_circle_cp, 0.5 * r_width, 0.5 * Math.PI, Math.PI);
            var floor_left = new Point3d(-0.5 * i_width, -l_width - 50, 0);
            var floor_right = new Point3d(0.5 *  i_width, -r_width - 50, 0);
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
            var branchEndLine = endlines[0] as Line;
            var mainBigEndLine = endlines[1] as Line;
            var mainSmallEndLine = endlines[2] as Line;
            var org_p = Point3d.Origin;
            var tee_center_line = new DBObjectCollection() { new Line(org_p, Get_mid_point(branchEndLine)),
                                                             new Line(org_p, Get_mid_point(mainBigEndLine)),
                                                             new Line(org_p, Get_mid_point(mainSmallEndLine))};
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
                ThDuctPortsService.Get_ports(tee_center_line.Cast<Line>().ToList(), out List<Point3d> ports1, out List<Point3d> ports_ext1);
                return new Line_Info(outline, endlines, tee_center_line, ports1, ports_ext1);
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
            ThDuctPortsService.Get_ports(tee_center_line.Cast<Line>().ToList(), out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(tee_outline, endlines, tee_center_line, ports, ports_ext);
        }
        private static Line_Info Create_r_tee_geometries(DBObjectCollection endlines, double i_width, double o_width1)
        {
            double ext = 45;
            var branchEndLine = endlines[0].Clone() as Line;
            branchEndLine.StartPoint += new Vector3d(0, -ext, 0);
            branchEndLine.EndPoint += new Vector3d(0, ext, 0);
            var mainBigEndLine = endlines[1].Clone() as Line;
            mainBigEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainBigEndLine.EndPoint += new Vector3d(ext, 0, 0);
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
            var tee_center_line = new DBObjectCollection() {new Line(Point3d.Origin, Get_mid_point(branchEndLine)),
                                                            new Line(Point3d.Origin, Get_mid_point(mainBigEndLine)),
                                                            new Line(Point3d.Origin, Get_mid_point(mainSmallEndLine))};
            ThDuctPortsService.Get_ports(tee_center_line.Cast<Line>().ToList(), out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(tee_outline, endlines, tee_center_line, ports, ports_ext);
        }
        public static Line_Info Create_elbow(double angle, double width)
        {
            double extend_len = 50;
            double cos_angle = Math.Cos(angle);
            double sin_angle = Math.Sin(angle);
            var center_point = new Point3d(-0.7 * width, -Math.Abs(0.7 * width * Math.Tan(0.5 * angle)), 0);
            //创建弯头内外侧圆弧
            var outerarc = new Arc(center_point, 1.2 * width, 0, angle);
            var innerarc = new Arc(center_point, 0.2 * width, 0, angle);
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
            var center_arc = new Arc(center_point, 0.7 * width, 0, angle);
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
            extend_len = 45;
            flg1 = new Line(flg1.StartPoint - dir_vec * extend_len, flg1.EndPoint + dir_vec * extend_len);
            var flg2 = new Line(innerendextendline.EndPoint, outerendextendline.EndPoint);
            dir_vec = (flg2.EndPoint - flg2.StartPoint).GetNormal();
            flg2 = new Line(flg2.StartPoint - dir_vec * extend_len, flg2.EndPoint + dir_vec * extend_len);
            var elbow_flg = new DBObjectCollection() { flg1, flg2 };
            var tmp_center_line = new DBObjectCollection() { elbow_centerline[1], elbow_centerline[2]};
            ThDuctPortsService.Get_ports(tmp_center_line.Cast<Line>().ToList(), out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(elbow_outline, elbow_flg, elbow_centerline, ports, ports_ext);
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
        public static Line_Info Create_duct(Point2d sp, Point2d ep, double width)
        {
            var aux_line = new Line() { StartPoint = sp.ToPoint3d(), EndPoint = ep.ToPoint3d()};
            var dir_vec = ThDuctPortsService.Get_edge_direction(aux_line);
            var l_v_vec = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            var r_v_vec = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            var sp1 = aux_line.StartPoint + l_v_vec * 0.5 * width;
            var sp2 = aux_line.StartPoint + r_v_vec * 0.5 * width;
            var ep1 = aux_line.EndPoint + l_v_vec * 0.5 * width;
            var ep2 = aux_line.EndPoint + r_v_vec * 0.5 * width;

            var geo = new DBObjectCollection() { new Line(sp1, ep1), new Line(sp2, ep2) };
            sp1 += l_v_vec * 50;
            sp2 += r_v_vec * 50;
            ep1 += l_v_vec * 50;
            ep2 += r_v_vec * 50;
            var flg = new DBObjectCollection() { new Line(sp1, sp2), new Line(ep1, ep2) };
            var center_line = new DBObjectCollection() { aux_line };
            ThDuctPortsService.Get_ports(aux_line, out List<Point3d> ports, out List<Point3d> ports_ext);
            return new Line_Info(geo, flg, center_line, ports, ports_ext);
        }
        public static DBObjectCollection Create_duct(double length, double width)
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
        public static DBObjectCollection Create_reducing_geo(Line center_line, double big_width, double small_width)
        {
            var dir_vec = ThDuctPortsService.Get_edge_direction(center_line);
            var left = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            var right = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            var p1 = center_line.StartPoint + left * big_width * 0.5;
            var p2 = center_line.StartPoint + right * big_width * 0.5;
            var p3 = center_line.EndPoint + left * small_width * 0.5;
            var p4 = center_line.EndPoint + right * small_width * 0.5;
            var up_hypotenuse = new Line(p1, p3);
            var down_hypotenuse = new Line(p2, p4);
            return new DBObjectCollection()
            {
                up_hypotenuse,
                down_hypotenuse
            };
        }
        public static DBObjectCollection Create_reducing_flg(DBObjectCollection reducing_duct_geo)
        {
            double dis = 45;
            var up_hypotenuse = reducing_duct_geo[0] as Line;
            var down_hypotenuse = reducing_duct_geo[1] as Line;
            var l1 = new Line(up_hypotenuse.StartPoint, down_hypotenuse.StartPoint);
            var dir_vec = ThDuctPortsService.Get_edge_direction(l1);
            var dis_vec = dis * dir_vec;
            l1 = new Line(l1.StartPoint - dis_vec, l1.EndPoint + dis_vec);
            var l2 = new Line(up_hypotenuse.EndPoint, down_hypotenuse.EndPoint);
            dir_vec = ThDuctPortsService.Get_edge_direction(l2);
            dis_vec = dis * dir_vec;
            l2 = new Line(l2.StartPoint - dis_vec, l2.EndPoint + dis_vec);
            return new DBObjectCollection() { l1, l2 };
        }
        public static DBObjectCollection Create_reducing_center(Line l)
        {
            return new DBObjectCollection() { new Line(l.StartPoint, l.EndPoint) };
        }
        private static Point3d Get_mid_point(Line l)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            return new Point3d((srt_p.X + end_p.X) * 0.5, (srt_p.Y + end_p.Y) * 0.5, 0);
        }
    }
}