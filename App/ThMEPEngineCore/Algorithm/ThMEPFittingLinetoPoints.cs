using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFittingLinetoPoints
    {
        public static void GenerateFittingPolylineFromPoints(
            Vector3d target_direction, List<Point3d> points, List<Polyline> wall_contours, List<Polyline> obstacles,
            double tol_degree, double dis_offset, ref Polyline result)
        {
            GetFittingPolyline(target_direction, points, tol_degree, ref result);
            if (result.Vertices().Count < 1) return;
            AvoidObstacles(target_direction, wall_contours, obstacles, dis_offset, ref result);
        }

        private static void AvoidObstacles(
            Vector3d target_direction, List<Polyline> wall_contours, List<Polyline> obstacles, double dis_offset, ref Polyline result)
        {
            Vector3d vec_offset = target_direction.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin));
            Vector3d vec_a = vec_offset.TransformBy(Matrix3d.Scaling(dis_offset / vec_offset.Length, Point3d.Origin));
            Vector3d vec_b = vec_offset.TransformBy(Matrix3d.Scaling(-dis_offset / vec_offset.Length, Point3d.Origin));
            Polyline result_a = new Polyline();
            Polyline result_b = new Polyline();
            for (int i = 0; i < result.Vertices().Count; i++)
            {
                result_a.AddVertexAt(i, result.Vertices()[i].ToPoint2d(), 0, 0, 0);
                result_b.AddVertexAt(i, result.Vertices()[i].ToPoint2d(), 0, 0, 0);
            }
            DBObjectCollection objs = new DBObjectCollection();
            wall_contours.ForEach(e => objs.Add(e));
            obstacles.ForEach(e => objs.Add(e));
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            int cycle_count = 0;
            while (true)
            {
                Point3dCollection ptcoll = ThDrawTool.ToRectangle(result_a.StartPoint, result_a.EndPoint, 1).Vertices();
                var cross_objs = spatialIndex.SelectCrossingPolygon(ptcoll);
                if (cross_objs.Count == 0)
                {
                    result = result_a;
                    break;
                }
                ptcoll = ThDrawTool.ToRectangle(result_b.StartPoint, result_b.EndPoint, 1).Vertices();
                cross_objs = spatialIndex.SelectCrossingPolygon(ptcoll);
                if (cross_objs.Count == 0)
                {
                    result = result_b;
                    break;
                }
                result_a.TransformBy(Matrix3d.Displacement(vec_a));
                result_b.TransformBy(Matrix3d.Displacement(vec_b));
                cycle_count++;
                if (cycle_count == 100)
                {
                    result = new Polyline();
                    break;
                }
            }
        }

        private static void GetFittingPolyline(Vector3d target_direction, List<Point3d> points, double tol_degree, ref Polyline result)
        {
            int n = points.Count;
            int num;
            int max = 0;
            List<int> insert_points_indexes;
            if (n < 2) return;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    insert_points_indexes = new List<int>();
                    num = 2;
                    Vector3d v_ij = new Vector3d(points[j].X - points[i].X, points[j].Y - points[i].Y, 0);
                    for (int k = 0; k < n; k++)
                    {
                        Vector3d v_prek;
                        if (insert_points_indexes.Count == 0)
                            v_prek = new Vector3d(points[k].X - points[i].X, points[k].Y - points[i].Y, 0);
                        else
                            v_prek = new Vector3d(points[k].X - points[insert_points_indexes[insert_points_indexes.Count - 1]].X,
                                points[k].Y - points[insert_points_indexes[insert_points_indexes.Count - 1]].Y, 0);
                        Vector3d v_kj = new Vector3d(points[j].X - points[k].X, points[j].Y - points[k].Y, 0);
                        if (TypeOfTwoVectors(v_ij, target_direction, tol_degree) > 0 &&
                            TypeOfTwoVectors(v_prek, v_kj, tol_degree) == 1)
                        {
                            insert_points_indexes.Add(k);
                            num++;
                        }
                    }
                    if (num >= max)
                    {
                        max = num;
                        result = new Polyline();
                        for (int k = 0; k < insert_points_indexes.Count; k++)
                        {
                            result.AddVertexAt(k, points[insert_points_indexes[k]].ToPoint2d(), 0, 0, 0);
                        }
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Judge the relationship of two vectors' direction.
        /// return: 0_not parallel, 1_same direction parallel, 2_inverse direction parallel
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int TypeOfTwoVectors(Vector3d a, Vector3d b, double tol_degree = 0)
        {
            if (a.Length == 0 || b.Length == 0) return 1;
            double angle = Math.Abs(Math.Atan2(b.Y, b.X) - Math.Atan2(a.Y, a.X));
            angle = angle / Math.PI * 180;
            if (angle < tol_degree) return 1;
            if (180 - angle < tol_degree) return 2;
            return 0;
        }
    }
}