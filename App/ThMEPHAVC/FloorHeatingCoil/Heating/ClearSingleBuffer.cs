using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using System.Diagnostics;
using NetTopologySuite.Operation.Buffer;
using ThMEPEngineCore.Diagnostics;
using GeometryExtensions;
using ThMEPHVAC.FloorHeatingCoil;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class ClearSingleBuffer
    {
        public Polyline OriginalPoly = new Polyline();
        public double ClearDis = 0;
        public Polyline Boundary = new Polyline();
        public Polyline ClearedPl = new Polyline();
        public ClearSingleBuffer(Polyline pl, Polyline boundary, double clearDis)
        {
            OriginalPoly = pl;
            ClearDis = clearDis;
            Boundary = boundary;
        }

        public void Pipeline() 
        {
            ClearedPl = ClearBendsLongFirstClosed(OriginalPoly, Boundary, ClearDis);
        }

        public Polyline ClearBendsLongFirstClosed(Polyline originalPl, Polyline boundary, double dis)
        {
            Polyline newPl = originalPl.Clone() as Polyline;
            var coords = PassageWayUtils.GetPolyPoints(newPl);
            //coords.RemoveAt(coords.Count - 1);

            List<Point3d> newPointList = new List<Point3d>(); 
            double bufferDis = 5;

            Polyline newBoundary = boundary.Buffer(bufferDis).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = coords.Count;
            for (int i = 0; i < coords.Count;) 
            {
                Point3d pt0 = coords[i];
                Point3d pt1 = coords[(i + 1) % num];
                Point3d pt2 = coords[(i + 2) % num];
                Point3d pt3 = coords[(i + 3) % num];

                if ((pt2 - pt1).Length < dis)
                {
                    Point3d newPt1 = FindDiagonalPoint(pt0, pt1, pt2);
                    Point3d newPt2 = FindDiagonalPoint(pt1, pt2, pt3);

                    bool ok1 = newBoundary.Contains(new Line(newPt1, pt2)) && newBoundary.Contains(new Line(newPt1, pt0));
                    bool ok2 = newBoundary.Contains(new Line(newPt2, pt1)) && newBoundary.Contains(new Line(newPt2, pt3));

                    //if (i + 3 == num - 1) ok2 = false;
                    //if (i == 0) ok1 = false;

                    //if (i + 3 == newPl.NumberOfVertices) ok2 = false;
                    //if (i == 0) ok1 = false;

                    Vector3d vec0 = pt1 - pt0;
                    Vector3d vec2 = pt3 - pt2;
                    if ((ok1 && ok2 && vec0.Length > vec2.Length) || (ok2 && !ok1))
                    {
                        newPointList.Add(pt0);
                        //newPointList.Add(newPt2);
                        newPointList.Add(newPt2);
                        coords[(i + 1) % num] = newPt2;
                        coords[(i + 2) % num] = newPt2;
                        coords[(i + 3) % num] = newPt2;

                        if (i >= coords.Count - 2)
                        {
                            List<Point3d> deleteList = new List<Point3d>();
                            deleteList.Add(pt1);
                            deleteList.Add(pt2);
                            deleteList.Add(pt3);

                            for (int a = 0; a < 3; a++)
                            {
                                if (deleteList.Contains(newPointList.First()))
                                {
                                    deleteList.Remove(newPointList.First());
                                    newPointList.RemoveAt(0);
                                }
                            }
                            
                        }
                        

                        

                        if (vec2.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            //Polyline excessPl = new Polyline();
                            //excessPl.AddVertexAt(0, newPt2.ToPoint2D(), 0, 0, 0);
                            //excessPl.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                            //ExcessPoly.Add(excessPl);
                            newPointList.Add(newPt2);
                            newPointList.Add(pt1);

                            if (i == num - 1) newPointList.Add(newPt2);
                            //newPointList.Add(newPt2);
                        }
                        i = i + 3;
                        continue;
                    }
                    else if ((ok1 && ok2 && vec0.Length < vec2.Length) || (!ok2 && ok1))
                    {
                        newPointList.Add(pt0);
                        newPointList.Add(newPt1);
                        coords[(i + 1) % num] = newPt2;
                        coords[(i + 2) % num] = newPt2;
                        

                        if (i >= coords.Count - 2)
                        {
                            List<Point3d> deleteList = new List<Point3d>();
                            deleteList.Add(pt1);
                            deleteList.Add(pt2);
                            for (int a = 0; a < 2; a++)
                            {
                                if (deleteList.Contains(newPointList.First()))
                                {
                                    deleteList.Remove(newPointList.First());
                                    newPointList.RemoveAt(0);
                                }
                            }
                        }


                        

                        if (vec0.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            //Polyline excessPl = new Polyline();
                            //excessPl.AddVertexAt(0, newPt1.ToPoint2D(), 0, 0, 0);
                            //excessPl.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                            //ExcessPoly.Add(excessPl);
                            newPointList.Add(newPt1);
                            newPointList.Add(pt2);
                            if (i == num - 1) newPointList.Add(newPt1);
                        }

                        i = i + 3;
                        continue;
                    }
                }
                
                //如果没有continue;
                newPointList.Add(pt0);
                i++;
                
            }

            if (newPointList.Last() != newPointList.First()) 
            {
                newPointList.Add(newPointList.First());
            }
            newPl = PassageWayUtils.BuildPolyline(newPointList);
            return newPl;
        }

        public Point3d FindDiagonalPoint(Point3d pt0, Point3d pt1, Point3d pt2)
        {
            Vector3d dir = pt1 - pt0;
            Point3d newPt = pt2 - dir;
            return newPt;
        }
    }
}
