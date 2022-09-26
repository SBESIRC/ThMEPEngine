using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPTCH.TCHArchDataConvert;
using ThMEPTCH.TCHArchDataConvert.THArchEntity;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.Services
{
    public class ThArchWallFixer
    {
        private static readonly double ExtendTolerance = 200.0;
        private static readonly double BufferTolerance = 10.0;

        /// <summary>
        /// 建筑墙融合
        /// </summary>
        /// <param name="thArchWalls"></param>
        /// <returns></returns>
        public static List<WallEntity> Union(List<WallEntity> thArchWalls)
        {
            // 暂不考虑偏心墙
            var geometryDict = new Dictionary<Line, WallEntity>();
            thArchWalls.ForEach(o => geometryDict.Add(o.CenterCurve as Line, o));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometryDict.Select(o => o.Key).ToCollection());
            var dictList = geometryDict.ToList();
            for (var i = 0; i < dictList.Count; i++)
            {
                if (!geometryDict.ContainsKey(dictList[i].Key))
                {
                    continue;
                }

                var direction = dictList[i].Key.LineDirection();
                var spExtend = new Line(dictList[i].Key.StartPoint - direction * ExtendTolerance, dictList[i].Key.StartPoint);
                SearchAndConnect(geometryDict, dictList, spatialIndex, dictList[i], spExtend, direction, dictList[i].Key.EndPoint);

                var epExtend = new Line(dictList[i].Key.EndPoint, dictList[i].Key.EndPoint + direction * ExtendTolerance);
                SearchAndConnect(geometryDict, dictList, spatialIndex, dictList[i], epExtend, direction, dictList[i].Key.StartPoint);
            }

            return geometryDict.Select(o => o.Value).ToList();
        }

        private static void SearchAndConnect(Dictionary<Line, WallEntity> geometryDict, List<KeyValuePair<Line, WallEntity>> dictList, ThCADCoreNTSSpatialIndex spatialIndex, KeyValuePair<Line, WallEntity> pair, Line extendLine, Vector3d direction, Point3d startPoint)
        {
            var buffer = extendLine.BufferSquare(BufferTolerance);
            var crossingEntities = spatialIndex.SelectCrossingPolygon(buffer).OfType<Line>();
            if (!crossingEntities.Contains(pair.Key))
            {
                return;
            }

            var filter = crossingEntities.Except(new List<Line> { pair.Key }).ToList();
            var parallel = filter.Where(o => GetWidth(geometryDict[o]).Equals(GetWidth(pair.Value))).Where(o => Math.Abs(o.LineDirection().DotProduct(direction)) > Math.Cos(Math.PI * 1 / 180.0)).OrderByDescending(o => o.Length).FirstOrDefault();

            if (!parallel.IsNull())
            {
                var gap = 0.0;
                var vertical = filter.Except(new List<Line> { parallel }).FirstOrDefault();
                if (!vertical.IsNull())
                {
                    gap = GetWidth(geometryDict[vertical]);
                }

                // 直接相连或通过其他墙相连
                if (parallel.Distance(pair.Key) < BufferTolerance + gap)
                {
                    var endPoint = startPoint.DistanceTo(parallel.StartPoint) > startPoint.DistanceTo(parallel.EndPoint) ? parallel.StartPoint : parallel.EndPoint;
                    var parallelWall = geometryDict[parallel].DBArchEntity as TArchWall;
                    var archEntity = pair.Value.DBArchEntity as TArchWall;
                    var archWall = new TArchWall
                    {
                        StartPoint = startPoint,
                        EndPoint = endPoint,
                        LeftWidth = archEntity.LeftWidth,
                        RightWidth = archEntity.RightWidth,
                        IsArc = false,
                        Bulge = archEntity.Bulge,
                        Height = archEntity.Height,
                        Elevation = archEntity.Elevation,
                        Material = archEntity.Material,

                        // 标识信息
                        Id = archEntity.Id + parallelWall.Id,
                    };

                    geometryDict.Remove(parallel);
                    geometryDict.Remove(pair.Key);
                    var wallEntity = DBToTHEntityCommon.TArchWallToEntityWall(archWall, 0, 0, 0, 0, new Vector3d(0, 0, 0));
                    geometryDict.Add(wallEntity.CenterCurve as Line, wallEntity);
                    dictList.Add(new KeyValuePair<Line, WallEntity>(wallEntity.CenterCurve as Line, wallEntity));

                    // 更新索引
                    spatialIndex.Update(new DBObjectCollection { wallEntity.CenterCurve }, new DBObjectCollection { parallel, pair.Key });
                }
                else
                {
                    if (!geometryDict.ContainsKey(pair.Key))
                    {
                        geometryDict.Add(pair.Key, pair.Value);
                    }
                }
            }
            else
            {
                if (!geometryDict.ContainsKey(pair.Key))
                {
                    geometryDict.Add(pair.Key, pair.Value);
                }
            }
        }

        private static double GetWidth(WallEntity wall)
        {
            return wall.LeftWidth + wall.RightWidth;
        }
    }
}
