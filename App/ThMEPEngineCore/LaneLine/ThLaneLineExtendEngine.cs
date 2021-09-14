﻿using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.KdTree;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.LaneLine
{
    public class ThLaneLineExtendEngine : ThLaneLineEngine
    {
        public static DBObjectCollection Extend(DBObjectCollection curves)
        {
            var extendedLines = CreateExtendedLines(curves);
            var allLines = curves.Cast<Line>().Union(extendedLines).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(allLines);
            extendedLines.RemoveAll(o =>
            {
                var objs = spatialIndex.SelectFence(o);
                objs.Remove(o);
                return !IsProperIntersects(objs, o);
            });
            return curves.Cast<Line>().Union(extendedLines).ToCollection();
        }

        public static DBObjectCollection ExtendEx(DBObjectCollection curves)
        {
            var extendedInfo = CreateExtendedLinesEx(curves);
            var extendedLines = extendedInfo.SelectMany(x => x.Value).ToList();
            var allLines = curves.Cast<Line>().Union(extendedLines).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(allLines);
            extendedLines.RemoveAll(o =>
            {
                var objs = spatialIndex.SelectFence(o);
                objs.Remove(o);
                var exLines = extendedInfo.Where(x => x.Value.Contains(o)).Select(x => x.Key).ToList();
                foreach (var exLine in exLines)
                {
                    objs.Remove(exLine);
                }

                return !IsProperIntersectsEx(objs, o);
            });

            return curves.Cast<Line>().Union(extendedLines).ToCollection();
        }

        private static bool IsProperIntersects(DBObjectCollection lines, Line line)
        {
            // 计算交点
            var geometry = lines.Intersection(line);
            // 判断是否存在多个交点（但是要排查共线的情况）
            if (geometry is MultiPoint points)
            {
                // 考虑到精度问题，在一定范围内的点认为是一个点
                var kdTree = new KdTree<Point>(1.0);
                var kdNodes = points.Geometries.Cast<Point>().Select(o => kdTree.Insert(o.Coordinate, o));
                return kdNodes.Distinct().Count() > 1;
            }
            return false;
        }

        private static List<Line> CreateExtendedLines(DBObjectCollection lines)
        {
            var objs = new List<Line>();
            lines.Cast<Line>().ForEach(o =>
            {
                var direction = o.LineDirection();
                objs.Add(new Line(o.EndPoint, o.EndPoint + direction * extend_distance));
                objs.Add(new Line(o.StartPoint, o.StartPoint - direction * extend_distance));
            });
            return objs;
        }

        private static bool IsProperIntersectsEx(DBObjectCollection lines, Line line)
        {
            // 计算交点
            var geometry = lines.Intersection(line);
            // 判断是否存在多个交点（但是要排查共线的情况）
            if (geometry is Point || geometry is MultiPoint)
            {
                return true;
            }
            return false;
        }

        private static Dictionary<Line, List<Line>> CreateExtendedLinesEx(DBObjectCollection lines)
        {
            Dictionary<Line, List<Line>> lineDic = new Dictionary<Line, List<Line>>();
            lines.Cast<Line>().ForEach(o =>
            {
                var objs = new List<Line>();
                var direction = o.LineDirection();
                objs.Add(new Line(o.EndPoint - extend_distance * direction, o.EndPoint + direction * extend_distance));
                objs.Add(new Line(o.StartPoint + extend_distance * direction, o.StartPoint - direction * extend_distance));

                lineDic.Add(o, objs);
            });
            return lineDic;
        }
    }
}
