using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static class MCompute
    {
        public static int CatchedTimes = 0;
        public static bool LogInfo = false;
        public static int ThreadCnt = 1;
        public static Serilog.Core.Logger Logger;
        public static int CalculateTheTotalNumOfParkingSpace(List<SubArea> subAreas, ref List<MParkingPartitionPro> mParkingPartitionPros, ref MParkingPartitionPro mParkingPartition, bool display = false)
        {
            if (subAreas.Count == 0)
            {
                return -1;
            }
            if (!IsValidatedSolutions(subAreas)) return -2;

            MParkingPartitionPro.LayoutMode = VMStock.RunMode;
            MParkingPartitionPro.LayoutScareFactor_Intergral = VMStock.LayoutScareFactor_Intergral;
            MParkingPartitionPro.LayoutScareFactor_Adjacent = VMStock.LayoutScareFactor_Adjacent;
            MParkingPartitionPro.LayoutScareFactor_betweenBuilds = VMStock.LayoutScareFactor_betweenBuilds;
            MParkingPartitionPro.LayoutScareFactor_SingleVert = VMStock.LayoutScareFactor_SingleVert;
            MParkingPartitionPro.SingleVertModulePlacementFactor = VMStock.SingleVertModulePlacementFactor;
            MParkingPartitionPro.ScareEnabledForBackBackModule = VMStock.DoubleRowModularDecrease200;

            var Walls = new BlockingCollection<LineString>();
            var Cars = new BlockingCollection<InfoCar>();
            var Pillars = new BlockingCollection<Polygon>();
            var IniPillars = new BlockingCollection<Polygon>();
            var ObsVertices = new BlockingCollection<Coordinate>();
            var Lanes = new BlockingCollection<LineSegment>();
            var Boundary = subAreas[0].OutBound.Clone();
            Boundary = Boundary.Simplify();
            var obs = new List<Polygon>();
            foreach (var subArea in subAreas) obs.AddRange(subArea.Buildings);
            var ObstaclesSpacialIndex = new MNTSSpatialIndex(obs);
            subAreas.ForEach(subArea =>
            {
                subArea.mParkingPartitionPro = subArea.ConvertSubAreaToMParkingPartitionPro();
                subArea.mParkingPartitionPro2 = subArea.ConvertSubAreaToMParkingPartitionPro();
                subArea.mParkingPartitionPro3 = subArea.ConvertSubAreaToMParkingPartitionPro();
            });
            //
            var sw = new Stopwatch();
            sw.Start();
            if (InterParameter.MultiThread)
            {
                Parallel.ForEach(subAreas, new ParallelOptions { MaxDegreeOfParallelism = ThreadCnt }, subarea => subarea.UpdateParkingCnts(display, 1));
            }
            else
            {
                subAreas.ForEach(subarea => subarea.UpdateParkingCnts(display, 1));
            }
            sw.Stop();
            var time_1 = sw.ElapsedMilliseconds;
            //
            sw.Restart();
            if (InterParameter.MultiThread)
            {
                Parallel.ForEach(subAreas, new ParallelOptions { MaxDegreeOfParallelism = ThreadCnt }, subarea => subarea.UpdateParkingCnts(display, 2));
            }
            else
            {
                subAreas.ForEach(subarea => subarea.UpdateParkingCnts(display, 2));
            }
            sw.Stop();
            var time_2 = sw.ElapsedMilliseconds;
            //
            sw.Restart();
            if (InterParameter.MultiThread)
            {
                Parallel.ForEach(subAreas, new ParallelOptions { MaxDegreeOfParallelism = ThreadCnt }, subarea => subarea.UpdateParkingCnts(display, 3));
            }
            else
            {
                subAreas.ForEach(subarea => subarea.UpdateParkingCnts(display, 3));
            }
            sw.Stop();
            var time_3 = sw.ElapsedMilliseconds;
            //
            #region 估算对比
            var count_1 = 0;
            var count_2 = 0;
            var count_3 = 0;
            foreach (var subArea in subAreas)
            {
                count_1 += subArea.mParkingPartitionPro.Cars.Count;
                count_2 += subArea.mParkingPartitionPro2.EstimateCountOne;
                count_3 += subArea.mParkingPartitionPro3.EstimateCountTwo;
            }

            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            FileStream fs = new FileStream(dir + "\\estimate.txt", FileMode.Append, FileAccess.Write);
            StreamWriter swr = new StreamWriter(fs);
            swr.WriteLine("原始方法用时：" + time_1 + ",车位数: " + count_1);
            swr.WriteLine("估算方法一用时：" + time_2 + ",车位数: " + count_2);
            swr.WriteLine("估算方法二用时：" + time_3 + ",车位数: " + count_3 + "\r\n");
            swr.Close();
            fs.Close();

            #endregion
            if (display)
            {
                var walls = new List<LineString>();
                var cars = new List<InfoCar>();
                var pillars = new List<Polygon>();
                var iniPillars = new List<Polygon>();
                var obsVertices = new List<Coordinate>();
                var lanes = new List<LineSegment>();

                foreach (var subArea in subAreas)
                {
                    walls.AddRange(subArea.mParkingPartitionPro.Walls);
                    cars.AddRange(subArea.mParkingPartitionPro.Cars);
                    pillars.AddRange(/*subArea.mParkingPartitionPro.Pillars*/subArea.mParkingPartitionPro3.EstimateLaneBoxes.Select(e => e.Box).ToList());
                    iniPillars.AddRange(subArea.mParkingPartitionPro.IniPillar);
                    obsVertices.AddRange(subArea.mParkingPartitionPro.ObstacleVertexes);
                    lanes.AddRange(subArea.mParkingPartitionPro.IniLanes.Select(e => e.Line));
                }
                RemoveDuplicatedLines(lanes);
                MLayoutPostProcessing.GenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
                //MLayoutPostProcessing.PostProcessLanes(ref lanes, cars.Select(e => e.Polyline).ToList(), iniPillars, obsVertices);
                MLayoutPostProcessing.GenerateCarsOntheEndofLanesByFillTheEndDistrict(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
                MLayoutPostProcessing.CheckLayoutDirectionInfoBeforePostProcessEndLanes(ref cars);
                MLayoutPostProcessing.RemoveInvalidPillars(ref pillars, cars);
                mParkingPartition = new MParkingPartitionPro();
                mParkingPartition.Cars = cars;
                mParkingPartition.Pillars = pillars;
                //mParkingPartition.OutputLanes = lanes;

                mParkingPartition.OutputLanes = new List<LineSegment>();
                var ensuredLanes = new List<LineSegment>();
                var unsuredLanes = new List<LineSegment>();
                foreach (var subArea in subAreas)
                {
                    ensuredLanes.AddRange(subArea.mParkingPartitionPro.OutEnsuredLanes);
                    unsuredLanes.AddRange(subArea.mParkingPartitionPro.OutUnsuredLanes);
                }
                mParkingPartition.OutEnsuredLanes= ensuredLanes;
                mParkingPartition.OutUnsuredLanes= unsuredLanes;

                return cars.Count;
            }
            if (false)
            {
                var walls = new List<LineString>();
                var cars = new List<InfoCar>();
                var pillars = new List<Polygon>();
                var iniPillars = new List<Polygon>();
                var obsVertices = new List<Coordinate>();
                var lanes = new List<LineSegment>();
                foreach (var subArea in subAreas)
                {
                    walls.AddRange(subArea.mParkingPartitionPro.Walls);
                    cars.AddRange(subArea.mParkingPartitionPro.Cars);
                    pillars.AddRange(subArea.mParkingPartitionPro.Pillars);
                    iniPillars.AddRange(subArea.mParkingPartitionPro.IniPillar);
                    obsVertices.AddRange(subArea.mParkingPartitionPro.ObstacleVertexes);
                    lanes.AddRange(subArea.mParkingPartitionPro.IniLanes.Select(e => e.Line));
                }
                RemoveDuplicatedLines(lanes);
                MLayoutPostProcessing.GenerateCarsOntheEndofLanesByRemoveUnnecessaryLanes(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
                MLayoutPostProcessing.PostProcessLanes(ref lanes, cars.Select(e => e.Polyline).ToList(), iniPillars, obsVertices);
                MLayoutPostProcessing.GenerateCarsOntheEndofLanesByFillTheEndDistrict(ref cars, ref pillars, ref lanes, walls, ObstaclesSpacialIndex, Boundary);
                MLayoutPostProcessing.CheckLayoutDirectionInfoBeforePostProcessEndLanes(ref cars);
                MLayoutPostProcessing.RemoveInvalidPillars(ref pillars, cars);
                mParkingPartition = new MParkingPartitionPro();
                mParkingPartition.Cars = cars;
                mParkingPartition.Pillars = pillars;
                mParkingPartition.OutputLanes = lanes;
                return cars.Count;
            }
            else
            {
                return subAreas.Sum(sa => sa.Count);
            }
            //if (display)
            //    mParkingPartitionPros.AddRange(subAreas.Select(subarea => subarea.mParkingPartitionPro));
            //return subAreas.Sum(sa => sa.Count);
        }
        public static MParkingPartitionPro ConvertSubAreaToMParkingPartitionPro(this SubArea subArea)
        {
            var bound = new Polygon(subArea.Area.Shell);
            bound = bound.Simplify();
            var inilanes = new List<LineSegment>();
            foreach (var lane in subArea.VaildLanes)
            {
                inilanes.Add(SplitLine(lane, bound.Coordinates.ToList())
                    .Where(e => bound.ClosestPoint(e.MidPoint).Distance(e.MidPoint) < 1)
                    .OrderByDescending(e => e.Length).First());
            }
            var obs = subArea.Buildings;
            var box = subArea.BoundingBoxes;
            var points = new List<Coordinate>();
            inilanes.ForEach(e =>
            {
                points.Add(e.P0);
                points.Add(e.P1);
            });
            points = points.Select(p => bound.Coordinates.OrderBy(t => t.Distance(p)).First()).ToList();
            points=SortAlongCurve(points, bound);
            points = RemoveDuplicatePts(points);
            var linestring = new LineString(bound.Coordinates);
            //var walls = linestring.GetSplitCurves(points)
            //    .Where(e => e.Length > 1).ToList();
            var walls = subArea.Walls.Select(e => new LineString(e.Coordinates)).ToList();
            if (walls.Count > 0)
            {
                walls = walls.Where(e => ClosestPointInCurvesFast(e.GetMidPoint(), inilanes.Select(f => f.ToLineString()).ToList()) > 10)
                    .Select(e => new LineString(RemoveDuplicatePts(e.Coordinates.ToList()).ToArray())).ToList();
            }
            MParkingPartitionPro mParkingPartitionPro = new MParkingPartitionPro(
           walls, inilanes.Select(e => new LineSegment(e.P0,e.P1)).ToList(), obs.Select(e => e.Clone()).ToList(), bound.Clone());
            mParkingPartitionPro.OutBoundary = subArea.OutBound.Clone();
            mParkingPartitionPro.BuildingBoxes = box.Select(e => e.Clone()).ToList();
            //mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(obs);
            mParkingPartitionPro.ObstaclesSpatialIndex = InterParameter.BuildingSpatialIndex;
            mParkingPartitionPro.RampList = subArea.Ramps.Where(e => bound.Contains(e.InsertPt)).ToList();
            return mParkingPartitionPro;
        }
        public static bool IsValidatedSolutions(List<SubArea> subAreas)
        {
            var lanes = new List<LineSegment>();
            var boundary = subAreas[0].OutBound;
            for (int k = 0; k < subAreas.Count; k++)
            {
                lanes.AddRange(subAreas[k].VaildLanes);
            }
            var tmplanes = new List<LineSegment>();
            //与边界邻近的无效车道线剔除
            for (int i = 0; i < lanes.Count; i++)
            {
                var buffer = lanes[i].Buffer(2750 - 1);
                var splits = SplitCurve(boundary, buffer);
                if (splits.Count() == 1) continue;
                splits = splits.Where(e => buffer.Contains(e.GetMidPoint())).Where(e => e.Length > 1).ToArray();
                if (splits.Count() == 0) continue;
                var split = splits.First();
                var ps = lanes[i].ClosestPoint(split.StartPoint.Coordinate);
                var pe = lanes[i].ClosestPoint(split.EndPoint.Coordinate);
                var splitline = new LineSegment(ps, pe);
                var splitedlines = SplitLine(lanes[i], new List<Coordinate>() { ps, pe });
                splitedlines = splitedlines.Where(e => e.MidPoint.Distance(splitline.ClosestPoint(e.MidPoint)) > 1).ToList();
                lanes.RemoveAt(i);
                tmplanes.AddRange(splitedlines);
                i--;
            }
            lanes.AddRange(tmplanes);
            RemoveDuplicatedLines(lanes);
            //连接碎车道线
            int count = 0;
            while (true)
            {
                count++;
                if (count > 10) break;
                if (lanes.Count < 2) break;
                for (int i = 0; i < lanes.Count - 1; i++)
                {
                    var joined = false;
                    for (int j = i + 1; j < lanes.Count; j++)
                    {
                        if (IsParallelLine(lanes[i], lanes[j]) && (lanes[i].P0.Distance(lanes[j].P0) == 0
                            || lanes[i].P0.Distance(lanes[j].P1) == 0
                            || lanes[i].P1.Distance(lanes[j].P0) == 0
                            || lanes[i].P1.Distance(lanes[j].P1) == 0))
                        {
                            var pl = JoinCurves(new List<LineString>(), new List<LineSegment>() { lanes[i], lanes[j] }).First();
                            var line = new LineSegment(pl.StartPoint.Coordinate, pl.EndPoint.Coordinate);
                            if (Math.Abs(line.Length - lanes[i].Length - lanes[j].Length) < 1)
                            {
                                lanes.RemoveAt(j);
                                lanes.RemoveAt(i);
                                lanes.Add(line);
                                joined = true;
                                break;
                            }
                        }
                    }
                    if (joined) break;
                }
            }
            return true;
            ////判断是否有孤立的车道线
            //if (lanes.Count == 1) return true;
            //for (int i = 0; i < lanes.Count; i++)
            //{
            //    bool connected = false;
            //    for (int j = 0; j < lanes.Count; j++)
            //    {
            //        if (i != j)
            //        {
            //            if (IsConnectedLines(lanes[i], lanes[j]) || lanes[i].IntersectPoint(lanes[j]).Count() > 0)
            //            {
            //                connected = true;
            //                break;
            //            }
            //        }
            //    }
            //    if (!connected) return false;
            //}
            //return true;
        }

    }
}
