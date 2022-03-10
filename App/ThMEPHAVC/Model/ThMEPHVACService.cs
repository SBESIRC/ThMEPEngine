using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPEngineCore.CAD;

namespace ThMEPHVAC.Model
{
    public class ThMEPHVACService
    {
        public static double GetWidth(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[0]);
        }
        public static double GetHeight(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[1]);
        }
        public static void GetWidthAndHeight(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            width = Double.Parse(s[0]);
            height = Double.Parse(s[1]);
        }
        public static void GetLinePosInfo(Line l, out double angle, out Point3d centerPoint)
        {
            var srt_p = l.StartPoint;
            var end_p = l.EndPoint;
            var edge_vec = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
            angle = edge_vec.Angle;
            centerPoint = GetMidPoint(srt_p, end_p);
        }
        public static Point2d GetVerticalPoint(Point2d p, Line l)
        {
            var mirror = GetMirrorPoint(p, l);
            return GetMidPoint(mirror, p);
        }
        public static Point2d GetMirrorPoint(Point2d p, Line l)
        {
            return p.Mirror(new Line2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D()));
        }
        public static Point2d GetMidPoint(Point2d p1, Point2d p2)
        {
            return new Point2d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
        }
        public static Point3d GetMidPoint(Line l)
        {
            var sp = l.StartPoint;
            var ep = l.EndPoint;
            return new Point3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
        }
        public static Point3d GetMidPoint(Point3d p1, Point3d p2)
        {
            return new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);
        }
        public static Vector3d GetEdgeDirection(Line l)
        {
            var srt_p = l.StartPoint;
            var end_p = l.EndPoint;
            return (end_p - srt_p).GetNormal();
        }
        public static Vector3d GetLeftVerticalVec(Vector3d dir_vec)
        {
            return new Vector3d(-dir_vec.Y, dir_vec.X, 0);
        }
        public static Vector3d GetRightVerticalVec(Vector3d dir_vec)
        {
            return new Vector3d(dir_vec.Y, -dir_vec.X, 0);
        }
        public static Vector2d GetLeftVerticalVec(Vector2d dir_vec)
        {
            return new Vector2d(-dir_vec.Y, dir_vec.X);
        }
        public static Vector2d GetRightVerticalVec(Vector2d dir_vec)
        {
            return new Vector2d(dir_vec.Y, -dir_vec.X);
        }
        public static Vector3d GetDirVecByAngle3(double angle)
        {
            var v = GetDirVecByAngle(angle);
            return new Vector3d(v.X, v.Y, 0);
        }
        public static Vector2d GetDirVecByAngle(double angle)
        {
            return new Vector2d(Math.Cos(angle), Math.Sin(angle));
        }
        public static bool IsVertical(Vector3d v1, Vector3d v2)
        {
            var angle = v1.GetAngleTo(v2);
            return Math.Abs(angle - Math.PI * 0.5) < (5 / 180.0 * Math.PI);
        }
        public static bool IsVertical(Line l)
        {
            return Math.Abs(l.StartPoint.X - l.EndPoint.X) <= 1e-1;
        }
        public static bool IsHorizontal(Line l)
        {
            return Math.Abs(l.StartPoint.Y - l.EndPoint.Y) <= 1e-1;
        }
        public static bool IsOutter(Vector2d v2_1, Vector2d v2_2)
        {
            var v1 = new Vector3d(v2_1.X, v2_1.Y, 0);
            var v2 = new Vector3d(v2_2.X, v2_2.Y, 0);
            return IsOutter(v1, v2);
        }
        public static bool IsOutter(Vector3d v1, Vector3d v2)
        {
            return v1.CrossProduct(v2).Z > 0;
        }
        public static double GetPortRotateAngle(Vector3d dirVec)
        {
            var judger = -Vector3d.YAxis;
            double angle = dirVec.GetAngleTo(judger);
            var z = judger.CrossProduct(dirVec).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z < 0)
                angle = 2 * Math.PI - angle;
            return angle;
        }
        public static double GetTextHeight(string scale)
        {
            double h = 450;
            if (scale == "1:100")
                h = 300;
            else if (scale == "1:50")
                h = 150;
            return h;
        }
        public static string GetScenarioByGeoLayer(string geoLayer)
        {
            if (geoLayer == "H-DUCT-DUAL")
                return "消防排烟兼平时排风";
            else if (geoLayer == "H-DUCT-FIRE")
                return "消防排烟";
            else if (geoLayer == "H-DUCT-VENT")
                return "平时送风";
            else if (geoLayer == "H-DUCT-ACON")
                return "空调送风";
            else
                throw new NotImplementedException("[CheckError]: No such geo layer!");
        }
        public static string GetScaleByHeight(double height)
        {
            if (Math.Abs(height - 450) < 1e-3)
                return "1:150";
            else if (Math.Abs(height - 300) < 1e-3)
                return "1:100";
            else
                return "1:50";
        }
        public static double GetTextSepDis(string scale)
        {
            double seperate_dis = 2500;
            if (scale == "1:100")
                seperate_dis = 1800;
            else if (scale == "1:50")
                seperate_dis = 1100;
            return seperate_dis - 400;
        }
        public static string GetDimStyle(string scale)
        {
            string style = "TH-DIM150";
            if (scale == "1:100")
                style = "TH-DIM100";
            else if (scale == "1:50")
                style = "TH-DIM50";
            return style;
        }
        public static DuctModifyParam CreateDuctModifyParam(DBObjectCollection centerLine,
                                                            string ductSize,
                                                            string elevation,
                                                            double airVolume)
        {
            if (centerLine.Count > 0)
            {
                var line = centerLine[0] as Line;
                var e = String.IsNullOrEmpty(elevation) ? 0 : Double.Parse(elevation);
                return new DuctModifyParam(ductSize, airVolume, e, line.StartPoint, line.EndPoint);
            }
            else
                throw new NotImplementedException();
        }
        public static bool IsCollinear(Vector3d vec1, Vector3d vec2)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            return vec1.IsEqualTo(vec2, tor) || vec1.IsEqualTo(-vec2, tor);
        }
        public static Vector3d GetVerticalVec(Vector3d dir_vec)
        {
            Vector3d vertical_vec;
            if (Math.Abs(dir_vec.X) < 1e-3)
                vertical_vec = (dir_vec.Y > 0) ? GetLeftVerticalVec(dir_vec) : GetRightVerticalVec(dir_vec);
            else if (dir_vec.X > 0)
                vertical_vec = GetLeftVerticalVec(dir_vec);
            else
                vertical_vec = GetRightVerticalVec(dir_vec);
            return vertical_vec;
        }
        public static bool IsSameLine(Line l1, Line l2)
        {
            var tor = new Tolerance(1.5, 1.5);
            var sp1 = l1.StartPoint;
            var ep1 = l1.EndPoint;
            var sp2 = l2.StartPoint;
            var ep2 = l2.EndPoint;
            return ((sp1.IsEqualTo(sp2, tor) && ep1.IsEqualTo(ep2, tor)) ||
                    (sp1.IsEqualTo(ep2, tor) && ep1.IsEqualTo(sp2, tor)));
        }
        public static bool IsSameLine(Line l1, Line l2, Tolerance pointTor)
        {
            var sp1 = l1.StartPoint;
            var ep1 = l1.EndPoint;
            var sp2 = l2.StartPoint;
            var ep2 = l2.EndPoint;
            return ((sp1.IsEqualTo(sp2, pointTor) && ep1.IsEqualTo(ep2, pointTor)) ||
                    (sp1.IsEqualTo(ep2, pointTor) && ep1.IsEqualTo(sp2, pointTor)));
        }
        public static void GetLongestDis(Line l1, Line l2, out Point3d p1, out Point3d p2)
        {
            GetLongestDis(l1.StartPoint,
                          l1.EndPoint,
                          l2.StartPoint,
                          l2.EndPoint,
                          out p1, out p2);
        }
        public static void GetLongestDis(Point3d sp1, Point3d ep1, Point3d sp2, Point3d ep2, out Point3d p1, out Point3d p2)
        {
            var sp_2D_1 = sp1.ToPoint2D();
            var ep_2D_1 = ep1.ToPoint2D();
            var sp_2D_2 = sp2.ToPoint2D();
            var ep_2D_2 = ep2.ToPoint2D();
            GetLongestDis(sp_2D_1, ep_2D_1, sp_2D_2, ep_2D_2, out Point2d p_2D_1, out Point2d p_2D_2);
            p1 = new Point3d(p_2D_1.X, p_2D_1.Y, 0);
            p2 = new Point3d(p_2D_2.X, p_2D_2.Y, 0);
        }
        public static void GetLongestDis(Point2d sp1, Point2d ep1, Point2d sp2, Point2d ep2, out Point2d p1, out Point2d p2)
        {
            double dis1 = sp1.GetDistanceTo(sp2);
            double dis2 = sp1.GetDistanceTo(ep2);
            double dis3 = ep1.GetDistanceTo(sp2);
            double dis4 = ep1.GetDistanceTo(ep2);
            double[] a = { dis1, dis2, dis3, dis4 };
            double max = a[0];
            int max_idx = 0;
            for (int i = 1; i < 4; ++i)
            {
                if (max < a[i])
                {
                    max_idx = i;
                    max = a[i];
                }
            }
            switch (max_idx)
            {
                case 0: p1 = sp1; p2 = sp2; break;
                case 1: p1 = sp1; p2 = ep2; break;
                case 2: p1 = ep1; p2 = sp2; break;
                case 3: p1 = ep1; p2 = ep2; break;
                default: throw new NotImplementedException();
            }
        }
        public static Line ExtendLine(Line l, double dis)
        {
            var dir_vec = GetEdgeDirection(l);
            var dis_vec = dis * dir_vec;
            var sp = l.StartPoint - dis_vec;
            var ep = l.EndPoint + dis_vec;
            return new Line(sp, ep);
        }
        public static double Extract_decimal(string s)
        {
            s = Regex.Replace(s, @"[^\d.\d]", "");
            if (Regex.IsMatch(s, @"^[+-]?\d*[.]?\d*$"))
                return Double.Parse(s);
            throw new NotImplementedException();
        }
        public static void PromptMsg(string message)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(message);
        }
        public static Polyline GetLineExtend(Point3d sp, Point3d ep, double ext_len)
        {
            var l = new Line(sp, ep);
            return l.Buffer(ext_len * 0.5);
        }
        public static Polyline GetLineExtend(Line l, double ext_len)
        {
            return l.Buffer(ext_len * 0.5);
        }
        public static Point3d RoundPoint(Point3d p, int tailNum)
        {
            var X = Math.Abs(p.X) < 1e-3 ? 0 : p.X;
            var Y = Math.Abs(p.Y) < 1e-3 ? 0 : p.Y;
            return new Point3d(Math.Round(X, tailNum), Math.Round(Y, tailNum), 0);
        }
        public static Point2d RoundPoint(Point2d p, int tailNum)
        {
            double X = Math.Round(p.X, tailNum);
            if (Math.Abs(X) < 1e-3)
                X = 0;
            double Y = Math.Round(p.Y, tailNum);
            if (Math.Abs(Y) < 1e-3)
                Y = 0;
            return new Point2d(X, Y);
        }
        public static void SearchPolyBorder(DBObjectCollection lines, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom)
        {
            top = new Point2d(0, Double.MinValue);
            left = new Point2d(Double.MaxValue, 0);
            right = new Point2d(Double.MinValue, 0);
            bottom = new Point2d(0, Double.MaxValue);
            foreach (Line l in lines)
            {
                UpdateBorder(l.StartPoint.ToPoint2D(), ref top, ref left, ref right, ref bottom);
                UpdateBorder(l.EndPoint.ToPoint2D(), ref top, ref left, ref right, ref bottom);
            }
        }
        private static void UpdateBorder(Point2d p, ref Point2d top, ref Point2d left, ref Point2d right, ref Point2d bottom)
        {
            if (p.X > right.X)
                right = p;
            if (p.X < left.X)
                left = p;
            if (p.Y > top.Y)
                top = p;
            if (p.Y < bottom.Y)
                bottom = p;
        }
        public static Point3d IntersectPoint(Line l1, Line l2)
        {
            var tor = new Tolerance(1.5, 1.5);
            if (l1.StartPoint.IsEqualTo(l2.StartPoint, tor) || l1.StartPoint.IsEqualTo(l2.StartPoint, tor))
                return l1.StartPoint;
            if (l1.EndPoint.IsEqualTo(l2.StartPoint, tor) || l1.EndPoint.IsEqualTo(l2.StartPoint, tor))
                return l1.EndPoint;
            var vec1 = l1.EndPoint - l1.StartPoint;
            var vec2 = l2.EndPoint - l2.StartPoint;
            var cross_x = l2.StartPoint.X - l1.StartPoint.X;
            var cross_y = l2.StartPoint.Y - l1.StartPoint.Y;
            var det = vec2.X * vec1.Y - vec2.Y * vec1.X;
            if (Math.Abs(det) < 1e-9)
                return Point3d.Origin;//The two line segmenta are parallel 
            var det_inv = 1.0f / det;
            var S = (vec2.X * cross_y - vec2.Y * cross_x) * det_inv;
            var T = (vec1.X * cross_y - vec1.Y * cross_x) * det_inv;
            if (Math.Abs(S) < 1e-9)
                S = 0;
            if (Math.Abs(T) < 1e-9)
                T = 0;
            if (Math.Abs(S - 1) < 1e-8)
                S = 1;
            if (Math.Abs(T - 1) < 1e-8)
                T = 1;
            if (S < 0 || S > 1 || T < 0 || T > 1)
                return Point3d.Origin;//Intersection not within line segments
            else
                return l1.StartPoint + vec1 * S;
        }
        public static double GetElbowOpenAngle(Line l1, Line l2, Point3d centerP)
        {
            var tor = new Tolerance(1.5, 1.5);

            if ((centerP.IsEqualTo(l1.StartPoint, tor) || centerP.IsEqualTo(l1.EndPoint, tor)) ||
                (centerP.IsEqualTo(l2.StartPoint, tor) || centerP.IsEqualTo(l2.EndPoint, tor)))
            {
                var p1 = GetOtherPoint(l1, centerP, tor);
                var p2 = GetOtherPoint(l2, centerP, tor);
                var ll1 = new Line(centerP, p1);
                var ll2 = new Line(centerP, p2);
                var v1 = GetEdgeDirection(ll1);
                var v2 = GetEdgeDirection(ll2);
                return v1.GetAngleTo(v2); 
            }
            throw new NotImplementedException("[CheckError]: centerP doesn't connect with l1 or l2");
        }
        public static double GetLineDis(Line l1, Line l2)
        {
            var coordinate1 = new Coordinate[] { l1.StartPoint.ToNTSCoordinate(), l1.EndPoint.ToNTSCoordinate() };
            var coordinate2 = new Coordinate[] { l2.StartPoint.ToNTSCoordinate(), l2.EndPoint.ToNTSCoordinate() };
            var ll1 = new LineString(coordinate1);
            var ll2 = new LineString(coordinate2);
            return ll1.Distance(ll2);
        }
        public static double GetLineDis(Point3d line1Sp, Point3d line1Ep, Point3d line2Sp, Point3d line2Ep)
        {
            var l1 = new Line(line1Sp, line1Ep);
            var l2 = new Line(line2Sp, line2Ep);
            return GetLineDis(l1, l2);
        }
        public static Line GetMaxLine(DBObjectCollection lines)
        {
            var max_line = new Line();
            double max_len = 0;
            foreach (Line l in lines)
            {
                if (max_len < l.Length)
                {
                    max_len = l.Length;
                    max_line = l;
                }
            }
            return max_line;
        }
        public static bool IsCross(DBObjectCollection centerLine, DBObjectCollection bypassLine)
        {
            if (bypassLine.Count == 0)
                return false;
            var index = new ThCADCoreNTSSpatialIndex(centerLine);
            foreach (Line l in bypassLine)
            {
                var pl = GetLineExtend(l, 1);
                var res = index.SelectCrossingPolygon(pl);
                if (res.Count > 0)
                    return true;
            }
            return false;
        }
        public static string ClassifyBypassPattern(DBObjectCollection roomLine, DBObjectCollection notRoomLine, DBObjectCollection bypassLine)
        {
            if (bypassLine.Count == 0)
                return "RBType5";// Dash vertical bypass
            var IsCrossRoom = IsCross(roomLine, bypassLine);
            var IsCrossNotRoom = IsCross(notRoomLine, bypassLine);
            if (IsCrossRoom && IsCrossNotRoom)
                return "RBType3";// Connect in and out
            if (IsCrossRoom || IsCrossNotRoom)
                return "RBType2";// Inner line break by a tee
            throw new NotImplementedException("[Check Error]: No such tee pattern!");
        }
        public static Point3d GetOtherPoint(Line l, Point3d p, Tolerance tor)
        {
            return p.IsEqualTo(l.StartPoint, tor) ? l.EndPoint : l.StartPoint;
        }
        public static Polyline CreateDetector(Point3d p, double len)
        {
            var pl = new Polyline();
            pl.CreatePolygon(p.ToPoint2D(), 4, len);
            return pl;
        }
        public static Polyline CreateDetector(Point3d p)
        {
            var pl = new Polyline();
            pl.CreatePolygon(p.ToPoint2D(), 4, 10);
            return pl;
        }
        public static Point3d FindSamePoint(Line l1, Line l2)
        {
            var tor = new Tolerance(1.5, 1.5);
            if (l1.StartPoint.IsEqualTo(l2.StartPoint, tor))
                return l1.StartPoint;
            if (l1.StartPoint.IsEqualTo(l2.EndPoint, tor))
                return l1.StartPoint;
            if (l1.EndPoint.IsEqualTo(l2.StartPoint, tor))
                return l1.EndPoint;
            if (l1.EndPoint.IsEqualTo(l2.EndPoint, tor))
                return l1.EndPoint;
            throw new NotImplementedException("此函数只针对有共点线的情况");
        }
        public static double RoundNum(double num, double round)
        {
            return (Math.Floor(num / round)) * round;
        }
        public static ThCADCoreNTSSpatialIndex CreateRoomOutlineIndex(Point3d srtP)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            var wallBounds = ThDuctPortsReadComponent.GetBoundsByLayer(ThHvacCommon.AI_ROOM_BOUNDS);
            var mpObjs = new DBObjectCollection();
            foreach (Polyline pl in wallBounds)
            {
                pl.DPSimplify(1);
                pl.TransformBy(mat);
                mpObjs.Add(pl.ToNTSPolygon().ToDbMPolygon());
            }
            return new ThCADCoreNTSSpatialIndex(mpObjs);
        }
        public static void SetAttr(ObjectId obj, Dictionary<string, string> attr, double angle)
        {
            var block = obj.GetDBObject<BlockReference>();
            foreach (ObjectId attId in block.AttributeCollection)
            {
                // 获取块参照属性对象
                AttributeReference attRef = attId.GetDBObject<AttributeReference>();
                //判断属性名是否为指定的属性名
                if (attr.Any(c => c.Key.Equals(attRef.Tag)))
                {
                    attRef.Rotation = angle;
                }
            }
        }
        public static string GetADuctSize(double airVolume, string scenario)
        {
            var ductInfo = new ThDuctParameter(airVolume, scenario);
            return ductInfo.DuctSizeInfor.RecommendInnerDuctSize;
        }
        public static DBObjectCollection CastMPolygon2Lines(MPolygon pl)
        {
            var walllines = new DBObjectCollection();
            pl.Explode(walllines);
            var polyline = walllines[0] as Polyline;
            var lines = polyline.ToLines();
            var t = new DBObjectCollection();
            foreach (Line l in lines)
                t.Add(l);
            return t;
        }
        public static Vector3d GetEleDis(double mmElevation, double mainHeight, double height)
        {
            return new Vector3d(0, 0, mmElevation + (mainHeight - height) + height * 0.5);
        }
    }
}