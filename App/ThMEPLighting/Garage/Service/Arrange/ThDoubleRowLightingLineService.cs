using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public class ThDoubleRowLightingLineService
    {
        public double DoubleRowOffsetDis { get; set; }

        public ThDoubleRowLightingLineService(double doubleRowOffsetDis)
        {
            DoubleRowOffsetDis = doubleRowOffsetDis;
        }

        public void LightingLineBuffer(ThRegionBorder regionBorder)
        {
            // Buffer
            // Extend
            // Trim
            // 用非灯线连接
            // 创建Node后的1、2号线（可以是无序的）
            // 后期如果让用户指定起始点的，需要做一个排序
            var objs = new List<Curve>();
            var centerLineIndex = new ThCADCoreNTSSpatialIndex(regionBorder.DxCenterLines.ToCollection());
            regionBorder.DxCenterLines.ForEach(line =>
            {
                var startCurve = TryToCreatePolyline(line, true, centerLineIndex);
                var endCurve = TryToCreatePolyline(line, false, centerLineIndex);
                if (startCurve is Polyline && endCurve is Polyline)
                {
                    objs.Add(startCurve);
                    objs.Add(endCurve);
                }
                else if (startCurve is Polyline)
                {
                    objs.Add(startCurve);
                }
                else if (endCurve is Polyline)
                {
                    objs.Add(endCurve);
                }
                else
                {
                    objs.Add(line);
                }
            });

            // 将直接连接的两根线合并成一根Polyline，确保buffer时形状规整
            var obbs = objs.ToCollection().Buffer(DoubleRowOffsetDis / 2).OfType<Polyline>();
            var lightingLines = new List<Line>();
            obbs.ForEach(p =>
            {
                var entities = new DBObjectCollection();
                p.Explode(entities);
                entities = ThLaneLineMergeExtension.Merge(entities.OfType<Line>().Where(o => o.Length >= 10).ToCollection());
                entities.OfType<Line>().ForEach(l =>
                {
                    if (l.Length < DoubleRowOffsetDis + 1 && l.Length > DoubleRowOffsetDis - 1)
                    {
                        var square = LineCenter(l).CreateSquare(10.0);
                        var crossingPolygon = centerLineIndex.SelectCrossingPolygon(square);
                        if (crossingPolygon.Count > 0)
                        {
                            return;
                        }
                        var fence = centerLineIndex.SelectFence(square);
                        if (fence.Count > 0)
                        {
                            return;
                        }
                    }
                    lightingLines.Add(l);
                });
            });
            lightingLines = ThLaneLineMergeExtension.Merge(lightingLines.ToCollection()).OfType<Line>().ToList();

            // 延伸非灯线，缩进单排线槽线
            Extend(regionBorder.FdxCenterLines, lightingLines);
            Shorten(regionBorder.SingleRowLines, lightingLines);

            // 在交叉处对线进行延伸
            var extendLines = new List<Line>();
            lightingLines.ForEach(l =>
            {
                NonLightingLineExtend(l, true, centerLineIndex, extendLines);
                NonLightingLineExtend(l, false, centerLineIndex, extendLines);
            });
            extendLines = ThLaneLineMergeExtension.Merge(extendLines.ToCollection()).OfType<Line>().ToList();

            // 保留十字型车道线的较长边和T字型的横边
            CrossLineMerge(regionBorder.DxCenterLines, lightingLines, ref extendLines);
            var beforeNumber = lightingLines.Count;
            lightingLines = ThLaneLineMergeExtension.Merge(lightingLines.ToCollection()).OfType<Line>().ToList();
            var afterNumer = lightingLines.Count;
            while (beforeNumber > afterNumer)
            {
                beforeNumber = afterNumer;
                lightingLines = ThLaneLineMergeExtension.Merge(lightingLines.ToCollection()).OfType<Line>().ToList();
                afterNumer = lightingLines.Count;
            }

            // 检测连通性添加非灯线
            AddNonLightingLine(lightingLines, regionBorder.FdxCenterLines, regionBorder.DxCenterLines, extendLines);
            extendLines = extendLines.Except(regionBorder.FdxCenterLines).ToList();
            regionBorder.ExtendLines = extendLines;

            // 对中心线归一化后，将灯线分为1、2号线
            var firstLines = new List<Line>();
            var secondLines = new List<Line>();
            regionBorder.DxCenterLines = ThCenterLineService.NormalizeEx(regionBorder.DxCenterLines); //有序化
            Distinguish(regionBorder.DxCenterLines, lightingLines, firstLines, secondLines);

            // 连接处打断
            regionBorder.FirstLightingLines = Noding(firstLines);
            regionBorder.SecondLightingLines = Noding(secondLines);

            //using (var acad = Linq2Acad.AcadDatabase.Active())
            //{
            //    var results = ThLightingLineCorrectionService.SingleRowCorrect(regionBorder.FirstLightingLines,
            //        regionBorder.SecondLightingLines, 1000.0);
            //    results.ForEach(x => acad.ModelSpace.Add(x));
            //    regionBorder.SecondLightingLines.ForEach(x => acad.ModelSpace.Add(x.Clone() as Entity));
            //}
        }

        private Curve TryToCreatePolyline(Line line, bool isStartPoint, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var square = isStartPoint ? line.StartPoint.CreateSquare(10.0) : line.EndPoint.CreateSquare(10.0);
            var filter = spatialIndex.SelectCrossingPolygon(square);
            if (filter.Count == 2)
            {
                var other = filter.OfType<Line>().Except(new List<Line> { line }).First();
                var vertices = new Point3dCollection();
                if (isStartPoint)
                {
                    vertices.Add(line.EndPoint);
                    vertices.Add(line.StartPoint);
                }
                else
                {
                    vertices.Add(line.StartPoint);
                    vertices.Add(line.EndPoint);
                }
                if (isStartPoint)
                {
                    if (other.StartPoint.DistanceTo(line.StartPoint) < 10.0)
                    {
                        vertices.Add(other.EndPoint);
                    }
                    else
                    {
                        vertices.Add(other.StartPoint);
                    }
                }
                else
                {
                    if (other.StartPoint.DistanceTo(line.EndPoint) < 10.0)
                    {
                        vertices.Add(other.EndPoint);
                    }
                    else
                    {
                        vertices.Add(other.StartPoint);
                    }
                }

                var pline = new Polyline();
                pline.CreatePolyline(vertices);
                return pline;
            }
            return line;
        }

        private Point3d LineCenter(Line line)
        {
            return new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, 0);
        }

        private void NonLightingLineExtend(Line line, bool isStartPoint, ThCADCoreNTSSpatialIndex centerLineIndex,
             List<Line> extendLines)
        {
            var newLine = isStartPoint ? new Line(line.StartPoint, line.StartPoint - line.LineDirection() * DoubleRowOffsetDis)
                : new Line(line.EndPoint, line.EndPoint + line.LineDirection() * DoubleRowOffsetDis);
            var lineFrame = LineCenter(newLine).CreateSquare(10.0);
            var crossLines = centerLineIndex.SelectFence(lineFrame);
            if (crossLines.Count == 1)
            {
                extendLines.Add(newLine);
            }
        }

        private void CrossLineMerge(List<Line> centerLines, List<Line> lightingLines, ref List<Line> extendLines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(extendLines.ToCollection());
            var lightingIndex = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());
            for (var i = 0; i < centerLines.Count; i++)
            {
                for (var j = i + 1; j < centerLines.Count; j++)
                {
                    var points = centerLines[i].IntersectWithEx(centerLines[j]);
                    if (points.Count != 1)
                    {
                        continue;
                    }
                    var square = points[0].CreateSquare(DoubleRowOffsetDis + 10.0);
                    var addLine = new List<Line>();
                    if (IsEndPoint(points[0], centerLines[i]))
                    {
                        if (IsEndPoint(points[0], centerLines[j]))
                        {
                            // 两边都是端点
                            addLine = HandleEvenEdge(centerLines[i], centerLines[j], spatialIndex, square);

                            // 对L字型灯线进行修剪
                            if (addLine.Count == 1)
                            {
                                var filterFrame = addLine[0].StartPoint.CreateSquare(10.0);
                                var filterLightingLine = lightingIndex.SelectCrossingPolygon(filterFrame);
                                if (filterLightingLine.Count != 1)
                                {
                                    filterFrame = addLine[0].EndPoint.CreateSquare(10.0);
                                    filterLightingLine = lightingIndex.SelectCrossingPolygon(filterFrame);
                                }
                                if (filterLightingLine.Count == 1)
                                {
                                    var thisLine = filterLightingLine[0] as Line;
                                    var direction = thisLine.LineDirection();
                                    if (thisLine.StartPoint.DistanceTo(points[0]) < thisLine.EndPoint.DistanceTo(points[0]))
                                    {
                                        var newLine = new Line(thisLine.StartPoint + DoubleRowOffsetDis * direction, thisLine.EndPoint);
                                        lightingLines.Add(newLine);
                                        lightingLines.Remove(thisLine);
                                        Update(lightingIndex, newLine, thisLine);
                                        extendLines.Add(new Line(thisLine.StartPoint, thisLine.StartPoint + DoubleRowOffsetDis * direction));
                                    }
                                    else
                                    {
                                        var newLine = new Line(thisLine.StartPoint, thisLine.EndPoint - DoubleRowOffsetDis * direction);
                                        lightingLines.Add(newLine);
                                        lightingLines.Remove(thisLine);
                                        Update(lightingIndex, newLine, thisLine);
                                        extendLines.Add(new Line(thisLine.EndPoint - DoubleRowOffsetDis * direction, thisLine.EndPoint));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // T字型，i边端点，j边非端点
                            addLine = HandleOddEdge(centerLines[j], spatialIndex, square);
                        }
                    }
                    else
                    {
                        if (IsEndPoint(points[0], centerLines[j]))
                        {
                            // T字型，i边非端点，j边端点
                            addLine = HandleOddEdge(centerLines[i], spatialIndex, square);
                        }
                        else
                        {
                            // 十字型
                            addLine = HandleEvenEdge(centerLines[i], centerLines[j], spatialIndex, square);
                        }
                    }
                    lightingLines.AddRange(addLine);
                    extendLines = extendLines.Except(addLine).ToList();
                }
            }
        }

        private void AddNonLightingLine(List<Line> lightingLines, List<Line> nonLightingLines, List<Line> centerLines, List<Line> extendLines)
        {
            var searchedLine = new List<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());
            var graphFrames = new List<Polyline>();
            lightingLines.ForEach(line =>
            {
                if (searchedLine.Contains(line))
                {
                    return;
                }

                searchedLine.Add(line);
                var searchFrame = line.BufferSquare(10.0);
                var filter = searchFrame.SelectCrossingEntities(spatialIndex).Except(searchedLine).ToList();
                while (filter.Count() != 0)
                {
                    searchedLine.AddRange(filter);
                    var unionArea = new DBObjectCollection();
                    unionArea.Add(searchFrame);
                    filter.ForEach(o =>
                    {
                        unionArea.Add(o.BufferSquare(10.0));
                    });
                    searchFrame = unionArea.ToNTSMultiPolygon().Union().ToDbCollection().OfType<Polyline>()
                        .OrderByDescending(p => p.Area).First();
                    filter = searchFrame.SelectCrossingEntities(spatialIndex).Except(searchedLine).ToList();
                }
                graphFrames.Add(searchFrame);
            });

            if (graphFrames.Count > 1)
            {
                var graphIndex = new ThCADCoreNTSSpatialIndex(graphFrames.ToCollection());
                ExtendLineSort(extendLines, centerLines).ForEach(line =>
                {
                    var lineBuffer = line.BufferSquare(10.0);
                    var frames = graphIndex.SelectCrossingPolygon(lineBuffer);
                    if (frames.Count > 1)
                    {
                        nonLightingLines.Add(line);
                        frames.Add(lineBuffer);
                        var union = frames.ToNTSMultiPolygon().Union().ToDbCollection();
                        graphIndex.Update(union, frames);
                    }
                });
            }
        }

        private void Extend(List<Line> nonLightingLines, List<Line> lightingLines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());
            for (var i = 0; i < nonLightingLines.Count; i++)
            {
                var buffer = nonLightingLines[i].BufferSquare(10.0);
                var filter = buffer.SelectCrossingEntities(spatialIndex);
                if (filter.Count > 0)
                {
                    // 终点延伸
                    var endNewLine = new Line(nonLightingLines[i].StartPoint,
                        nonLightingLines[i].EndPoint + nonLightingLines[i].LineDirection() * DoubleRowOffsetDis / 2);
                    var pointSquare = endNewLine.EndPoint.CreateSquare(10.0);
                    var endExtendSearch = pointSquare.SelectCrossingEntities(spatialIndex);

                    // 起点延伸
                    var startNewLine = new Line(nonLightingLines[i].StartPoint - nonLightingLines[i].LineDirection() * DoubleRowOffsetDis / 2,
                        nonLightingLines[i].EndPoint);
                    pointSquare = startNewLine.StartPoint.CreateSquare(10.0);
                    var startExtendSearch = pointSquare.SelectCrossingEntities(spatialIndex);

                    if (endExtendSearch.Count > 0 && startExtendSearch.Count > 0)
                    {
                        nonLightingLines[i] = new Line(nonLightingLines[i].StartPoint - nonLightingLines[i].LineDirection() * DoubleRowOffsetDis / 2,
                        nonLightingLines[i].EndPoint + nonLightingLines[i].LineDirection() * DoubleRowOffsetDis / 2);
                    }
                    else if (endExtendSearch.Count > 0)
                    {
                        nonLightingLines[i] = endNewLine;
                    }
                    else if (startExtendSearch.Count > 0)
                    {
                        nonLightingLines[i] = startNewLine;
                    }
                }
            }
        }

        private void Shorten(List<Line> singleRowLines, List<Line> lightingLines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());
            for (var i = 0; i < singleRowLines.Count; i++)
            {
                var buffer = singleRowLines[i].BufferSquare(10.0);
                var filter = buffer.SelectCrossingEntities(spatialIndex);
                if (filter.Count > 0)
                {
                    // 终点延伸
                    var newLine = new Line(singleRowLines[i].EndPoint - singleRowLines[i].LineDirection() * DoubleRowOffsetDis / 2,
                        singleRowLines[i].EndPoint + singleRowLines[i].LineDirection() * DoubleRowOffsetDis / 2);
                    var pointSquare = newLine.EndPoint.CreateSquare(10.0);
                    var extendSearch = pointSquare.SelectCrossingEntities(spatialIndex);
                    if (extendSearch.Count > 1)
                    {
                        singleRowLines[i] = new Line(singleRowLines[i].StartPoint, newLine.StartPoint);
                    }
                    else
                    {
                        newLine = new Line(singleRowLines[i].StartPoint - singleRowLines[i].LineDirection() * DoubleRowOffsetDis / 2,
                            singleRowLines[i].StartPoint + singleRowLines[i].LineDirection() * DoubleRowOffsetDis / 2);
                        pointSquare = newLine.StartPoint.CreateSquare(10.0);
                        extendSearch = pointSquare.SelectCrossingEntities(spatialIndex);
                        if (extendSearch.Count > 1)
                        {
                            singleRowLines[i] = new Line(singleRowLines[i].EndPoint, newLine.EndPoint); ;
                        }
                    }
                }
            }
        }

        private void Distinguish(List<Line> centerLines, List<Line> lightingLines, List<Line> firstLines, List<Line> secondLines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lightingLines.ToCollection());
            centerLines.ForEach(line =>
            {
                var direction = line.LineDirection();
                var antiClockwise = line.LineDirection().TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin));
                var clockwise = line.LineDirection().TransformBy(Matrix3d.Rotation(-Math.PI / 2, Vector3d.ZAxis, Point3d.Origin));
                var leftLine = (line.Clone() as Line);
                leftLine.TransformBy(Matrix3d.Displacement(antiClockwise * DoubleRowOffsetDis / 2));
                spatialIndex.SelectCrossingPolygon(leftLine.Buffer(10.0)).OfType<Line>()
                    .Where(o => Math.Abs(o.LineDirection().DotProduct(direction)) > Math.Cos(1 / 180.0 * Math.PI))
                    .ForEach(o => firstLines.Add(o));

                var rightLine = (line.Clone() as Line);
                rightLine.TransformBy(Matrix3d.Displacement(clockwise * DoubleRowOffsetDis / 2));
                spatialIndex.SelectCrossingPolygon(rightLine.Buffer(10.0)).OfType<Line>()
                    .Where(o => Math.Abs(o.LineDirection().DotProduct(direction)) > Math.Cos(1 / 180.0 * Math.PI))
                    .ForEach(o => secondLines.Add(o));
            });

            lightingLines.Except(firstLines).Except(secondLines).ForEach(line => firstLines.Add(line));
        }

        private void LineSearch(Line line, Point3d center, ThCADCoreNTSSpatialIndex spatialIndex, Vector3d direction, Vector3d vertical, List<Line> lines)
        {
            var searchLine = new Line(center, center + vertical * DoubleRowOffsetDis / 2);
            var searchFrame = searchLine.BufferSquare(10.0);
            var filter = searchFrame.SelectCrossingEntities(spatialIndex);
            if (filter.Count == 1)
            {
                lines.Add(line);
            }
            else if (filter.Count > 1)
            {
                var parallelLines = filter.Where(o => Math.Abs(o.LineDirection().DotProduct(direction)) > Math.Cos(1 / 180.0 * Math.PI)).FirstOrDefault();
                if (!parallelLines.IsNull())
                {
                    lines.Add(line);
                }
            }
        }

        private List<Line> Noding(List<Line> firstLines)
        {
            var nodingService = new ThLineNodingService(firstLines, new List<Line>(), new List<Line>());
            nodingService.Noding();
            return nodingService.DxLines;
        }

        private bool IsEndPoint(Point3d point, Line line)
        {
            return point.DistanceTo(line.StartPoint) < 10.0 || point.DistanceTo(line.EndPoint) < 10.0;
        }

        private void Update(ThCADCoreNTSSpatialIndex index, Line add, Line delete)
        {
            index.Update(new DBObjectCollection { add }, new DBObjectCollection { delete });
        }

        private List<Line> ExtendLineSort(List<Line> extendLines, List<Line> centerLines)
        {
            return extendLines.OrderByDescending(o => Distance(o, centerLines)).ToList();
        }

        private double Distance(Line line, List<Line> centerLines)
        {
            return centerLines.Select(o => o.Distance(line)).OrderBy(o => o).First();
        }

        private List<Line> HandleEvenEdge(Line first, Line second, ThCADCoreNTSSpatialIndex spatialIndex, Polyline frame)
        {
            var filter = spatialIndex.SelectCrossingPolygon(frame);
            if (first.Length > second.Length)
            {
                return filter.OfType<Line>()
                    .Where(x => Math.Abs(x.LineDirection().DotProduct(first.LineDirection())) > Math.Cos(1 / 180.0 * Math.PI))
                    .ToList();
            }
            else
            {
                return filter.OfType<Line>()
                    .Where(x => Math.Abs(x.LineDirection().DotProduct(second.LineDirection())) > Math.Cos(1 / 180.0 * Math.PI))
                    .ToList();
            }
        }

        private List<Line> HandleOddEdge(Line main, ThCADCoreNTSSpatialIndex spatialIndex, Polyline frame)
        {
            var filter = spatialIndex.SelectCrossingPolygon(frame);
            return filter.OfType<Line>()
                .Where(x => Math.Abs(x.LineDirection().DotProduct(main.LineDirection())) > Math.Cos(1 / 180.0 * Math.PI))
                .ToList();

        }
    }
}
