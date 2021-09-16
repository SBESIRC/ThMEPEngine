using AcHelper.Commands;
using System;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPElectrical.Broadcast.Service;
using System.Collections.Generic;
using System.Collections;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Runtime;
using NFox.Collections;//树
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Overlay;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.LinearReferencing;

namespace ThMEPElectrical.AlarmLayout.Utils
{
    public static class CenterLineSimplify
    {
        /*
        public static void CLSimplify(MPolygon mPolygon, AcadDatabase acdb, double interpolationDistance)
        {
            List<Point3d> centerPts = CenterPoints(mPolygon.ToNTSPolygon(), interpolationDistance);
            centerPts.Distinct();
            double dis = interpolationDistance * 2;
            Hashtable ht = new Hashtable();
            //ht(Point3d, int) 
            foreach (Point3d pt in centerPts)
            {
                ht.Add(pt, 0);
            }
            ht[centerPts[0]] = 1;
            List<Point3d> tmpList = new List<Point3d>();
            DFS(ht, centerPts[0], acdb, dis, centerPts, tmpList);
            

        }
        
        public static void DFS(Hashtable ht, Point3d curPoint, AcadDatabase acdb, double dis, List<Point3d> centerPts, List<Point3d> tmpList, int flag)
        {
            List<Point3d> nearPoints = PointsDealer.GetNearestPoints(curPoint, centerPts, dis);
            //bool flag0 = false;
            //bool flag1 = false;
            //bool flag2 = false;
            int cntFlag0 = 0;
            foreach(Point3d pt in nearPoints)
            {
                if ((int)ht[pt] == 0)
                {

                    ++cntFlag0;
                    if(cntFlag0 >= 2)
                    {
                        //闭合tmpList，并把里面填充为2
                        foreach(var ptt in tmpList)
                        {
                            ht[ptt] = 2;

                            Line line = new Line(curPoint, pt);
                            line.ColorIndex = 130;
                            HostApplicationServices.WorkingDatabase.AddToModelSpace(line);

                        }

                        //生成新的list
                        List<Point3d> newList = new List<Point3d>();
                        newList.Add(pt);
                        DFS(ht, pt, acdb, dis, centerPts, newList);
                    }
                    else
                    {
                        tmpList.Add(pt);
                        //flag0 = true;
                        DFS(ht, pt, acdb, dis, centerPts, tmpList);
                    }
                }
            }
            if(cntFlag0 == 0)//没必要， 肯定等于0
            {
                //闭合list，并把里面填充为1
                foreach (var pt in tmpList)
                {
                    ht[pt] = 1;
                }
                return;
            }
        }
        */

        public static void ShowCenterLine(MPolygon mPolygon, AcadDatabase acdb)
        {
            var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), 300);
            // 生成、显示中线
            centerlines.Cast<Entity>().ToList().CreateGroup(acdb.Database, 130);
        }

        public static List<Point3d> CenterPoints(Polygon geometry, double interpolationDistance)
        {
            List<Point3d> centerPoints = new List<Point3d>();
            foreach (Polygon polygon in geometry.VoronoiDiagram(interpolationDistance).Geometries)
            {
                var iterator = new LinearIterator(polygon.Shell);
                for (; iterator.HasNext(); iterator.Next())
                {
                    if (!iterator.IsEndOfLine)
                    {
                        var line = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(new Coordinate[] { iterator.SegmentStart, iterator.SegmentEnd });
                        if (line.Within(geometry))
                        {
                            Point3d curPoint = new Point3d(iterator.SegmentStart.X, iterator.SegmentStart.Y, 0);
                            centerPoints.Add(curPoint);
                        }
                    }
                }
            }
            return centerPoints;
        }

    }
}
