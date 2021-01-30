﻿using System;
using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.LaneLine
{
    public abstract class ThLaneLineEngine
    {
        public static double extend_distance = 20.0;
        public static double collinear_gap_distance = 2.0;

        public static DBObjectCollection Noding(DBObjectCollection curves)
        {
            return NodingLines(curves).ToCollection();
        }

        public static DBObjectCollection Explode(DBObjectCollection curves)
        {
            return ExplodeCurves(curves).ToCollection();
        }

        public static DBObjectCollection CleanZeroCurves(DBObjectCollection curves)
        {
            return curves.Cast<Line>().Where(c => c.Length > ThMEPEngineCoreCommon.LOOSE_ZERO_LENGTH).ToCollection();
        }

        public static DBObjectCollection Simplify(DBObjectCollection curves)
        {
            return curves.Cast<Polyline>().Select(o => o.TPSimplify(extend_distance)).ToCollection();
        }

        protected static List<Curve> ExplodeCurves(DBObjectCollection curves)
        {
            var objs = new List<Curve>();
            foreach (Curve curve in curves)
            {
                if (curve is Line line)
                {
                    if (line.Length > ThMEPEngineCoreCommon.LOOSE_ZERO_LENGTH)
                    {
                        objs.Add(line.WashClone() as Line);
                    }
                }
                else if (curve is Polyline polyline)
                {
                    var entitySet = new DBObjectCollection();
                    polyline.Explode(entitySet);
                    objs.AddRange(ExplodeCurves(entitySet));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return objs;
        }

        protected static List<Line> NodingLines(DBObjectCollection curves)
        {
            var results = new List<Line>();
            var geometry = curves.ToNTSNodedLineStrings();
            if (geometry is LineString line)
            {
                results.Add(line.ToDbline());
            }
            else if (geometry is MultiLineString lines)
            {
                results.AddRange(lines.Geometries.Cast<LineString>().Select(o => o.ToDbline()));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }

        protected static List<DBObjectCollection> GroupParallelLines(DBObjectCollection curves)
        { 
            // 利用建立空间索引剔除重复对象（几何意义上的重复）
            var spatialIndex = new ThCADCoreNTSSpatialIndex(curves);
            var lines = spatialIndex.Geometries.Values.ToCollection();
            lines.Cast<Line>().ForEach(o =>
            {
                var buffer = ExpandBy(o, extend_distance, collinear_gap_distance);
                var objs = spatialIndex.SelectCrossingPolygon(buffer);
                if (objs.Count > 1)
                {
                    var parallelLines = objs.Cast<Line>().Where(l => IsParallel(o, l));
                    if (parallelLines.Count() > 1)
                    {
                        var tagLines = parallelLines.Where(l => spatialIndex.Tag(l) != null);
                        if (tagLines.Count() == 0)
                        {
                            var tag = Guid.NewGuid().ToString();
                            parallelLines.ForEach(l => spatialIndex.AddTag(l, tag));
                        }
                        else
                        {
                            var tag = spatialIndex.Tag(tagLines.First());
                            parallelLines.ForEach(l => spatialIndex.AddTag(l, tag));
                        }
                    }
                }
            });
            var results = new List<DBObjectCollection>();
            var groups = lines.Cast<Line>().GroupBy(o => spatialIndex.Tag(o));
            foreach (var group in groups)
            {
                if (group.Key == null)
                {
                    group.ForEach(o => results.Add(new DBObjectCollection(){ o }));
                }
                else
                {
                    results.Add(group.ToList().ToCollection());
                }
            }
            return results;
        }

        protected static Polyline ExpandBy(Line line, double xOffset, double yOffset)
        {
            Vector3d xaxis = line.LineDirection();
            Vector3d yaxis = line.Normal.CrossProduct(xaxis).GetNormal();
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(new Point3dCollection()
            {
                line.StartPoint - xaxis * xOffset + yaxis * yOffset,
                line.StartPoint - xaxis * xOffset - yaxis * yOffset,
                line.EndPoint + xaxis * xOffset - yaxis * yOffset,
                line.EndPoint + xaxis * xOffset + yaxis * yOffset,
            });
            return pline;
        }

        protected static Line CenterLine(Geometry geometry)
        {
            var rectangle = MinimumDiameter.GetMinimumRectangle(geometry) as Polygon;
            var shell = rectangle.Shell.ToDbPolyline();
            return new Line(
                shell.GetPoint3dAt(0) + 0.5 * (shell.GetPoint3dAt(1) - shell.GetPoint3dAt(0)),
                shell.GetPoint3dAt(2) + 0.5 * (shell.GetPoint3dAt(3) - shell.GetPoint3dAt(2)));
        }

        protected static bool IsParallel(Line line1, Line line2)
        {
            return ThGeometryTool.IsParallelToEx(line1.LineDirection(), line2.LineDirection());
        }
    }
}
