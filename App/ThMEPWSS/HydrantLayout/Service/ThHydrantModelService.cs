using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.HydrantLayout.Model;

namespace ThMEPWSS.HydrantLayout.Service
{
    public static class ThHydrantModelService
    {
        public static ThHydrantModel CreateHydrantMode(ThIfcDistributionFlowElement hydrant)
        {
            var blk = hydrant.Outline as BlockReference;

            var name = blk.GetEffectiveName();
            var type = -1;
            if (name.Contains(ThHydrantCommon.BlkName_Hydrant))
            {
                type = 0;
            }
            else if (name.Contains(ThHydrantCommon.BlkName_Hydrant_Extinguisher))
            {
                type = 1;
            }


            var dir = Vector3d.YAxis.RotateBy(blk.Rotation, Vector3d.ZAxis);
            var outline = new Polyline();
            var center = new Point3d();
            var openDir = -1;
            if (type == 0)
            {
                outline = GetHydrantBoundary(blk, dir, out openDir);
            }
            else if (type == 1)
            {
                outline = GetExtinguisherBoundary(blk);
            }
            if (outline != null && outline.NumberOfVertices > 0)
            {
                center = outline.GetCenter();
            }

            var hydrantModel = new ThHydrantModel();
            if (outline != null && outline.NumberOfVertices > 0)
            {
                hydrantModel.Data = blk;
                hydrantModel.BlkDir = dir;
                hydrantModel.Outline = outline;
                hydrantModel.Center = center;
                hydrantModel.Type = type;
                hydrantModel.OpenDir = openDir;
            }

            return hydrantModel;
        }


        private static Polyline GetHydrantBoundary(BlockReference blk, Vector3d dir, out int openDir)
        {
            var boundary = new Polyline();
            openDir = -1;

            var explodeObjs = new DBObjectCollection();
            blk.Explode(explodeObjs);
            var visibleObjs = explodeObjs.OfType<Entity>().Where(x => x.Visible == true).ToList();

            var arc = visibleObjs.OfType<Arc>().FirstOrDefault();
            if (arc != null)
            {
                var connectLine = FindArcConnectPolyline(arc, visibleObjs, blk.Rotation, out var basePt);
                if (connectLine.Count == 2)
                {
                    connectLine = connectLine.OrderBy(x => x.Length).ToList();
                    var shortSide = connectLine.First();
                    var longSide = connectLine.Last();

                    boundary = CreateBoundary(basePt, shortSide, longSide, out openDir);
                }
            }

            return boundary;
        }

        private static Polyline GetExtinguisherBoundary(BlockReference blk)
        {
            var boundary = new Polyline();

            var explodeObjs = new DBObjectCollection();
            blk.Explode(explodeObjs);
            var visibleObjs = explodeObjs.OfType<Polyline>().Where(x => x.Visible == true).FirstOrDefault();

            if (visibleObjs!=null && visibleObjs.NumberOfVertices >0)
            {
                boundary = visibleObjs;
            }    
           

            return boundary;
        }


        private static Polyline CreateBoundary(Point3d basePt, Line shortSide, Line longSide, out int openDir)
        {
            var tol = new Tolerance(10, 10);
            var shortDir = (shortSide.EndPoint - shortSide.StartPoint).GetNormal();
            if (shortSide.StartPoint.IsEqualTo(basePt, tol))
            {
                shortDir = -shortDir;
            }
            var longDir = (longSide.EndPoint - longSide.StartPoint).GetNormal();
            if (longSide.EndPoint.IsEqualTo(basePt, tol))
            {
                longDir = -longDir;
            }

            openDir = -1;
            if (shortDir.GetAngleTo(longDir, Vector3d.ZAxis) <= Math.PI)
            {
                openDir = 0;
            }
            else
            {
                openDir = 1;
            }

            var pt1 = basePt + (-shortDir) * shortSide.Length;
            var pt2 = pt1 + longDir * longSide.Length;
            var pt3 = pt2 + shortDir * shortSide.Length;

            var boundary = new Polyline();
            boundary.Closed = true;

            boundary.AddVertexAt(0, basePt.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(2, pt2.ToPoint2D(), 0, 0, 0);
            boundary.AddVertexAt(3, pt3.ToPoint2D(), 0, 0, 0);

            return boundary;
        }
        private static List<Line> FindArcConnectPolyline(Arc arc, List<Entity> visibleObjs, double blkAngle, out Point3d basePt)
        {
            var connectLine = new List<Line>();
            var spt = arc.StartPoint;
            var ept = arc.EndPoint;

            connectLine = FindConnectLine(spt, visibleObjs, blkAngle);
            basePt = spt;
            if (connectLine.Count == 0)
            {
                connectLine = FindConnectLine(ept, visibleObjs, blkAngle);
                basePt = ept;
            }

            return connectLine;

        }
        private static List<Line> FindConnectLine(Point3d pt, List<Entity> visibleObjs, double blkAngle)
        {
            var connectLine = new List<Line>();
            var tolA = new Tolerance(10, 10);
            var tol = 1;
            foreach (var obj in visibleObjs)
            {
                if (obj is Line l)
                {
                    if (l.StartPoint.IsEqualTo(pt, tolA) || (l.EndPoint.IsEqualTo(pt, tolA)))
                    {
                        if (IsOrthogonalAngle(l.Angle, blkAngle, tol))
                        {
                            connectLine.Add(l);
                        }
                    }
                }
            }

            return connectLine;
        }

        /// <summary>
        /// 判断角A角B是否正交。角A角B弧度制
        /// tol:角度容差（角度制），数值大于0 小于90
        /// </summary>
        /// <param name="angleA"></param>
        /// <param name="angleB"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static bool IsOrthogonalAngle(double angleA, double angleB, double tol)
        {
            var bReturn = false;
            var angleDelta = angleA - angleB;
            var cosAngle = Math.Abs(Math.Cos(angleDelta));

            if (cosAngle > Math.Cos(tol * Math.PI / 180) || cosAngle < Math.Cos((90 - tol) * Math.PI / 180))
            {
                bReturn = true;
            }

            return bReturn;
        }
    }
}
