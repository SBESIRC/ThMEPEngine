using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThTee
    {
        // 中心点
        public Point3d CP { get; set; }
        // 支路管道直径
        public double BDmtr { get; set; }
        // 主路大端管道直径
        public double MBDmtr { get; set; }
        // 主路小端管道直径
        public double MSDmtr { get; set; }

        public ThTee(Point3d center_point, 
                     double branch_diameter, 
                     double main_big_diameter, 
                     double main_small_diameter)
        {
            CP = center_point;
            BDmtr = branch_diameter;
            MBDmtr = main_big_diameter;
            MSDmtr = main_small_diameter;
        }
        public void RunTeeDrawEngine(ThDbModelFan fanmodel, Matrix3d mat)
        {
            var flg = CreateTeeFlangeline();
            var geo = CreateTeeGeometries(flg);
            var branchEndLine = flg[0] as Line;
            var mainBigEndLine = flg[1] as Line;
            var mainSmallEndLine = flg[2] as Line;
            var center_line = new DBObjectCollection() {new Line(Point3d.Origin, ThMEPHVACService.Get_mid_point(branchEndLine)),
                                                        new Line(Point3d.Origin, ThMEPHVACService.Get_mid_point(mainBigEndLine)),
                                                        new Line(Point3d.Origin, ThMEPHVACService.Get_mid_point(mainSmallEndLine))};
            var a = new ThIfcDistributionElement
            {
                FlangeLine = flg,
                Representation = geo,
                Centerline = center_line,
                Matrix = mat
            };
            var TeeSegments = new List<ThIfcDistributionElement>() { a };
            string modelLayer = fanmodel.Data.BlockLayer;
            string geo_layer = ThDuctUtils.DuctLayerName(modelLayer);
            string flg_layer = ThDuctUtils.FlangeLayerName(modelLayer);
            string centerline_layer = ThDuctUtils.DuctCenterLineLayerName(modelLayer);

            DrawTeeDWG(TeeSegments, geo_layer, flg_layer, centerline_layer);
        }
        private void DrawTeeDWG( List<ThIfcDistributionElement> tees, 
                                 string geo_layer, 
                                 string flg_layer,
                                 string centerline_layer)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var tee in tees)
                {
                    var mat = tee.Matrix;
                    ThDuctPortsDrawService.Draw_lines(tee.Representation, mat, geo_layer, out ObjectIdList geo_ids);
                    ThDuctPortsDrawService.Draw_lines(tee.FlangeLine, mat, flg_layer, out ObjectIdList flg_ids);
                    ThDuctPortsDrawService.Draw_lines(tee.Centerline, mat, centerline_layer, out ObjectIdList center_ids);
                    GetPort(tee.Centerline, out List<Point3d> pos, out List<Point3d> pos_ext);
                    ThDuctPortsDrawService.Draw_ports(pos, pos_ext, mat, out ObjectIdList ports_ids, out ObjectIdList ext_ports_ids);
                    var param = ThMEPHVACService.Create_special_modify_param("Tee", mat, ObjectId.Null.Handle, tee.FlangeLine, tee.Centerline);
                    ThDuctPortsRecoder.Create_group(geo_ids, flg_ids, center_ids, ports_ids, ext_ports_ids, param);
                }  
            }
        }
        private void GetPort(DBObjectCollection center_line, 
                             out List<Point3d> pos,
                             out List<Point3d> pos_ext)
        {
            pos = new List<Point3d>();
            pos_ext = new List<Point3d>();
            foreach (Line l in center_line)
            {
                pos.Add(l.EndPoint);
                var dir_vec = l.EndPoint.GetAsVector().GetNormal();
                pos_ext.Add(l.EndPoint - dir_vec);
            }
        }
        private DBObjectCollection CreateTeeFlangeline()
        {
            //创建支路端线
            double xOft = (MBDmtr+ BDmtr) * 0.5 + 50;
            double yOft = 0.5 * BDmtr;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * MSDmtr;
            yOft = 0.5 * BDmtr + 100;
            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };
            xOft = 0.5 * MBDmtr;
            yOft = -BDmtr - 50;
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

        private DBObjectCollection CreateTeeGeometries(DBObjectCollection endLines)
        {
            double ext = 45;
            Line branchEndLine = endLines[0].Clone() as Line;
            branchEndLine.StartPoint += new Vector3d(0, -ext, 0);
            branchEndLine.EndPoint += new Vector3d(0, ext, 0);
            Line mainBigEndLine = endLines[1].Clone() as Line;
            mainBigEndLine.StartPoint += new Vector3d(-ext, 0, 0);
            mainBigEndLine.EndPoint += new Vector3d(ext, 0, 0);
            Line mainSmallEndLine = endLines[2].Clone() as Line;
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
            Point3d circleCenter = new Point3d(0.5 * (MBDmtr + BDmtr), -BDmtr, 0);
            Arc branchInnerArc = new Arc(circleCenter, 0.5 * BDmtr, 0.5 * Math.PI, Math.PI);

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
                Radius = 1.5 * BDmtr
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

            return new DBObjectCollection()
            {
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
    }
}
