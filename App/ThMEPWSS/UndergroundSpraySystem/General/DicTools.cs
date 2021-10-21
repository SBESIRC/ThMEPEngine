﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class DicTools
    {
        public static void AddPtDicItem(SprayIn sprayIn, Point3dEx pt1, Point3dEx pt2)
        {
            if(sprayIn.PtDic.ContainsKey(pt1))//包含pt1
            {
                if (!sprayIn.PtDic[pt1].Contains(pt2))
                {
                    sprayIn.PtDic[pt1].Add(pt2);
                }
            }
            else//不包含pt1
            {
                sprayIn.PtDic.Add(pt1, new List<Point3dEx> { pt2 });
            }
            if (sprayIn.PtDic.ContainsKey(pt2))//包含pt2
            {
                if (!sprayIn.PtDic[pt2].Contains(pt1))
                {
                    sprayIn.PtDic[pt2].Add(pt1);
                }
            }
            else//不包含pt2
            {
                sprayIn.PtDic.Add(pt2, new List<Point3dEx> { pt1 });
            }
        }

        public static void AddItem(this List<Point3dEx> ptls, Point3dEx pt)
        {
            if(ptls?.Contains(pt) == false)
            {
                ptls.Add(pt);
            }
        }

        public static void AddItem(this Dictionary<Line, List<Line>> leadLineDic, Line l1, Line l2)
        {
            if (!leadLineDic.ContainsKey(l1))
            {
                var ptls = new List<Line>
                {
                    l2
                };
                leadLineDic.Add(l1, ptls);
            }
            else
            {
                if (!leadLineDic[l1].Contains(l2))
                {
                    leadLineDic[l1].Add(l2);
                }
            }
        }

        public static void AddType(this Dictionary<Point3dEx, string> ptTypeDic, Point3dEx pt1, string ptType)
        {
            if (!ptTypeDic.ContainsKey(pt1))
            {

                ptTypeDic.Add(pt1, ptType);
            }
            else
            {
                ptTypeDic[pt1] = ptType;
            }
        }

        public static void CreatePtDic(this List<Line> PipeLines, SprayIn sprayIn)
        {
            sprayIn.PtDic.Clear();
            foreach (var line in PipeLines)
            {
                var pt1 = new Point3dEx(line.StartPoint);
                var pt2 = new Point3dEx(line.EndPoint);
                if(pt1._pt.DistanceTo(pt2._pt) <= 1)
                {
                    continue;
                }
                AddPtDicItem(sprayIn, pt1, pt2);
            }
        }

        public static void CreatePtDic(SprayIn sprayIn)
        {
            double maxDist = 1000;
            double minDist = 100;
            var ptOffsetDic = new Dictionary<Point3dEx, Point3d>();
            foreach(var pt in sprayIn.PtDic.Keys)
            {
                if(sprayIn.PtDic[pt].Count == 1)
                {
                    var point = pt._pt;
                    var f1 = pt._pt.GetFloor(sprayIn.FloorRectDic);
                    if(f1.Equals(""))
                    {
                        continue;
                    }
                    var jizhunPt = sprayIn.FloorPtDic[f1];
                    var offsetPt = new Point3d(point.X - jizhunPt.X, point.Y - jizhunPt.Y, 0);
                    ptOffsetDic.Add(pt, offsetPt);
                }
            }
            foreach(var pt1 in ptOffsetDic.Keys)
            {
                foreach(var pt2 in ptOffsetDic.Keys)
                {
                    if(pt1._pt.DistanceTo(pt2._pt) > maxDist && ptOffsetDic[pt1].DistanceTo(ptOffsetDic[pt2]) < minDist)
                    {
                        AddPtDicItem(sprayIn, pt1, pt2);

                        sprayIn.ThroughPt.AddItem(pt1);
                        sprayIn.ThroughPt.AddItem(pt2);
                    }
                }
            }
            
        }

        public static void CreatePtTypeDic(List<Point3dEx> pts, string ptType, SprayIn sprayIn)
        {
            foreach(var pt in pts)
            {
                sprayIn.PtTypeDic.AddType(pt, ptType);
            }
        }

        public static void CreatePtTypeDic1(List<Point3dEx> pts, string ptType, ref SprayIn sprayIn)
        {
            foreach (var pt in sprayIn.PtDic.Keys)
            {
                foreach(var pt1 in pts)
                {
                    if(pt._pt.DistanceTo(pt1._pt) < 20)
                    {
                        sprayIn.PtTypeDic.AddType(pt, ptType);
                    }
                }
            }
        }


        public static void SetPointType(SprayIn sprayIn, List<List<Point3dEx>> rstPaths, List<Point3dEx> extraNodes)
        {
            foreach (var ptls in rstPaths)
            {
                for (int i = 0; i < ptls.Count; i++)
                {
                    var pt = ptls[i];
                    if(sprayIn.PtDic[pt].Count == 1)
                    {
                        continue;
                    }
                    bool typeFlag = false;
                    if (sprayIn.PtDic[pt].Count == 3)//3个邻接点： 次环点SubLoop  或  支路点 Branch 或 AlarmValve
                    {
                        if(sprayIn.PtTypeDic[pt].Contains("AlarmValve"))
                        {
                            continue;
                        }
                        foreach(var p in sprayIn.PtDic[pt])
                        {
                            if(ptls.Contains(p))
                            {
                                continue;
                            }
                            if(sprayIn.PtTypeDic[p].Contains("PressureValve"))
                            {
                                sprayIn.PtTypeDic.AddType(pt, "SubLoop");
                                typeFlag = true;
                                break;
                            }
                            ;
                            foreach(var p2 in sprayIn.PtDic[p])
                            {
                                if(ptls.Contains(p2))
                                {
                                    continue;
                                }
                                if (sprayIn.PtTypeDic[p2].Contains("PressureValve"))
                                {
                                    sprayIn.PtTypeDic.AddType(pt, "SubLoop");
                                    typeFlag = true;
                                    break;
                                }
                            }
                            
                        }
                        if(typeFlag)
                        {
                            continue;
                        }
                        foreach (var p in sprayIn.PtDic[pt])
                        {
                            if (extraNodes.Contains(p))
                            {
                                sprayIn.PtTypeDic.AddType(pt, "PangTong");
                                typeFlag = true;
                                break;
                            }
                        }
                        if (typeFlag)
                        {
                            continue;
                        }
                        sprayIn.PtTypeDic.AddType(pt, "Branch");
                    }

                    if (sprayIn.PtDic[pt].Count == 2)//2个邻接点： 主环点MainLoop  或  阀门 Valve
                    {
                        if (!sprayIn.PtTypeDic.ContainsKey(pt))//没有初始化的必定是 主环点MainLoop
                        {
                            sprayIn.PtTypeDic.Add(pt, "MainLoop");
                        }
                    }
                }
            }
        }
    }
}
