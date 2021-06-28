using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class Point3dEx : IEquatable<Point3dEx>
    {
        public Point3d _pt;
        public Point3dEx(Point3d pt)
        {
            _pt = pt;
        }

        public Point3dEx(double x, double y, double z)
        {
            _pt = new Point3d(x, y, z);
        }

        public override int GetHashCode()
        {
            return ((int)_pt.X / 10).GetHashCode() ^ ((int)_pt.Y / 10).GetHashCode();
        }
        public bool Equals(Point3dEx other)
        {
            var tolerance = 1; //mm
            return Math.Abs(other._pt.X - this._pt.X) < tolerance && Math.Abs(other._pt.Y - this._pt.Y) < tolerance;
        }
    }

    class ThPointCountService
    {
        public static void AddPoint(ref FireHydrantSystemIn fireHydrantSysIn, ref Point3dEx pt1, ref Point3dEx pt2, string type)
        {
            if (fireHydrantSysIn.ptDic.Count == 0)
            {
                fireHydrantSysIn.ptDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.ptDic.Add(pt2, new List<Point3dEx>() { pt1 });
                fireHydrantSysIn.ptTypeDic.Add(pt1, type);
                fireHydrantSysIn.ptTypeDic.Add(pt2, type);
                return;
            }

            if(fireHydrantSysIn.ptDic.ContainsKey(pt1))
            {
                if (!fireHydrantSysIn.ptDic[pt1].Contains(pt2))
                {
                    fireHydrantSysIn.ptDic[pt1].Add(pt2);
                }
                if(!fireHydrantSysIn.ptTypeDic[pt1].Equals(type))
                {
                    fireHydrantSysIn.ptTypeDic.Remove(pt1);
                    fireHydrantSysIn.ptTypeDic.Add(pt1, type);
                }
            }
            else
            {
                fireHydrantSysIn.ptDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.ptTypeDic.Add(pt1, type);

            }

            if (fireHydrantSysIn.ptDic.ContainsKey(pt2))
            {
                if (!fireHydrantSysIn.ptDic[pt2].Contains(pt1))
                {
                    fireHydrantSysIn.ptDic[pt2].Add(pt1);
                }
                if (!fireHydrantSysIn.ptTypeDic[pt2].Equals(type))
                {
                    fireHydrantSysIn.ptTypeDic.Remove(pt2);
                    fireHydrantSysIn.ptTypeDic.Add(pt2, type);
                }
            }
            else
            {
                fireHydrantSysIn.ptDic.Add(pt2, new List<Point3dEx>() { pt1 });
                fireHydrantSysIn.ptTypeDic.Add(pt2, type);

            }
        }

        public static void AddPoint(ref FireHydrantSystemIn fireHydrantSysIn, ref Point3dEx pt1, ref Point3dEx pt2)
        {
            if (fireHydrantSysIn.ptDic.Count == 0)
            {
                fireHydrantSysIn.ptDic.Add(pt1, new List<Point3dEx>() { pt2 });
                fireHydrantSysIn.ptDic.Add(pt2, new List<Point3dEx>() { pt1 });
                return;
            }

            if (fireHydrantSysIn.ptDic.ContainsKey(pt1))
            {
                if (!fireHydrantSysIn.ptDic[pt1].Contains(pt2))
                {
                    fireHydrantSysIn.ptDic[pt1].Add(pt2);
                }
            
            }
            else
            {
                fireHydrantSysIn.ptDic.Add(pt1, new List<Point3dEx>() { pt2 });
            }

            if (fireHydrantSysIn.ptDic.ContainsKey(pt2))
            {
                if (!fireHydrantSysIn.ptDic[pt2].Contains(pt1))
                {
                    fireHydrantSysIn.ptDic[pt2].Add(pt1);
                }
            }
            else
            {
                fireHydrantSysIn.ptDic.Add(pt2, new List<Point3dEx>() { pt1 });
            }
        }


        public static void SetPointType(ref FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> rstPath)
        {
            foreach (var pt in rstPath)
            {
                if (fireHydrantSysIn.ptDic[pt].Count == 3)//3个邻接点： 次环点SubLoop  或  支路点 Branch
                {
                    foreach (var nd in fireHydrantSysIn.nodeList)
                    {
                        if (nd.Contains(pt))//次环点SubLoop
                        {
                            fireHydrantSysIn.ptTypeDic.Remove(pt);
                            fireHydrantSysIn.ptTypeDic.Add(pt, "SubLoop");
                            break;
                        }
                        fireHydrantSysIn.ptTypeDic.Remove(pt);//支路点 Branch
                        fireHydrantSysIn.ptTypeDic.Add(pt, "Branch");
                    }
                    
                }

                if (fireHydrantSysIn.ptDic[pt].Count == 2)//2个邻接点： 主环点MainLoop  或  阀门 Valve
                {
                    if (!fireHydrantSysIn.ptTypeDic.ContainsKey(pt))//没有初始化的必定是 主环点MainLoop
                    {
                        fireHydrantSysIn.ptTypeDic.Add(pt, "MainLoop");
                    }
                }
            }
        }
    }
}
