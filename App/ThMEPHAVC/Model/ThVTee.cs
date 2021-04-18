using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThVTee
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public double W { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double H { get; set; }

        /// <summary>
        /// 中心点到起始点的距离为K * 100
        /// </summary>
        public double K { get; set; }

        public ThVTee(double width, double height, double step)
        {
            K = step;
            W = width;
            H = height;
        }

        public void RunVTeeDrawEngine(ThDbModelFan fanmodel, Duct_InParam info, string line_type, Vector2d rot_vec, Vector3d dis_vec)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string duct_layer = ThDuctUtils.DuctLayerName(modelLayer);
            string text_layer = ThDuctUtils.DuctTextLayerName(modelLayer);
            string flange_layer = ThDuctUtils.FlangeLayerName(modelLayer);

            List<ThIfcDistributionElement> vtee_seg = Create_vt_duct(rot_vec.Angle, dis_vec, info);
            DrawVTeeDWG(vtee_seg, duct_layer, flange_layer, line_type);
            Insert_text_info(info, text_layer, info.text_size_info, rot_vec, dis_vec);
        }
        public ThIfcDistributionElement CreateVTeeBlock(Duct_InParam info)
        {
            return new ThIfcDistributionElement()
            {
                FlangeLine = CreateVTeeFlange(),
                Representation = CreateVTeeGeo()
            };
        }
        private DBText Create_vt_info(Duct_InParam info)
        {
            string str;
            string text_size = info.text_size_info;
            string elevation = info.elevation_info;
            string tee_info = info.tee_info;
            if (string.IsNullOrEmpty(elevation))
            {
                str = $"{tee_info} (h+X.XXm)";
            }
            else
            {
                double num = Double.Parse(elevation);
                if (num > 0)
                    str = $"{tee_info} (h+" + num.ToString("0.00") + "m)";
                else
                    str = $"{tee_info} (h" + num.ToString("0.00") + "m)";

            }
            double h = 450;
            if (text_size != null)
            {
                if (text_size == "1:100")
                    h = 300;
                else if (text_size == "1:50")
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
                Position = new Point3d(0, 0, 0),
                Rotation = 0,
            };

            return infortext;
        }

        private DBObjectCollection CreateVTeeFlange()
        {
            double extCoef = K > 20 ? 20 : K;
            double hw = W / 2;
            double hh = H / 2;
            double ext = 45;

            Point3d dL = new Point3d(-hw - ext, -hh - ext, 0);
            Point3d uL = new Point3d(-hw - ext, hh + ext, 0);
            Point3d dR = new Point3d(hw + ext, -hh - ext, 0);
            Point3d uR = new Point3d(hw + ext, hh + ext, 0);

            var points = new Point3dCollection() { uR, dR, dL, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            Line intverL = new Line(uL + new Vector3d(-ext, 0, 0),
                                    dL + new Vector3d(-ext, 0, 0));
            double wallLen = extCoef * 100 + 350;//350-> is half fan len
            DBObjectCollection dbobj1 = new DBObjectCollection() { frame, intverL };
            foreach (Curve c in dbobj1)
            {
                c.TransformBy(Matrix3d.Displacement(new Vector3d(wallLen, 0, 0)));
            }
            Polyline cframe = frame.Clone() as Polyline;
            Line cintverL = intverL.Clone() as Line;
            DBObjectCollection dbobj2 = new DBObjectCollection() { cframe, cintverL };

            foreach (Curve c in dbobj2)
            {
                c.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, hh, 0), new Point3d(0, -hh, 0))));
            }
            return new DBObjectCollection() { frame, intverL, cframe, cintverL };
        }
        private DBObjectCollection CreateVTeeGeo()
        {
            double extCoef = K > 20 ? 20 : K;
            double hw = W / 2;
            double hh = H / 2;

            Point3d dL = new Point3d(-hw, -hh, 0);
            Point3d uL = new Point3d(-hw, hh, 0);
            Point3d dR = new Point3d(hw, -hh, 0);
            Point3d uR = new Point3d(hw, hh, 0);
            var points = new Point3dCollection() { uR, dL, dR, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            Line closeL1 = new Line(uL, dL);
            Line closeL2 = new Line(uR, dR);

            double wallLen = extCoef * 100 + 350;//350-> is half fan len
            Line w1 = new Line(dL, new Point3d(-wallLen, -hh, 0));
            Line w2 = new Line(uL, new Point3d(-wallLen, hh, 0));
            DBObjectCollection dbobj1 = new DBObjectCollection() { frame, closeL1, closeL2, w1, w2 };
            foreach (Curve c in dbobj1)
            {
                c.TransformBy(Matrix3d.Displacement(new Vector3d(wallLen, 0, 0)));
            }
            Polyline cframe = frame.Clone() as Polyline;
            Line ccloseL1 = closeL1.Clone() as Line;
            Line ccloseL2 = closeL2.Clone() as Line;
            Line cw1 = w1.Clone() as Line;
            Line cw2 = w2.Clone() as Line;
            DBObjectCollection dbobj2 = new DBObjectCollection() { cframe, ccloseL1, ccloseL2, cw1, cw2 };

            foreach (Curve c in dbobj2)
            {
                c.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, hh, 0), new Point3d(0, -hh, 0))));
            }

            return new DBObjectCollection() { frame, closeL1, closeL2, w1, w2,
                                             cframe, ccloseL1, ccloseL2, cw1, cw2 };
        }
        private List<ThIfcDistributionElement> Create_vt_duct(double angle,
                                                              Vector3d disVec,
                                                              Duct_InParam info)
        {
            var ductSegment = CreateVTeeBlock(info);
            List<ThIfcDistributionElement> OutletVTeeSeg = new List<ThIfcDistributionElement>();
            ductSegment.Matrix = Matrix3d.Displacement(disVec) *
                                 Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            OutletVTeeSeg.Add(ductSegment);
            return OutletVTeeSeg;
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
        private ObjectId CreateVTeelinetype(string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLinetype(linetype, true);
                return acadDatabase.Linetypes.ElementOrDefault(linetype).ObjectId;
            }
        }
        private void DrawVTeeDWG(List<ThIfcDistributionElement> duct_degments,
                                 string duct_layer,
                                 string flange_layer,
                                 string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var linetypeId = CreateVTeelinetype(linetype);
                foreach (var Segment in duct_degments)
                {
                    // 绘制风管
                    var layerId = CreateLayer(duct_layer);
                    foreach (Curve dbobj in Segment.Representation)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }
                    // 绘制法兰线
                    layerId = CreateLayer(flange_layer);
                    foreach (Curve dbobj in Segment.FlangeLine)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }
                }
            }
        }
        private void Insert_text_info(Duct_InParam info, string text_layer, string text_size, Vector2d rot_vec, Vector3d dis_vec)
        {
            DBText text_info = Create_vt_info(info);
            if (text_info.IsNull())
                return;
            double duct_width = Get_duct_width(info.in_duct_info);
            double ro_angle = rot_vec.Angle;

            Move_to_origin(ref text_info, ro_angle, duct_width);
            Get_text_oft_vec(info.text_size_info, duct_width, rot_vec, out Vector2d oft_v_vec, out Vector2d oft_h_vec, ref ro_angle);

            Set_text_info(text_layer, ref text_info, out DBText duct_size_info);



            Matrix3d mat = Matrix3d.Displacement(new Vector3d(dis_vec.X + oft_v_vec.X, dis_vec.Y + oft_v_vec.Y, 0)) *
                           Matrix3d.Rotation(ro_angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            duct_size_info.TransformBy(mat);

            Matrix3d dis_mat = Matrix3d.Displacement(new Vector3d(oft_h_vec.X, oft_h_vec.Y, 0)) * mat;
            text_info.TransformBy(dis_mat);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.ModelSpace.Add(duct_size_info);
                acadDatabase.ModelSpace.Add(text_info);

                text_info.SetDatabaseDefaults();
            }
        }

        private void Set_text_info(string text_layer, ref DBText text_info, out DBText duct_size_info)
        {
            var textlayerId = CreateLayer(text_layer);
            var textstyleId = CreateDuctTextStyle();
            text_info.LayerId = textlayerId;
            text_info.TextStyleId = textstyleId;

            duct_size_info = text_info.Clone() as DBText;
            if (duct_size_info == null)
                return;

            string[] str = text_info.TextString.Split(' ');
            if (str.Length != 2)
                return;
            text_info.TextString = str[1];
            duct_size_info.TextString = str[0];
        }

        private void Move_to_origin(ref DBText info_text, double angle, double duct_width)
        {
            if (!info_text.IsNull())
            {
                var textbounding = info_text.GeometricExtents;
                double dis_x = (textbounding.MaxPoint.X + textbounding.MinPoint.X) * 0.5;
                double dis_y = (textbounding.MaxPoint.Y + textbounding.MinPoint.Y) * 0.5;
                info_text.Position = new Point3d(-dis_x * 0.5, -dis_y * 0.5, 0);
            }
        }
        private double Get_duct_width(string duct_info)
        {
            string[] duct_size = duct_info.Split('x');
            if (duct_size.Length != 2)
            {
                return 0;
            }
            return Double.Parse(duct_size[0]);
        }
        private void Get_text_oft_vec(string text_size, double duct_width, Vector2d rot_vec,
                                      out Vector2d oft_v_vec, out Vector2d oft_h_vec, ref double ro_angle)
        {
            double oft = duct_width * 0.5 + 200;
            Vector2d v = rot_vec.GetNormal();
            Vector2d rot_left_vec = new Vector2d(-v.Y, v.X);
            Vector2d rot_right_vec = new Vector2d(v.Y, -v.X);
            oft_v_vec = rot_left_vec * oft;
            double seperate_dis = 2000;
            if (text_size != null)
            {
                if (text_size == "1:100")
                    seperate_dis = 1300;
                else if (text_size == "1:50")
                    seperate_dis = 700;
            }
            if (ro_angle > Math.PI * 0.5 && ro_angle <= Math.PI * 1.5)
            {
                ro_angle += Math.PI;
                oft_v_vec = rot_right_vec * oft;
                seperate_dis = -seperate_dis;
            }

            oft_h_vec = v * seperate_dis;
        }
        private ObjectId CreateDuctTextStyle()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportTextStyle(ThHvacCommon.DUCT_TEXT_STYLE);
                return acadDatabase.TextStyles.ElementOrDefault(ThHvacCommon.DUCT_TEXT_STYLE).ObjectId;
            }
        }
    }
}
