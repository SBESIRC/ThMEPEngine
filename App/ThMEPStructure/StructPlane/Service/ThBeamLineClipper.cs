using System;
using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThBeamLineClipper:IDisposable
    {
        private double BufferTolerance = 1.0;
        private DBObjectCollection Polygons { get; set; }
        private DBObjectCollection BufferPolygons { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private double SnapTolerance=0.0;

        public ThBeamLineClipper(DBObjectCollection polygons)
        {
            Polygons = polygons;
            // 若对polygons进行了扩大，再对梁线裁剪会造成间隙
            // 如果需进行业务Snap,则要另外赋值
            SnapTolerance = BufferTolerance;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(polygons);

            // 对polygons Buffer (为了避免多次buffer)         
            BufferPolygons = DoBuffer(polygons, BufferTolerance);
        }
        public void Dispose()
        {
            BufferPolygons = BufferPolygons.Difference(Polygons);
            BufferPolygons.MDispose();
        }
        public DBObjectCollection Handle(DBObjectCollection beamLines)
        {
            // 收集要释放的物体
            var garbages = new DBObjectCollection();

            // clip
            var newBeamLines = DoClip(beamLines, BufferPolygons);
            garbages = garbages.Union(newBeamLines);
            newBeamLines = FitlerSmallLength(newBeamLines);

            // snap
            newBeamLines = DoSnap(newBeamLines);
            garbages = garbages.Union(newBeamLines);
            newBeamLines = FitlerSmallLength(newBeamLines);

            var results = new DBObjectCollection();
            results = results.Union(newBeamLines);

            // 释放
            garbages = garbages.Difference(results);
            garbages = garbages.Difference(beamLines);
            garbages.MDispose();

            return results;
        }

        private DBObjectCollection DoSnap(DBObjectCollection beamLines)
        {
            if(SnapTolerance>0.0)
            {
                var results = new DBObjectCollection();
                beamLines.OfType<Line>().ForEach(l =>
                {
                    var dir = l.LineDirection();
                    // 延伸起点                    
                    var spNext = l.StartPoint - dir.MultiplyBy(SnapTolerance);
                    var spObjs = Query(l.StartPoint, spNext, 5.0);
                    
                    // 延伸终点
                    var epNext = l.EndPoint + dir.MultiplyBy(SnapTolerance);
                    var epObjs = Query(l.EndPoint, epNext, 5.0);

                    var newLine = new Line(spNext, epNext);
                    var newSp = l.StartPoint;
                    double totalSnapLength = 0.0;
                    if (spObjs.Count>0)
                    {                       
                        var intersPts = spObjs
                        .OfType<Entity>()
                        .SelectMany(e=> ThGeometryTool.IntersectWithEx(newLine, e).OfType<Point3d>())
                        .ToCollection();
                        if(intersPts.Count>0)
                        {
                            totalSnapLength += SnapTolerance;
                            newSp = intersPts.OfType<Point3d>().OrderBy(p => p.DistanceTo(newSp)).First();
                        }
                    }
                    var newEp = l.EndPoint;
                    if (epObjs.Count > 0)
                    {
                        var intersPts = epObjs
                        .OfType<Entity>()
                        .SelectMany(e => ThGeometryTool.IntersectWithEx(newLine, e).OfType<Point3d>())
                        .ToCollection();
                        if (intersPts.Count > 0)
                        {
                            totalSnapLength += SnapTolerance;
                            newEp = intersPts.OfType<Point3d>().OrderBy(p => p.DistanceTo(newEp)).First();
                        }
                    }
                    if(Math.Abs(newSp.DistanceTo(newEp) - l.Length)<=1e-6 || 
                    (newSp.DistanceTo(newEp) - l.Length)> totalSnapLength)
                    {
                        // 如果长度没变,或调整的长度过长，则不做调整
                        results.Add(l);
                    }
                    else
                    {
                        results.Add(new Line(newSp, newEp));
                    }
                });

                return results;
            }
            else
            {
                return beamLines;
            }
        }

        private DBObjectCollection Query(Point3d sp,Point3d ep,double width)
        {
            var outline = ThDrawTool.ToRectangle(sp, ep, width);
            var results = SpatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return results;
        }

        private DBObjectCollection DoBuffer(DBObjectCollection polygons,double length)
        {
            if(length>0.0)
            {
                var results = new DBObjectCollection();
                polygons.OfType<Entity>().ForEach(polygon =>
                {
                    if (polygon is Polyline poly)
                    {
                        results = results.Union(poly.Buffer(length));
                    }
                    else if (polygon is MPolygon mPolygon)
                    {
                        results = results.Union(mPolygon.Buffer(length));
                    }
                });
                var validPolygons = results.FilterSmallArea(1.0);
                var invalidPolygons = results.Difference(validPolygons);
                invalidPolygons.MDispose();
                return validPolygons;
            }
            else
            {
                return polygons;
            }
        }

        private DBObjectCollection DoClip(DBObjectCollection beamLines, DBObjectCollection polygons)
        {
            var lastObjs = beamLines;
            var garbages = new DBObjectCollection();
            polygons.OfType<Entity>().ForEach(e =>
            {
                // 反向裁剪，保留外面的实体
                var tempObjs = e.Clip(lastObjs, true);
                garbages = garbages.Union(tempObjs);
                lastObjs = tempObjs;
            });            
            
            // 炸成线
            var lines = ExplodeToLines(lastObjs);
            garbages = garbages.Difference(beamLines);
            garbages = garbages.Difference(lines);
            garbages.MDispose();

            return lines;
        }
        private DBObjectCollection ExplodeToLines(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Curve>().ForEach(e =>
            {
                if(e is Line line)
                {
                    results.Add(line);
                }
                else if (e is Polyline polyline)
                {
                    var lineObjs = new DBObjectCollection();
                    polyline.Explode(lineObjs);
                    lineObjs.OfType<Line>().ForEach(l => results.Add(l));
                }
                else
                {
                    //
                }
            });
            return results;
        }
        private DBObjectCollection FitlerSmallLength(DBObjectCollection lines,double tolerance=1.0)
        {
            return lines.OfType<Line>().Where(o => o.Length > tolerance).ToCollection();
        }
    }
}
