﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Diagnostics;
using ThCADExtension;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimConflictService
    {

        public static bool IsConflicted(Line line, ThCADCoreNTSSpatialIndex wallLinesSI, double tolerance = 200.0)
        {
            //穿图的墙线crossGraphLines
            List<Line> crossGraphLines = new List<Line>();
            DBObjectCollection dbSelect = wallLinesSI.SelectFence(line);
            foreach (DBObject dbo in dbSelect)
            {
                crossGraphLines.Add((Line)dbo);
            }

            if (crossGraphLines.Count > 0)
            {
                //把crossGraphLines转换为同方向
                Vector3d dir = (crossGraphLines[0].StartPoint - crossGraphLines[0].EndPoint).GetNormal();
                for (int i = 1; i < crossGraphLines.Count; i++)
                {
                    Vector3d tDir = (crossGraphLines[i].StartPoint - crossGraphLines[i].EndPoint).GetNormal();
                    if (dir.DotProduct(tDir) < 0)
                    {
                        crossGraphLines[i] = new Line(crossGraphLines[i].EndPoint, crossGraphLines[i].StartPoint);
                    }

                }

                //图线两边的距离最大值，两边最大值中取最小值
                double distance1 = 0;
                double distance2 = 0;
                foreach (Line l in crossGraphLines)
                {
                    Point3d pt1 = line.GetClosestPointTo(l.StartPoint, true);
                    Point3d pt2 = line.GetClosestPointTo(l.EndPoint, true);

                    double td1 = pt1.DistanceTo(l.StartPoint);
                    double td2 = pt2.DistanceTo(l.EndPoint);

                    if (distance1 < td1)
                        distance1 = td1;
                    if (distance2 < td2)
                        distance2 = td2;
                }

                if (Math.Min(distance1, distance2) > tolerance)
                    return true;
            }

            return false;
        }


        public static bool IsDimCrossReference(Line dim, ThCADCoreNTSSpatialIndex referenceSI, double tolerance=10)
        {
            if(dim.Length > tolerance)
            {
                //穿标注的参考线crossDimLines 过滤平行线
                List<Line> crossDimLines = new List<Line>();
                DBObjectCollection dbSelect = referenceSI.SelectFence(dim);
                foreach (DBObject dbo in dbSelect)
                {
                    Line line = (Line)dbo;
                    if (ThCoordinateService.IsParalleled(dim, line) || line.Length < tolerance)
                        continue;

                    crossDimLines.Add(line);
                }

                if (crossDimLines.Count > 0)
                {
                    foreach (Line l in crossDimLines)
                    {
                        Point3d p = dim.Intersection(l);

                        if (p.DistanceTo(dim.StartPoint) < tolerance || p.DistanceTo(dim.EndPoint) < tolerance)
                            continue;
                        else
                            return true;

                    }

                }
            }

            return false;
        }




        public static bool IsConflicted(Polyline dim, List<Entity> objects)
        {


            return false;
        }


    }
}