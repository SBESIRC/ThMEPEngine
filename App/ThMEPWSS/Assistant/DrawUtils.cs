using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS.Assistant
{
    public class DrawingTransaction : IDisposable
    {
        public void Dispose()
        {
            DrawUtils.Draw();
        }
    }
    public static class DrawUtils
    {
        public static DrawingTransaction DrawingTransaction => new DrawingTransaction();
        public static Queue<Action<AcadDatabase>> DrawingQueue { get; } = new Queue<Action<AcadDatabase>>(4096);
        public static void Draw()
        {
            //Dbg.PrintLine("DrawUtils.Draw()");
            if (DrawingQueue.Count == 0) return;
            using (var adb = AcadDatabase.Active())
            {
                while (DrawingQueue.Count > 0)
                {
                    DrawingQueue.Dequeue()(adb);
                }
            }
        }
        public static Polyline DrawBoundaryLazy(Entity[] ents, double thickness = 2)
        {
            if (ents.Length == 0) return null;
            var lst = ents.Select(e => GeoAlgorithm.GetBoundaryRect(e)).ToList();
            var minx = lst.Select(r => r.MinX).Min();
            var miny = lst.Select(r => r.MinY).Min();
            var maxx = lst.Select(r => r.MaxX).Max();
            var maxy = lst.Select(r => r.MaxY).Max();
            return DrawRectLazy(new ThWGRect(minx, miny, maxx, maxy));
        }
        public static void DrawBoundaryLazy(Entity e, double thickness = 2)
        {
            DrawingQueue.Enqueue(adb => { DrawBoundary(adb.Database, e, thickness); });
        }
        public static void DrawBoundary(Database db, Entity e, double thickness)
        {
            if (e is BlockReference br)
            {
                var colle = br.ExplodeToDBObjectCollection();
                ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, colle.OfType<Entity>().ToArray());
                foreach (Entity ent in colle)
                {
                    ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, ent);
                }
            }
            else
            {
                ThMEPWSS.Uitl.DebugNs.DebugTool.DrawBoundary(db, thickness, e);
            }
        }
        public static Polyline DrawRectLazyFromLeftButtom(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y + height, leftButtom.Z));
        }
        public static Polyline DrawRectLazyFromLeftTop(Point3d leftButtom, double width, double height)
        {
            return DrawRectLazy(leftButtom, new Point3d(leftButtom.X + width, leftButtom.Y - height, leftButtom.Z));
        }
        public static Polyline DrawRectLazy(ThWGRect rect)
        {
            return DrawRectLazyFromLeftTop(new Point2d(rect.MinX, rect.MaxY).ToPoint3d(), rect.Width, rect.Height);
        }
        public static Polyline DrawRectLazy(Point3d pt1, Point3d pt2)
        {
            var polyline = ThMEPWSS.Uitl.DebugNs.DebugTool.CreateRectangle(pt1.ToPoint2D(), pt2.ToPoint2D());
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(polyline);
            });
            return polyline;
        }
        public static Point3d GetMidPoint(Point3d first, Point3d second)
        {
            var x = (first.X + second.X) / 2;
            var y = (first.Y + second.Y) / 2;

            return new Point3d(x, y, 0);
        }
        public static Circle DrawCircleLazy(ThWGRect rect)
        {
            var p1 = new Point3d(rect.MinX, rect.MinY, 0);
            var p2 = new Point3d(rect.MaxX, rect.MaxY, 0);
            var center = GetMidPoint(p1, p2);
            var radius = GeoAlgorithm.Distance(p1, p2) / 2;
            return DrawCircleLazy(center, radius);
        }
        public static Circle DrawCircleLazy(Point3d center, double radius)
        {
            if (radius <= 0) radius = 1;
            var circle = new Circle() { Center = center, Radius = radius };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(circle);
            });
            return circle;
        }
        public static Line DrawLineLazy(Point3d start, Point3d end)
        {
            var line = new Line() { StartPoint = start, EndPoint = end };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(line);
            });
            return line;
        }
        public static DBText DrawTextLazy(string text, double height, Point3d position)
        {
            var dbText = new DBText
            {
                TextString = text,
                Position = position,
                Height = height,
            };
            DrawingQueue.Enqueue(adb =>
            {
                adb.ModelSpace.Add(dbText);
            });
            return dbText;
        }
        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var curve in curves)
                {
                    var clone = curve.Clone() as Curve;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static void CreateLayer(string aimLayer, Color color)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = false;
                }
            }
        }
    }
}
