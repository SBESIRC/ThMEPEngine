using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public abstract class ThSplitBeamSevice
    {
        protected DBObjectCollection Components { get; set; }
        public List<ThIfcBeam> SplitBeams { get; private set; }
        protected List<Tuple<Polyline, Point3d, Point3d>> IntersectAreas { get; set; }
        protected ThSplitBeamSevice(DBObjectCollection components)
        {
            Components = components;
            SplitBeams = new List<ThIfcBeam>();
            IntersectAreas = new List<Tuple<Polyline, Point3d, Point3d>>();
        }
        public abstract void Split();
        /// <summary>
        /// 打断直梁中心线
        /// </summary>
        /// <param name="breakPoints">在线内，相互无交集</param>
        /// <returns></returns>
        protected List<Tuple<Point3d, Point3d>> BreakBeamCenterLine(
            Point3d startPt, Point3d endPt, List<Tuple<Point3d, Point3d>> breakPoints)
        {
            List<Tuple<Point3d, Point3d>> pts = new List<Tuple<Point3d, Point3d>>();
            Point3d lineSp = startPt;
            for (int i = 0; i < breakPoints.Count; i++)
            {
                pts.Add(Tuple.Create(lineSp, breakPoints[i].Item1));
                lineSp = breakPoints[i].Item2;
            }
            pts.Add(Tuple.Create(lineSp, endPt));
            return pts;
        }
        protected Point3dCollection IntersectWithEx(Entity firstEntity, Entity secondEntity)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane zeroPlane = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, zeroPlane, pts, IntPtr.Zero, IntPtr.Zero);
            zeroPlane.Dispose();
            return pts;
        }
        protected bool ValidateIntersectPts(Point3dCollection intersPts, ThIfcLineBeam thIfcLineBeam, double tolerance = 1.0)
        {
            bool valid = true;
            foreach (Point3d pt in intersPts)
            {
                Point3d projectPt = pt.GetProjectPtOnLine(thIfcLineBeam.StartPoint, thIfcLineBeam.EndPoint);
                if (projectPt.DistanceTo(thIfcLineBeam.StartPoint) <= tolerance ||
                    projectPt.DistanceTo(thIfcLineBeam.EndPoint) <= tolerance)
                {
                    valid = false;
                    break;
                }
            }
            return valid;
        }
        protected bool CheckTwoLineUnIntersect(Point3d firstSpt, Point3d firstEpt, Point3d secondSpt, Point3d secondEpt)
        {
            double firstLength = firstSpt.DistanceTo(firstEpt);
            double secondLength = secondSpt.DistanceTo(secondEpt);
            return firstSpt.DistanceTo(secondEpt) > (firstLength + secondLength);
        }
        protected ThIfcLineBeam CreateLineBeam(ThIfcLineBeam thIfcLineBeam, Point3d startPt, Point3d endPt)
        {
            Vector3d direction = startPt.GetVectorTo(endPt);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d pt1 = startPt - perpendDir.GetNormal().MultiplyBy(thIfcLineBeam.ActualWidth / 2.0);
            Point3d pt2 = startPt + perpendDir.GetNormal().MultiplyBy(thIfcLineBeam.ActualWidth / 2.0);
            Point3d pt3 = pt2 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3d pt4 = pt1 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            ThIfcLineBeam newLineBeam = new ThIfcLineBeam()
            {
                Uuid = Guid.NewGuid().ToString(),
                StartPoint = startPt,
                EndPoint = endPt,
                Direction = direction,
                Width = thIfcLineBeam.Width,
                Height = thIfcLineBeam.Height,
                ComponentType = thIfcLineBeam.ComponentType,
                Outline = pts.CreatePolyline()
            };
            return newLineBeam;
        }
        protected Point3dCollection OrderbyPts(Point3dCollection pts)
        {
            List<Point3d> ptList = new List<Point3d>();
            foreach(Point3d pt in pts)
            {
                ptList.Add(pt);
            }
            Point3d basePt = ptList.OrderBy(o => o.Y).ThenBy(o => o.X).FirstOrDefault();
            ptList.Remove(basePt);
            Point3dCollection sortPts = new Point3dCollection();
            ptList.OrderBy(o => Math.Cos(basePt.GetVectorTo(o).GetAngleTo(
                Vector3d.XAxis, Vector3d.ZAxis))).ToList().ForEach(o=> sortPts.Add(o));
            sortPts.Insert(0, basePt);
            return sortPts;
        }
    }
}
