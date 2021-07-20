using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PtDic
    {
        public static void CreatePtDic(ref FireHydrantSystemIn fireHydrantSysIn, List<Line> lineList)
        {
            //管线添加
            fireHydrantSysIn.ptDic = new Dictionary<Point3dEx, List<Point3dEx>>();//清空  当前点和邻接点字典对
            foreach (var L in lineList)
            {
                var pt1 = new Point3dEx(L.StartPoint);
                var pt2 = new Point3dEx(L.EndPoint);

                ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2);
            }

            var lonelyPtList = new List<Point3dEx>();//找出孤立线段
            foreach (var ptKey in fireHydrantSysIn.ptDic.Keys)
            {
                if (fireHydrantSysIn.ptDic[ptKey].Count == 1)
                {
                    if (fireHydrantSysIn.ptDic[fireHydrantSysIn.ptDic[ptKey][0]].Equals(ptKey))
                    {
                        lonelyPtList.Add(ptKey);
                    }
                }
            }

            foreach (var pt in lonelyPtList)
            {
                fireHydrantSysIn.ptDic.Remove(pt);
            }

        }

        public static void CreateDNDic(ref FireHydrantSystemIn fireHydrantSysIn, DBObjectCollection PipeDN, List<Line> lineList)
        {
            foreach (var dn in PipeDN)//创建DN字典对
            {
                var dbtext = dn as DBText;
                var cenPt = General.GetMidPt(dbtext.GeometricExtents.MaxPoint, dbtext.GeometricExtents.MinPoint);
                var checkPt = new Point3d(dbtext.Position.X, cenPt.Y, 0);
                if (Math.Abs(dbtext.Rotation - Math.PI / 2) < 0.035)
                {
                    checkPt = new Point3d(cenPt.X, dbtext.Position.Y, 0);
                }

                foreach (var line in lineList)
                {
                    var ang = line.Angle;
                    if (PointAngle.IsParallelLine(ang, dbtext.Rotation) &&
                       line.GetClosestPointTo(dbtext.Position, false).DistanceTo(checkPt) < 400)
                    {
                        if (!fireHydrantSysIn.ptDNDic.ContainsKey(new LineSegEx(line)))
                        {
                            fireHydrantSysIn.ptDNDic.Add(new LineSegEx(line), dbtext.TextString);//贴边标注
                        }
                    }
                }
            }
        }

        public static void CreateTermPtDic(ref FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> pointList, 
            List<Line> labelLine, ThCADCoreNTSSpatialIndex textSpatialIndex, Dictionary<Point3dEx, string> ptTextDic,
            ThCADCoreNTSSpatialIndex fhSpatialIndex)
        {
            foreach (var pt in fireHydrantSysIn.hydrantPosition)//每个圈圈的中心点
            {
                var tpt = new Point3dEx(new Point3d());
                foreach (var p in pointList)
                {
                    var dis = Math.Abs(p._pt.X - pt._pt.X) + Math.Abs(p._pt.Y - pt._pt.Y);
                    if (p._pt.DistanceTo(pt._pt) < 80)
                    {
                        tpt = p;
                    }
                }
                if (tpt._pt.Equals(new Point3d()))
                {
                    continue;
                }

                var termPoint = new TermPoint(pt);
                termPoint.SetLines(labelLine);
                if (termPoint.StartLine is null)
                {
                    continue;
                }
                if (termPoint.TextLine is null)
                {
                    continue;
                }
                termPoint.SetPipeNumber(textSpatialIndex);
                if (termPoint.PipeNumber is null)
                {
                    if (ptTextDic.ContainsKey(termPoint.PtEx))
                    {
                        termPoint.PipeNumber = ptTextDic[termPoint.PtEx];
                    }
                    else
                    {
                        continue;
                    }


                }
                if (termPoint.PipeNumber.Equals("B1-3-XL-1"))
                {
                    ;
                }
                if (termPoint.PipeNumber.Equals("DXL-A"))
                {
                    ;
                }
                termPoint.SetType(fhSpatialIndex);
                if (fireHydrantSysIn.termPointDic.ContainsKey(tpt))
                {
                    continue;
                }
                else
                {
                    fireHydrantSysIn.termPointDic.Add(tpt, termPoint);
                }

            }

            var lpt = new Point3dEx(new Point3d());
            foreach (var pt in pointList)
            {
                if (!fireHydrantSysIn.ptDic.ContainsKey(pt))
                {
                    lpt = pt;
                    break;
                }

                if (fireHydrantSysIn.ptDic[pt].Count == 1)
                {
                    if (fireHydrantSysIn.ptDic[pt].Equals(lpt))
                    {
                        ;
                    }
                    if (!fireHydrantSysIn.termPointDic.ContainsKey(pt))
                    {
                        var termPoint = new TermPoint(pt);
                        termPoint.Type = 2;
                        termPoint.PipeNumber = " ";
                        fireHydrantSysIn.termPointDic.Add(pt, termPoint);
                    }
                }
            }
        }
        public static Dictionary<Line, List<Point3d>> CreateLabelPtDic(List<Point3dEx> hydrantPosition, List<Line> labelLine)
        {
            var labelPtDic = new Dictionary<Line, List<Point3d>>();//把在同一条标记线上的点聚集
            foreach (var pt in hydrantPosition)//遍历点
            {
                foreach (var l in labelLine)//遍历线
                {
                    if (PtOnLine.PtIsOnLine(pt._pt, l))//点在线上
                    {
                        if (labelPtDic.ContainsKey(l))//线存在字典
                        {
                            labelPtDic[l].Add(pt._pt);//直接添加
                        }
                        else
                        {
                            var ptls = new List<Point3d>();//新建后添加
                            ptls.Add(pt._pt);
                            labelPtDic.Add(l, ptls);
                        }
                    }
                }
            }
            foreach (var l in labelPtDic.Keys.ToArray())
            {
                if (labelPtDic[l].Count <= 1)
                {
                    labelPtDic.Remove(l);//删除掉单点
                }
                else//进行排序
                {
                    var ptList = labelPtDic[l];
                    Sort.PointsSort(ref ptList);
                    labelPtDic.Remove(l);
                    labelPtDic.Add(l, ptList);
                }
            }
            return labelPtDic;
        }

        public static Dictionary<Line, List<Line>> CreateLabelLineDic(Dictionary<Line, List<Point3d>> labelPtDic, List<Line> labelLine)
        {
            var labelLineDic = new Dictionary<Line, List<Line>>();
            foreach (var lk in labelPtDic.Keys)
            {
                var listLine = new List<Line>();
                foreach(var l in labelLine)
                {
                    if(labelPtDic.ContainsKey(l))
                    {
                        continue;
                    }
                    if(PtOnLine.PtIsOnLine(l.StartPoint, lk) || PtOnLine.PtIsOnLine(l.EndPoint, lk))
                    {
                        listLine.Add(l);
                    }

                }
                Sort.LinesSort(ref listLine);
                labelLineDic.Add(lk, listLine);
            }
            return labelLineDic;
        }
    
        public static Dictionary<Point3dEx, string> CreatePtTextDic(Dictionary<Line, List<Point3d>> labelPtDic, 
            Dictionary<Line, List<Line>> labelLineDic, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var ptTextDic = new Dictionary<Point3dEx, string>();
            foreach(var lk in labelPtDic.Keys)
            {
                for(int i = 0; i < Math.Min(labelPtDic[lk].Count, labelLineDic[lk].Count); i++)
                {
                    var line = labelLineDic[lk][i];
                    var text = GetText(spatialIndex, line);
                    if(text.Count() <= 1)
                    {
                        ;
                    }
                    ptTextDic.Add(new Point3dEx(labelPtDic[lk][i]), text);
                }
            }
            return ptTextDic;
        }

        public static string GetText(ThCADCoreNTSSpatialIndex spatialIndex, Line TextLine)
        {
            string text = "";
            var leftX = 0.0;
            var rightX = 0.0;
            var leftY = 0.0;
            var rightY = 0.0;
            var textHeight = 500;
            if (TextLine.StartPoint.X < TextLine.EndPoint.X)
            {
                leftX = TextLine.StartPoint.X;
                rightX = TextLine.EndPoint.X;
                leftY = TextLine.StartPoint.Y;
                rightY = TextLine.EndPoint.Y;

            }
            else
            {
                leftX = TextLine.EndPoint.X;
                rightX = TextLine.StartPoint.X;
                leftY = TextLine.EndPoint.Y;
                rightY = TextLine.StartPoint.Y;
            }

            var pt1 = new Point3d(leftX, leftY + textHeight, 0);
            var pt2 = new Point3d(rightX, rightY, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//文字范围

            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            foreach (var obj in DBObjs)
            {
                var br = obj as DBText;
                text = br.TextString;
                /*
                if (obj is DBText)
                {
                    var br = obj as DBText;
                    text = br.TextString;
                }
                else
                {

                    var ad = (obj as Entity).AcadObject;
                    dynamic o = ad;
                    if ((o.ObjectName as string).Equals("TDbText"))
                    {
                        text = o.Text;
                    }

                }
                */

            }

            return text;
        }
    
        public static void CreateBranchDic(ref Dictionary<Point3dEx, List<List<Point3dEx>>> branchDic, List<List<Point3dEx>> mainPathList, 
            FireHydrantSystemIn fireHydrantSysIn, HashSet<Point3dEx> visited, List<Point3dEx> extraNodes)
        {
            foreach (var rstPath in mainPathList)
            {
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, rstPath, fireHydrantSysIn, extraNodes);//支路遍历

                        if (branchPath.Count != 0)
                        {
                            branchDic.Add(pt, branchPath);
                        }
                    }
                }
            }
        }


    }
}
