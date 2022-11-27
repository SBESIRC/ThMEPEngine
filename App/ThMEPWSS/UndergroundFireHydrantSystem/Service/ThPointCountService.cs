using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class Point3dEx : IEquatable<Point3dEx>
    {
        public double Tolerance = 1; //mm
        public Point3d _pt;

        public Point3dEx(double tol = 1)
        {
            _pt = new Point3d();
            Tolerance = tol;
        }
        public Point3dEx(Point3d pt, double tol = 1)
        {
            _pt = new Point3d(pt.X,pt.Y,0);
            Tolerance = tol;
        }

        public Point3dEx(Point2d pt, double tol = 1)
        {
            _pt = new Point3d(pt.X, pt.Y, 0);
            Tolerance = tol;
        }

        public Point3dEx(double x, double y, double z,double tol = 1)
        {
            _pt = new Point3d(x, y, z);
            Tolerance = tol;
        }

        public override int GetHashCode()
        {
            return ((int)_pt.X / 1000).GetHashCode() ^ ((int)_pt.Y / 1000).GetHashCode();
        }
        public bool Equals(Point3dEx other)
        {
            return Math.Abs(other._pt.X - this._pt.X) < Tolerance && Math.Abs(other._pt.Y - this._pt.Y) < Tolerance;
        }
    }

    public class LineSegEx : IEquatable<LineSegEx>
    {
        public Point3d _spt;
        public Point3d _ept;

        public LineSegEx()
        {
            _spt = new Point3d();
            _ept = new Point3d();
        }
        public LineSegEx(Line line)
        {
            _spt = line.StartPoint;
            _ept = line.EndPoint;
        }
        public LineSegEx(Point3d spt, Point3d ept)
        {
            _spt = spt;
            _ept = ept;
        }
        public override int GetHashCode()
        {
            return _spt.GetHashCode() ^ _ept.GetHashCode();
        }
        public bool Equals(LineSegEx other)
        {
            var tolerance = 5; //mm
            return (other._spt.DistanceTo(_spt) < tolerance && other._ept.DistanceTo(_ept) < tolerance) ||
                   (other._spt.DistanceTo(_ept) < tolerance && other._ept.DistanceTo(_spt) < tolerance);
        }

        public bool IsTermPt(Point3d pt)
        {
            var tolerance = 5; //mm
            return _spt.DistanceTo(pt) < tolerance || _ept.DistanceTo(pt) < tolerance;
        }
    }

    class ThPointCountService
    {
        public static void AddPoint(FireHydrantSystemIn fireHydrantSysIn, ref Point3dEx pt1, ref Point3dEx pt2, string type)
        {
            if (fireHydrantSysIn.PtDic.Count == 0)
            {
                fireHydrantSysIn.PtDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.PtDic.Add(pt2, new List<Point3dEx>() { pt1 });
                fireHydrantSysIn.PtTypeDic.Add(pt1, type);
                fireHydrantSysIn.PtTypeDic.Add(pt2, type);
                return;
            }

            if(fireHydrantSysIn.PtDic.ContainsKey(pt1))
            {
                if (!fireHydrantSysIn.PtDic[pt1].Contains(pt2))
                {
                    fireHydrantSysIn.PtDic[pt1].Add(pt2);
                }
                if(!fireHydrantSysIn.PtTypeDic[pt1].Equals(type))
                {
                    fireHydrantSysIn.PtTypeDic.Remove(pt1);
                    fireHydrantSysIn.PtTypeDic.Add(pt1, type);
                }
            }
            else
            {
                fireHydrantSysIn.PtDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.PtTypeDic.Add(pt1, type);
            }

            if (fireHydrantSysIn.PtDic.ContainsKey(pt2))
            {
                if (!fireHydrantSysIn.PtDic[pt2].Contains(pt1))
                {
                    fireHydrantSysIn.PtDic[pt2].Add(pt1);
                }
                if (!fireHydrantSysIn.PtTypeDic[pt2].Equals(type))
                {
                    fireHydrantSysIn.PtTypeDic.Remove(pt2);
                    fireHydrantSysIn.PtTypeDic.Add(pt2, type);
                }
            }
            else
            {
                fireHydrantSysIn.PtDic.Add(pt2, new List<Point3dEx>() { pt1 });
                fireHydrantSysIn.PtTypeDic.Add(pt2, type);
            }
        }

        public static void AddPoint(ref FireHydrantSystemIn fireHydrantSysIn, ref Point3dEx pt1, ref Point3dEx pt2)
        {
            if(pt1.Equals(pt2))
            {
                return;
            }
            if (fireHydrantSysIn.PtDic.Count == 0)
            {
                fireHydrantSysIn.PtDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.PtDic.Add(pt2, new List<Point3dEx>() { pt1 });
                return;
            }

            if (fireHydrantSysIn.PtDic.ContainsKey(pt1))
            {
                if (!fireHydrantSysIn.PtDic[pt1].Contains(pt2))
                {
                    fireHydrantSysIn.PtDic[pt1].Add(pt2);
                }
            }
            else
            {
                fireHydrantSysIn.PtDic.Add(pt1, new List<Point3dEx>() { pt2 });
            }

            if (fireHydrantSysIn.PtDic.ContainsKey(pt2))
            {
                if (!fireHydrantSysIn.PtDic[pt2].Contains(pt1))
                {
                    fireHydrantSysIn.PtDic[pt2].Add(pt1);
                }
            }
            else
            {
                fireHydrantSysIn.PtDic.Add(pt2, new List<Point3dEx>() { pt1 });
            }
        }

        public static void SetPointType(ref FireHydrantSystemIn fireHydrantSysIn, List<List<Point3dEx>> rstPaths)
        {
            foreach(var ptls in rstPaths)
            {
                foreach (var pt in ptls)
                {
                    if (fireHydrantSysIn.PtDic[pt].Count == 3)//3个邻接点： 次环点SubLoop  或  支路点 Branch
                    {
                        fireHydrantSysIn.PtTypeDic.Remove(pt);//支路点 Branch
                        fireHydrantSysIn.PtTypeDic.Add(pt, "Branch");
                        foreach (var nd in fireHydrantSysIn.NodeList)
                        {
                            if (nd.Contains(pt))//次环点SubLoop
                            {
                                fireHydrantSysIn.PtTypeDic.Remove(pt);
                                fireHydrantSysIn.PtTypeDic.Add(pt, "SubLoop");
                                break;
                            }
                        }
                    }

                    if (fireHydrantSysIn.PtDic[pt].Count == 2)//2个邻接点： 主环点MainLoop  或  阀门 Valve
                    {
                        if (!fireHydrantSysIn.PtTypeDic.ContainsKey(pt))//没有初始化的必定是 主环点MainLoop
                        {
                            fireHydrantSysIn.PtTypeDic.Add(pt, "MainLoop");
                        }
                    }
                }
            }
        }
    }
}
