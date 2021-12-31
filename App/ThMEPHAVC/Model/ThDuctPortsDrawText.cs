using System;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawText
    {
        private string ductSizeLayer;
        private string ductSizeStyle;
        public ThDuctPortsDrawText(string ductSizeLayer)
        {
            ductSizeStyle = "TH-STYLE3";
            this.ductSizeLayer = ductSizeLayer;
        }
        public void DrawDuctText(DuctModifyParam param, string scale)
        {
            ThMEPHVACService.GetWidthAndHeight(param.ductSize, out double w, out double h);
            var sp = new Point3d(param.sp.X, param.sp.Y, 0);
            var ep = new Point3d(param.ep.X, param.ep.Y, 0);
            var l = new Line(sp, ep);
            var text = CreateDuctInfo(param.elevation, h, scale, param.ductSize);
            ThMEPHVACService.GetLinePosInfo(l, out double angle, out Point3d centerPoint);
            var mat = GetSideTextInfoTransMat(angle, w, centerPoint, text, l);
            var dirVec = ThMEPHVACService.GetEdgeDirection(l);
            SeperateDuctSizeElevation(scale, text, mat, dirVec, out DBText ductSizeText, out DBText elevationSize);
            var ductSizeInfo = new List<DBText> { ductSizeText, elevationSize };
            DrawDuctSizeInfo(ductSizeInfo);
        }
        public void GetMainDuctInfo(FanParam param,
                                    List<TextAlignLine> textAlignment, 
                                    double mainHeight,
                                    out List<DBText> ductSizeInfo)
        {
            ductSizeInfo = new List<DBText>();
            var roomElevation = Double.Parse(param.roomElevation);
            var notRoomElevation = Double.Parse(param.notRoomElevation);
            
            foreach (var t in textAlignment)
            {
                var elevation = t.isRoom ? roomElevation : notRoomElevation;
                var text = CreateDuctInfo(elevation, mainHeight, param.scale, t.ductSize);
                ThMEPHVACService.GetLinePosInfo(t.l, out double angle, out Point3d centerPoint);
                var mat = GetMainTextInfoTransMat(angle, centerPoint, text);
                var dirVec = ThMEPHVACService.GetEdgeDirection(t.l);
                var verticalVec = -ThMEPHVACService.GetVerticalVec(dirVec);
                mat = Matrix3d.Displacement(verticalVec * text.Height * 0.5) * mat;// 移动到管子中间
                SeperateDuctSizeElevation(param.scale, text, mat, dirVec, out DBText ductSizeText, out DBText elevationSize);
                ductSizeInfo.Add(ductSizeText);
                ductSizeInfo.Add(elevationSize);
            }
        }
        // 布风口的一侧一定是服务侧，不需要区分
        public void GetEndLineDuctTextInfo(ThMEPHVACParam param,
                                           List<TextAlignLine> textAlignment,
                                           out List<DBText> ductSizeInfo)
        {
            var roomElevation = param.elevation;
            ductSizeInfo = new List<DBText>();
            var mainHeight = ThMEPHVACService.GetHeight(param.inDuctSize);
            foreach (var t in textAlignment)
            {
                var w = ThMEPHVACService.GetWidth(t.ductSize);
                var text = CreateDuctInfo(roomElevation, mainHeight, param.scale, t.ductSize);
                ThMEPHVACService.GetLinePosInfo(t.l, out double angle, out Point3d centerPoint);
                var mat = GetSideTextInfoTransMat(angle, w, centerPoint, text, t.l);
                var dirVec = ThMEPHVACService.GetEdgeDirection(t.l);
                SeperateDuctSizeElevation(param.scale, text, mat, dirVec, out DBText ductSizeText, out DBText elevationSize);
                if (t.l.Length > 500)
                {
                    ductSizeInfo.Add(ductSizeText);
                    ductSizeInfo.Add(elevationSize);
                }
            }
        }
        private Matrix3d GetSideTextInfoTransMat(double rotateAngle, double ductWidth, Point3d centerPoint, DBText text, Line curLine)
        {
            var dirVec = ThMEPHVACService.GetEdgeDirection(curLine);
            var verticalVec = ThMEPHVACService.GetVerticalVec(dirVec);
            var leaveDuctMat = verticalVec * (ductWidth * 0.5 + 250);
            var mainMat = GetMainTextInfoTransMat(rotateAngle, centerPoint, text);
            mainMat = Matrix3d.Displacement(-verticalVec * text.Height * 0.5) * mainMat;//Correct to pipe center
            return Matrix3d.Displacement(leaveDuctMat) * mainMat;
        }
        public void DrawMainlineTextInfo(double angle, string ductSize, Point3d centerPoint, Vector3d dirVec, Matrix3d orgDisMat, PortParam portParam)
        {
            double mainHeight = ThMEPHVACService.GetHeight(portParam.param.inDuctSize);
            var text = CreateDuctInfo(portParam.param.elevation, mainHeight, portParam.param.scale, ductSize);
            var mat = GetMainTextInfoTransMat(angle, centerPoint, text);
            var verticalVec = -ThMEPHVACService.GetVerticalVec(dirVec);
            mat = orgDisMat * Matrix3d.Displacement(verticalVec * text.Height * 0.5) * mat;// 移动到管子中间
            SeperateDuctSizeElevation(portParam.param.scale, text, mat, dirVec, out DBText ductSizeText, out DBText elevationSize);
            DrawText(ductSizeText);
            DrawText(elevationSize);
        }
        private Matrix3d GetMainTextInfoTransMat(double rotateAngle, Point3d centerPoint, DBText text)
        {
            while (rotateAngle > 0.5 * Math.PI && (rotateAngle - 0.5 * Math.PI) > 1e-3)
                rotateAngle -= Math.PI;
            double text_len = (text.Bounds == null) ? 0 : (text.Bounds.Value.MaxPoint.X - text.Bounds.Value.MinPoint.X);
            return Matrix3d.Displacement(centerPoint.GetAsVector()) *
                   Matrix3d.Rotation(rotateAngle, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Displacement(new Vector3d(-0.5 * text_len, 0, 0));
        }
        public void DrawDuctSizeInfo(List<DBText> ductSizeInfo)
        {
            foreach (var info in ductSizeInfo)
                DrawText(info);
        }
        private void DrawText(DBText text)
        {
            using (var db = AcadDatabase.Active())
            {
                db.ModelSpace.Add(text);
                text.SetDatabaseDefaults();
                text.Layer = ductSizeLayer;
                text.ColorIndex = (int)ColorIndex.BYLAYER;
                text.Linetype = "ByLayer";
            }
        }
        public void DrawTextInfo(string s, string scale, Point3d p)
        {
            double h = ThMEPHVACService.GetTextHeight(scale);
            DrawTextInfo(s, h, p, 0);
        }
        public void DrawTextInfo(string s, double h, Point3d p, double rotation)
        {
            using (var db = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetTextStyleId(ductSizeStyle);
                var text = new DBText()
                {
                    Height = h,
                    Oblique = 0,
                    Rotation = rotation,
                    WidthFactor = 0.7,
                    TextStyleId = id,
                    TextString = s,
                    Position = p,
                    HorizontalMode = TextHorizontalMode.TextLeft
                };
                DrawText(text);
            }
        }
        public DBText CreateDuctInfo(double elevation, double mainHeight, string scale, string ductSize)
        {
            // 不处理main在树间的情况
            double ductHeight = ThMEPHVACService.GetHeight(ductSize);
            double num = (elevation * 1000 + mainHeight - ductHeight) / 1000;
            string textInfo = (num > 0) ? $"{ductSize} (h+" + num.ToString("0.00") + "m)":
                                          $"{ductSize} (h" + num.ToString("0.00") + "m)";
            double h = ThMEPHVACService.GetTextHeight(scale);
            using (var adb = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetTextStyleId(ductSizeStyle);
                return new DBText()
                {
                    Height = h, Oblique = 0, Rotation = 0, WidthFactor = 0.7, TextStyleId = id,
                    TextString = textInfo, Position = Point3d.Origin, 
                    HorizontalMode = TextHorizontalMode.TextLeft
                };
            }
        }
        public static void SeperateDuctSizeElevation(string scale,
                                                     DBText text, 
                                                     Matrix3d mat, 
                                                     Vector3d dirVec, 
                                                     out DBText ductSizeText, 
                                                     out DBText elevationSize)
        {
            string[] str = text.TextString.Split(' ');
            ductSizeText = text.Clone() as DBText;
            elevationSize = text.Clone() as DBText;
            if (str.Length != 2)
                return;
            ductSizeText.TextString = str[0];
            elevationSize.TextString = str[1];
            double seperateDis = ThMEPHVACService.GetTextSepDis(scale);
            ductSizeText.TransformBy(mat);
            if (Math.Abs(dirVec.CrossProduct(-Vector3d.YAxis).Z) < 1e-3)
            {
                if (dirVec.Y > 0)
                    elevationSize.TransformBy(Matrix3d.Displacement(dirVec * seperateDis) * mat);
                else
                    elevationSize.TransformBy(Matrix3d.Displacement(-dirVec * seperateDis) * mat);
            }
            else if (dirVec.CrossProduct(-Vector3d.YAxis).Z > 0)
                elevationSize.TransformBy(Matrix3d.Displacement(-dirVec * seperateDis) * mat);
            else
                elevationSize.TransformBy(Matrix3d.Displacement(dirVec * seperateDis) * mat);
        }
    }
}
