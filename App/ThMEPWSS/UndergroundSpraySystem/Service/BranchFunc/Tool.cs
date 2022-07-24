using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Service.BranchFunc
{
    public class Tool
    {
        public static bool CheckValid(Point3dEx pt, SpraySystem spraySystem)
        {
            if (!spraySystem.BranchPtDic.ContainsKey(pt))//支管图纸绘制起始点不存在, 跳过
            {
                return false;
            }
            if (!spraySystem.BranchDic.ContainsKey(pt))//支路列表没有这个点
            {
                return false;//跳过这个点
            }
            return true;
        }

        public static bool HasAutoValve(Point3dEx pt, List<Point3dEx> tpts, SpraySystem spraySystem, SprayIn sprayIn)
        {
            bool hasAutoValve = true;
            foreach (var tpt in tpts)
            {
                if (!sprayIn.TermPtDic.ContainsKey(tpt))
                {
                    continue;
                }
                var type = sprayIn.TermPtDic[tpt].Type;
                if (type == 4)//有立管无排气阀
                {
                    hasAutoValve = false;//没有自动排气阀
                    break;
                }
                if (!spraySystem.FireAreaStPtDic.ContainsKey(pt))//环管点无排气阀
                {
                    hasAutoValve = false;//没有自动排气阀
                    break;
                }
            }
            return hasAutoValve;
        }

        public static bool NeedNewDrawing(List<Point3dEx> tpts, SprayIn sprayIn)
        {
            bool hasFireArea = false;
            bool hasRiser = false;
            foreach (var tpt in tpts)
            {
                var type = sprayIn.TermPtDic[tpt].Type;
                if (type == 1)//防火分区
                {
                    hasFireArea = true;
                }
                if (type == 4 || type == 5)//立管
                {
                    hasRiser = true;
                }
            }
            var needNewDrawings = hasFireArea && hasRiser;//同时存在采用新的画法
            return needNewDrawings;
        }

        public static void DrawAutoValve(Point3d stPt, ref bool textRecord, SprayOut sprayOut)
        {
            sprayOut.SprayBlocks.Add(new SprayBlock("自动排气阀系统1", stPt));
            if (!textRecord)
            {
                textRecord = true;
                var stPt1 = stPt.OffsetY(731.7);
                var stPt2 = stPt1.OffsetY(2000);
                var stPt3 = stPt2.OffsetX(2140);
                sprayOut.NoteLine.Add(new Line(stPt1, stPt2));
                sprayOut.NoteLine.Add(new Line(stPt2, stPt3));
                var text = new Text("DN25排气阀余同", stPt2);
                sprayOut.Texts.Add(text);
            }
        }

        public static string GetDN(Point3dEx tpt, SprayIn sprayIn)
        {
            foreach (var vpt in sprayIn.TermDnDic.Keys)
            {
                if (tpt._pt.DistanceTo(vpt._pt) < 100)//端点管径标注包含当前点
                {
                    return sprayIn.TermDnDic[vpt];
                }
            }

            return "DNXXX";
        }

        public static Point3d GetEvadeStep(double length, Point3d pt3, string pipeNumber, SpraySystem spraySystem)
        {
            bool needEvade = true;//默认需要躲避
            double stepSize = 450;

            int indx = 0;
            while (needEvade)
            {
                var textPt = pt3.OffsetXY(-length, 900 + stepSize * indx);
                var text2 = new Text(pipeNumber, textPt);
                var texts = new List<DBText>() { text2.DbText };
                var line34 = new Line(pt3, pt3.OffsetY(900 + stepSize * indx));
                var lines = new List<Line>() { line34 };
                if (spraySystem.BlockExtents.SelectAll().Count == 0)//外包框数目为0
                {
                    spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                    spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                    break;
                }
                else
                {
                    var rect = GetRect(text2.DbText);//提框
                    var dbObjs = spraySystem.BlockExtents.SelectCrossingPolygon(rect);
                    if (dbObjs.Count == 0)
                    {
                        spraySystem.BlockExtents.Update(texts.ToCollection(), new DBObjectCollection());
                        spraySystem.BlockExtents.Update(lines.ToCollection(), new DBObjectCollection());
                        break;
                    }
                }
                indx++;
            }
            var pt4 = pt3.OffsetY(900 + stepSize * indx);
            return pt4;
        }

        public static Point3dCollection GetRect(DBText dBText)
        {
            double gap = 50;
            var maxPt = dBText.Position.OffsetXY(gap, gap);
            var minPt = dBText.Position.OffsetXY(-gap, -gap);
            if (!dBText.TextString.Equals(""))
            {
                maxPt = dBText.GeometricExtents.MaxPoint.OffsetXY(gap, gap);
                minPt = dBText.GeometricExtents.MinPoint.OffsetXY(-gap, -gap - 400);
            }

            var pts = new Point3d[5];
            pts[0] = new Point3d(minPt.X, maxPt.Y, 0);
            pts[1] = maxPt;
            pts[2] = new Point3d(maxPt.X, minPt.Y, 0);
            pts[3] = minPt;
            pts[4] = pts[0];

            var ptColl = new Point3dCollection(pts);
            return ptColl;
        }

        public static double GetLength(string text)
        {
            if (text.Equals(""))
            {
                return 0;
            }
            var dBText = new Text(text, new Point3d()).DbText;
            var maxPt = dBText.GeometricExtents.MaxPoint;
            var minPt = dBText.GeometricExtents.MinPoint;
            return Math.Abs(maxPt.X - minPt.X);
        }
    }
}
