using AcHelper;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using QuickGraph;
using System;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThServiceTee
    {
        internal static bool Is_bypass(Point3d tar_srt_pos,
                                       Point3d tar_end_pos,
                                       DBObjectCollection bypass_lines)
        {
            if (bypass_lines == null || bypass_lines.Count == 0)
                return false;
            Line dect_line = new Line(tar_srt_pos, tar_end_pos);
            foreach (Line l in bypass_lines)
            {
                if (Is_same_line(dect_line, l))
                    return true;
            }
            return false;
        }

        public static bool Is_same_line(Line l1, Line l2)
        {
            Tolerance t = new Tolerance(5, 5);
            return (l1.StartPoint.IsEqualTo(l2.StartPoint, t) && l1.EndPoint.IsEqualTo(l2.EndPoint, t)) ||
                    (l1.StartPoint.IsEqualTo(l2.EndPoint, t) && l1.EndPoint.IsEqualTo(l2.StartPoint, t));
        }
        public static void Fine_tee_duct(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> LineGraph,
                                           double IShrink,
                                           double Shrinkb,
                                           double Shrinkm,
                                           DBObjectCollection bypass_lines)
        {
            foreach (var edge in LineGraph.Edges)
            {
                if (LineGraph.OutDegree(edge.Target) == 2)
                {
                    var out1 = LineGraph.OutEdges(edge.Target).First();
                    var out2 = LineGraph.OutEdges(edge.Target).Last();
                    if (Is_bypass(out1.Source.Position, out1.Target.Position, bypass_lines))
                    {
                        edge.TargetShrink = IShrink;
                        out1.SourceShrink = Shrinkb;
                        out2.SourceShrink = Shrinkm;
                    }
                    else
                    {
                        edge.TargetShrink = IShrink;
                        out1.SourceShrink = Shrinkm;
                        out2.SourceShrink = Shrinkb;
                    }
                    break;
                }
            }
        }

        public void Run_insert_text_info(ThDbModelFan fanmodel, Duct_InParam info, Vector2d rot_vec, Vector3d dis_vec, bool is_vt)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string text_layer = ThDuctUtils.DuctTextLayerName(modelLayer);
            Insert_bypass_text_info(info, text_layer, rot_vec, dis_vec, is_vt);
        }
        private void Insert_bypass_text_info(Duct_InParam info, string text_layer, Vector2d rot_vec, Vector3d dis_vec, bool is_vt)
        {
            DBText text_info = Create_tee_info(info, is_vt);
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
        private DBText Create_tee_info(Duct_InParam info, bool is_vt)
        {
            string str;
            string tee_info = info.tee_info;
            string text_size = info.text_size_info;
            string elevation = info.elevation_info;
            
            if (is_vt)
            {
                str = $"{tee_info} (h+X.XXm)";
            }
            else
            {
                string duct_info = info.out_duct_info;
                double evel = Double.Parse(elevation);
                string[] s = tee_info.Split('x');
                if (s.Length != 2)
                    return new DBText();
                double tee_height = Double.Parse(s[1]);
                s = duct_info.Split('x');
                if (s.Length != 2)
                    return new DBText();
                double duct_height = Double.Parse(s[1]);

                double num = (evel * 1000 + duct_height - tee_height) / 1000;
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
        private double Get_duct_width(string duct_info)
        {
            string[] duct_size = duct_info.Split('x');
            if (duct_size.Length != 2)
            {
                return 0;
            }
            return Double.Parse(duct_size[0]);
        }
        private void Move_to_origin(ref DBText text_info, double ro_angle, double duct_width)
        {
            if (!text_info.IsNull())
            {
                var textbounding = text_info.GeometricExtents;
                double dis_x = (textbounding.MaxPoint.X + textbounding.MinPoint.X) * 0.5;
                double dis_y = (textbounding.MaxPoint.Y + textbounding.MinPoint.Y) * 0.5;
                text_info.Position = new Point3d(-dis_x * 0.5, -dis_y * 0.5, 0);
            }
        }
        private void Get_text_oft_vec(string text_size, double duct_width, Vector2d rot_vec, out Vector2d oft_v_vec, out Vector2d oft_h_vec, ref double ro_angle)
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
        private void Set_text_info(string text_layer, ref DBText text_info, out DBText duct_size_info)
        {
            var textlayerId = Create_layer(text_layer);
            var textstyleId = Create_duct_text_style();
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

        private ObjectId Create_layer(string name)
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
        private ObjectId Create_duct_text_style()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportTextStyle(ThHvacCommon.DUCT_TEXT_STYLE);
                return acadDatabase.TextStyles.ElementOrDefault(ThHvacCommon.DUCT_TEXT_STYLE).ObjectId;
            }
        }
        public ObjectId Insert_electric_valve(Vector3d fan_cp_vec,
                                              double valvewidth,
                                              double angle)
        {
            var e = new ThValve()
            {
                Length = 200,
                Width = valvewidth,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = "H-DAPP-EDAMP",
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = e.ValveBlockName;
                var layerName = e.ValveBlockLayer;
                Active.Database.ImportLayer(layerName);
                Active.Database.ImportValve(blockName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(e.Width, e.WidthPropertyName);
                objId.SetValveModel(e.ValveVisibility);

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Matrix3d mat = Matrix3d.Displacement(fan_cp_vec) *
                               Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat *= Matrix3d.Displacement(new Vector3d(-valvewidth / 2, 125, 0));

                blockRef.TransformBy(mat);
                return objId;
            }
        }
    }
}
