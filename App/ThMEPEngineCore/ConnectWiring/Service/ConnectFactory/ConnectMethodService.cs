using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.ConnectWiring.Service.ConnectFactory
{
    public class ConnectMethodService
    {
        double tol = 1;
        public Polyline CennectToPoint(Polyline wiring, BlockReference block, double range, List<Point3d> connectPts)
        {
            var blockPt = new Point3d(block.Position.X, block.Position.Y, 0);
            Circle circle = new Circle(blockPt, Vector3d.ZAxis, range);
            Point3dCollection pts = new Point3dCollection();
            wiring.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                var cutPoint = pts[0];
                var connectPt = connectPts.OrderBy(x => x.DistanceTo(cutPoint)).First();
                return UpdateWiring(wiring, connectPt, cutPoint, circle);
            }

            return wiring;
        }

        public Polyline ConnectByCircle(Polyline wiring, BlockReference block, double range)
        {
            Circle circle = new Circle(block.Position, Vector3d.ZAxis, range);
            Point3dCollection pts = new Point3dCollection();
            wiring.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            if (pts.Count > 0)
            {
                var cutPoint = pts[0];
                return UpdateWiring(wiring, cutPoint, circle);
            }

            return wiring;
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="connectPt"></param>
        /// <param name="turnPt"></param>
        /// <returns></returns>
        private Polyline UpdateWiring(Polyline wiring, Point3d connectPt, Point3d turnPt, Circle circle)
        {
            if (circle.EntityContains(wiring.StartPoint))
            {
                wiring.ReverseCurve();
            }
            Polyline poly = new Polyline();
            for (int i = 0; i < wiring.NumberOfVertices - 1; i++)
            {
                Point3d pt = wiring.GetPoint3dAt(i);
                Line line = new Line(pt, wiring.GetPoint3dAt((i + 1) % wiring.NumberOfVertices));
                poly.AddVertexAt(i, pt.ToPoint2D(), 0, 0, 0);
                if (line.GetClosestPointTo(turnPt, false).DistanceTo(turnPt) < tol)
                {
                    poly.AddVertexAt(i + 1, turnPt.ToPoint2D(), 0, 0, 0);
                    poly.AddVertexAt(i + 2, connectPt.ToPoint2D(), 0, 0, 0);
                    break;
                }
            }

            return poly;
        }

        public Polyline CennectToPoint(Polyline wiring, BlockReference block, List<Point3d> connectPts)
        {
            using (Linq2Acad.AcadDatabase acac = Linq2Acad.AcadDatabase.Active())
            {
                var blockPt = new Point3d(block.Position.X, block.Position.Y, 0);
                var blockObb = GetBlockReferenceOBB(acac.Database, block);
                var Intersectpts = new Point3dCollection();
                wiring.IntersectWith(blockObb, Intersect.OnBothOperands, Intersectpts, (IntPtr)0, (IntPtr)0);
                if (Intersectpts.Count == 0)
                {
                    return wiring;
                    throw new NotImplementedException();
                }
                Point3d intersectPt = Intersectpts.Cast<Point3d>().OrderByDescending(o => o.DistanceTo(wiring.StartPoint)).First();

                Point3d connectPt = connectPts.OrderBy(o => o.DistanceTo(intersectPt)).First();
                int pathedgs = wiring.NumberOfVertices;

                int i = 1;
                Line line = new Line();
                bool CanAdjustConnectionPt = true;
                while (i < pathedgs)
                {
                    var pt = wiring.GetPoint3dAt(i);
                    if (blockObb.Distance(pt) < 5)
                    {
                        if (CanAdjustConnectionPt)
                        {
                            CanAdjustConnectionPt = false;
                            connectPt = connectPts.OrderBy(o => o.DistanceTo(pt)).First();
                        }
                    }
                    else if (!blockObb.Contains(pt))
                    {
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                        break;
                    }
                    i++;
                }
                Point3d firstPt = line.GetClosestPointTo(connectPt, true);
                Point3d secondPt = line.EndPoint;
                Polyline newpolyline = new Polyline();
                double spacing = firstPt.DistanceTo(connectPt);
                double lineLength = firstPt.DistanceTo(secondPt);
                var VerticalDir = blockPt.GetVectorTo(connectPt).GetNormal();
                //平行连线
                if (VerticalDir.IsVertical(firstPt.GetVectorTo(secondPt)))
                {
                    //连线与块本身重合，需要切换连接点
                    connectPts.Remove(connectPt);
                    connectPt = connectPts.OrderBy(o => line.DistanceTo(o, false)).First();
                    var dir = VerticalDir + blockPt.GetVectorTo(connectPt).GetNormal();
                    Ray ray = new Ray();
                    ray.BasePoint = connectPt;
                    ray.UnitDir = dir;
                    var pts = new Point3dCollection();
                    ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    var RayIntersectPt = Point3d.Origin;
                    newpolyline.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                    if (pts.Count == 1)
                    {
                        RayIntersectPt = pts[0];
                    }
                    else if (pathedgs > i + 1)
                    {
                        i++;
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                        ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count == 1)
                        {
                            RayIntersectPt = pts[0];
                        }
                        else
                        {
                            return wiring;
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        return wiring;
                        throw new NotImplementedException();
                    }
                    newpolyline.AddVertexAt(1, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                    newpolyline.AddVertexAt(2, line.EndPoint.ToPoint2D(), 0, 0, 0);
                }
                //垂直连线
                else if (VerticalDir.IsParallelToEx(firstPt.GetVectorTo(secondPt)))
                {
                    if (pathedgs > i + 1 && lineLength < 200 && wiring.GetPoint3dAt(i).DistanceTo(wiring.GetPoint3dAt(i+1)) > 400)
                    {
                        //连线本身较短，需要切换连接点
                        i++;
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                        connectPts.Remove(connectPt);
                        connectPt = connectPts.OrderBy(o => line.DistanceTo(o, false)).First();
                        var dir = VerticalDir + blockPt.GetVectorTo(connectPt).GetNormal();
                        Ray ray = new Ray();
                        ray.BasePoint = connectPt;
                        ray.UnitDir = dir;
                        var pts = new Point3dCollection();
                        ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        var RayIntersectPt = Point3d.Origin;
                        if (pts.Count == 1)
                        {
                            RayIntersectPt = pts[0];
                            newpolyline.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                            newpolyline.AddVertexAt(1, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                            newpolyline.AddVertexAt(2, line.EndPoint.ToPoint2D(), 0, 0, 0);
                        }
                        else
                        {
                            //throw new NotImplementedException();
                            i--;
                            line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                            RayIntersectPt = firstPt;
                            newpolyline.AddVertexAt(0, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                            newpolyline.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);
                        }
                    }
                    else if (spacing < 5)
                    {
                        newpolyline.AddVertexAt(0, firstPt.ToPoint2D(), 0, 0, 0);
                        newpolyline.AddVertexAt(1, secondPt.ToPoint2D(), 0, 0, 0);
                    }
                    else
                    {
                        newpolyline.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                        var dir = VerticalDir.RotateBy(Math.PI / 4, Vector3d.ZAxis);
                        var RayIntersectPt = Point3d.Origin;
                        Ray ray = new Ray();
                        ray.BasePoint = connectPt;
                        ray.UnitDir = dir;
                        var pts = new Point3dCollection();
                        ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count == 1)
                        {
                            RayIntersectPt = pts[0];
                        }
                        else
                        {
                            dir = VerticalDir.RotateBy(-Math.PI / 4, Vector3d.ZAxis);
                            ray.UnitDir = dir;
                            ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count == 1)
                            {
                                RayIntersectPt = pts[0];
                            }
                            else
                            {
                                return wiring;
                                throw new NotImplementedException();
                            }
                        }
                        newpolyline.AddVertexAt(1, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                        newpolyline.AddVertexAt(2, secondPt.ToPoint2D(), 0, 0, 0);
                    }
                }
                //倾斜连线
                else
                {
                    newpolyline.AddVertexAt(0, intersectPt.ToPoint2D(), 0, 0, 0);
                    newpolyline.AddVertexAt(1, secondPt.ToPoint2D(), 0, 0, 0);
                    //throw new NotImplementedException();
                }

                int Number = pathedgs - i + 1;
                int newEdgesCount = newpolyline.NumberOfVertices;
                for (int j = 2; j < Number; j++)
                {
                    newpolyline.AddVertexAt(newEdgesCount++, wiring.GetPoint3dAt(++i).ToPoint2D(), 0, 0, 0);
                }
                //if (!(newpolyline.EndPoint.DistanceTo(wiring.EndPoint) < 10))
                //{
                //    newpolyline.AddVertexAt(newpolyline.NumberOfVertices, wiring.EndPoint.ToPoint2D(), 0, 0, 0);
                //}
                using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
                {
                    //acad.ModelSpace.Add(blockObb);
                    //acad.ModelSpace.Add(line);
                    //acad.ModelSpace.Add(wiring.Clone() as Entity);
                    //acad.ModelSpace.Add(new Circle() { Center = intersectPt ,Radius = 9});
                    //acad.ModelSpace.Add(new Circle() { Center = connectPt ,Radius = 10});
                    //acad.ModelSpace.Add(new Circle() { Center = firstPt ,Radius = 11});
                    //acad.ModelSpace.Add(new Circle() { Center = secondPt ,Radius = 12});
                }
                return newpolyline;
            }
        }

        /// <summary>
        /// 更新连接线
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="connectPt"></param>
        /// <param name="turnPt"></param>
        /// <returns></returns>
        private Polyline UpdateWiring(Polyline wiring, Point3d connectPt, Circle circle)
        {
            if (circle.EntityContains(wiring.StartPoint))
            {
                wiring.ReverseCurve();
            }
            Polyline poly = new Polyline();
            for (int i = 0; i < wiring.NumberOfVertices - 1; i++)
            {
                Point3d pt = wiring.GetPoint3dAt(i);
                Line line = new Line(pt, wiring.GetPoint3dAt((i + 1) % wiring.NumberOfVertices));
                poly.AddVertexAt(i, pt.ToPoint2D(), 0, 0, 0);
                if (line.GetClosestPointTo(connectPt, false).DistanceTo(pt) < tol)
                {
                    poly.AddVertexAt(i + 1, connectPt.ToPoint2D(), 0, 0, 0);
                    break;
                }
            }

            return poly;
        }

        private Polyline GetBlockReferenceOBB(Database database, BlockReference br)
        {
            using (var acadDatabase =Linq2Acad.AcadDatabase.Use(database))
            {
                // 业务需求: 去除隐藏的图元信息
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var entitys = blockTableRecord.GetEntities();
                var entities = entitys.Where(e =>
                {
                    if (e is AttributeDefinition) return false;//业务：过滤属性定义
                    if (e is BlockReference) return false;//业务：过滤块中块
                    var layerTableRecord = acadDatabase.Layers.Element(e.Layer);
                    return !layerTableRecord.IsFrozen & !layerTableRecord.IsOff;
                });
                var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                // 考虑到多段线不能使用非比例的缩放
                // 这里采用一个变通方法：
                // 将矩形柱转化成2d Solid，缩放后再转回多段线
                var solid = rectangle.ToSolid();
                solid.TransformBy(br.BlockTransform);
                return solid.ToPolyline().FlattenRectangle();
            }
        }
        public Polyline CennectToPoint(Polyline wiring, BlockReference block,Vector3d vector1 , List<Point3d> connectPts1,Vector3d vector2, List<Point3d> connectPts2)
        {
            using (Linq2Acad.AcadDatabase acac = Linq2Acad.AcadDatabase.Active())
            {
                var blockPt = new Point3d(block.Position.X, block.Position.Y, 0);
                var blockObb = GetBlockReferenceOBB(acac.Database, block);
                var Intersectpts = new Point3dCollection();
                wiring.IntersectWith(blockObb, Intersect.OnBothOperands, Intersectpts, (IntPtr)0, (IntPtr)0);
                if (Intersectpts.Count == 0)
                {
                    //acac.ModelSpace.Add(blockObb);
                    //acac.ModelSpace.Add(wiring.Clone() as Polyline);
                    return new Polyline();
                }
                int pathedgs = wiring.NumberOfVertices;

                int i = 1;
                Line line = new Line();
                while (i < pathedgs)
                {
                    var pt = wiring.GetPoint3dAt(i);
                    if (blockObb.Distance(pt) > 1 && !blockObb.Contains(pt))
                    {
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                        break;
                    }
                    i++;
                }
                var IntersectLineDir = line.LineDirection();
                Tuple<Vector3d, List<Point3d>> preferred = null, alternate = null;
                if (IntersectLineDir.IsParallelToEx(vector1))
                {
                    preferred = (vector1, connectPts1).ToTuple();
                    alternate = (vector2, connectPts2).ToTuple();
                }
                else if(IntersectLineDir.IsParallelToEx(vector2))
                {
                    preferred = (vector2, connectPts2).ToTuple();
                    alternate = (vector1, connectPts1).ToTuple(); 
                }
                else
                {
                    //倾斜插入块本身,不做处理
                    return CennectToPolyline();
                }
                if(line.Length < 10)
                {
                    return new Polyline();
                }
                Point3d intersectPt = line.ExtendLine(6).Buffer(6).Intersect(blockObb, Intersect.OnBothOperands).Cast<Point3d>().OrderBy(o => o.DistanceTo(line.EndPoint)).First();
                Point3d connectPt = preferred.Item2.OrderBy(o => o.DistanceTo(intersectPt)).First();
                Point3d firstPt = line.GetClosestPointTo(connectPt, true);
                Point3d secondPt = line.EndPoint;
                Polyline newpolyline = new Polyline();
                double spacing = firstPt.DistanceTo(connectPt);
                double lineLength = firstPt.DistanceTo(secondPt);
                var VerticalDir = blockPt.GetVectorTo(connectPt).GetNormal();
                {
                    if (pathedgs > i + 1 && (lineLength < 150 || spacing > lineLength))
                    {
                        //连线本身较短，需要切换连接点
                        i++;
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                        connectPt = alternate.Item2.OrderBy(o => line.DistanceTo(o, false)).First();
                        var dir = VerticalDir + blockPt.GetVectorTo(connectPt).GetNormal();
                        Ray ray = new Ray();
                        ray.BasePoint = connectPt;
                        ray.UnitDir = dir;
                        var pts = new Point3dCollection();
                        ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        var RayIntersectPt = Point3d.Origin;
                        if (pts.Count == 1)
                        {
                            RayIntersectPt = pts[0];
                            newpolyline.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                            newpolyline.AddVertexAt(1, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                            newpolyline.AddVertexAt(2, line.EndPoint.ToPoint2D(), 0, 0, 0);
                        }
                        else
                        {
                            //Rollback
                            i--;
                            line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                            return CennectToPolyline();
                        }
                    }
                    else if (spacing < 5)
                    {
                        return CennectToPolyline();
                    }
                    else
                    {
                        newpolyline.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                        var dir = VerticalDir.RotateBy(Math.PI / 4, Vector3d.ZAxis);
                        var RayIntersectPt = Point3d.Origin;
                        Ray ray = new Ray();
                        ray.BasePoint = connectPt;
                        ray.UnitDir = dir;
                        var pts = new Point3dCollection();
                        ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count == 1)
                        {
                            RayIntersectPt = pts[0];
                        }
                        else
                        {
                            dir = VerticalDir.RotateBy(-Math.PI / 4, Vector3d.ZAxis);
                            ray.UnitDir = dir;
                            ray.IntersectWith(line, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count == 1)
                            {
                                RayIntersectPt = pts[0];
                            }
                            else
                            {
                                return CennectToPolyline();
                            }
                        }
                        newpolyline.AddVertexAt(1, RayIntersectPt.ToPoint2D(), 0, 0, 0);
                        newpolyline.AddVertexAt(2, secondPt.ToPoint2D(), 0, 0, 0);
                    }
                }

                int Number = pathedgs - i + 1;
                int newEdgesCount = newpolyline.NumberOfVertices;
                for (int j = 2; j < Number; j++)
                {
                    newpolyline.AddVertexAt(newEdgesCount++, wiring.GetPoint3dAt(++i).ToPoint2D(), 0, 0, 0);
                }
                return newpolyline;

                Polyline CennectToPolyline()
                {
                    var objs = ThDrawTool.Explode(block).Cast<Entity>().ToList();
                    var blockGeos = ExplodeToBasic(objs);
                    var blockLst = blockGeos.Cast<Curve>().Select(o => Z0Curves(o)).Where(o => o.DistanceTo(blockPt, false) > 100).ToList();
                    var intersectPts = blockLst.SelectMany(o => o.Intersect(wiring, Intersect.OnBothOperands));
                    var pt = intersectPts.Cast<Point3d>().OrderBy(p => p.DistanceTo(line.EndPoint)).First();
                    newpolyline = new Polyline();
                    while (!pt.IsPointOnLine(line,1))
                    {
                        i--;
                        line = new Line(wiring.GetPoint3dAt(i - 1), wiring.GetPoint3dAt(i));
                    }
                    newpolyline.AddVertexAt(0, pt.ToPoint2D(), 0, 0, 0);
                    var number = pathedgs - i;
                    var EdgesCount = 1;
                    for (int j = 0; j < number; j++)
                    {
                        newpolyline.AddVertexAt(EdgesCount++, wiring.GetPoint3dAt(i++).ToPoint2D(), 0, 0, 0);
                    }
                    return newpolyline;
                }
            }
        }

        /// <summary>
        /// 处理块中的图元
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        private DBObjectCollection ExplodeToBasic(List<Entity> objs)
        {
            //理想是炸到Line,Arc,Circle,Ellipse,目前不支持对椭圆的处理，这里不抛出不支持的异常
            double TesslateLength = 50;
            var results = new DBObjectCollection();
            objs.Where(o => o is Curve).ForEach(c =>
            {
                if (c is Line)
                {
                    results.Add(c);
                }
                else if (c is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(TesslateLength));
                }
                else if (c is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(TesslateLength));
                }
                else if (c is Polyline2d || c is Polyline)
                {
                    var subObjs = new DBObjectCollection();
                    c.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                }
            });
            return results;
        }

        /// <summary>
        /// Z值归0
        /// </summary>
        /// <param name="curves"></param>
        /// <returns></returns>
        private Curve Z0Curves(Curve curve)
        {
            curve.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 1e99)));
            curve.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, -1e99)));
            return curve;
        }
    }
}
