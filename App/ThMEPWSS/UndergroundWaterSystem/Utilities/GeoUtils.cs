using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.UndergroundWaterSystem.Utilities
{
    public static class GeoUtils
    {
        /// <summary>
        /// 连接认为是一条直线的存在间距的两条直线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="hinderpts"></param>
        /// <returns></returns>
        public static List<Line> ConnectBrokenLine(List<Line> lines, List<Point3d> hinderpts)
        {
            List<Line> connectedLines = new List<Line>();
            List<Line> emilinatedSelfLines = new List<Line>();
            lines.ForEach(o => emilinatedSelfLines.Add(o));
            double tolHinderpts = 300;
            double tolOriHinder = 300;
            double tolBrokenLine = 2000;
            double toldegree = 3;
            List<Polyline> plylist = new List<Polyline>();
            hinderpts.ForEach(o => plylist.Add(o.CreateRectangle(tolHinderpts, tolHinderpts)));
            DBObjectCollection dbObjsOriStart = new DBObjectCollection();
            plylist.ForEach(o => dbObjsOriStart.Add(o));
            for (int i = 0; i < lines.Count; i++)
            {
                emilinatedSelfLines.RemoveAt(i);
                Point3d ptStart = lines[i].StartPoint;
                Point3d ptEnd = lines[i].EndPoint;
                Vector3d SelfLine = new Vector3d(ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y, 0);
                if (GetCrossObjsByPtCollection(ptStart.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptStart.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp1.X, ptStart.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptStart.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp2.X, ptStart.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                if (GetCrossObjsByPtCollection(ptEnd.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptEnd.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp1.X, ptEnd.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptEnd.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp2.X, ptEnd.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                emilinatedSelfLines.Insert(i, lines[i]);
            }
            return connectedLines;
        }
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static DBObjectCollection GetCrossObjsByPtCollection(Point3dCollection ptcoll, DBObjectCollection dbObjs)
        {
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            var crossObjs = spatialIndex.SelectCrossingPolygon(ptcoll);
            return crossObjs;
        }
    }
}
