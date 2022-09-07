using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public partial class ObliqueMPartition
    {
        public void GenerateLanes()
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count > 20) break;

                SortLaneByDirection(IniLanes, LayoutMode,ParentDir);
                GenerateLaneParas paras_integral_modules = new GenerateLaneParas();
                GenerateLaneParas paras_adj_lanes = new GenerateLaneParas();
                GenerateLaneParas paras_between_two_builds = new GenerateLaneParas();
                GenerateLaneParas paras_single_vert_modules = new GenerateLaneParas();

                var length_integral_modules = ((int)GenerateIntegralModuleLanesOptimizedByRealLength(ref paras_integral_modules, true));
                var length_adj_lanes = ((int)GenerateAdjacentLanesOptimizedByRealLength(ref paras_adj_lanes));
                var length_between_two_builds = ((int)GenerateLaneBetweenTwoBuilds(ref paras_between_two_builds));
                var length_single_vert_modules = (int)GenerateLaneForLayoutingSingleVertModule(ref paras_single_vert_modules);
                var max = new int[] { length_integral_modules, length_adj_lanes, length_between_two_builds, length_single_vert_modules }.Max();
                if (max > 0)
                {
                    if (max == length_integral_modules)
                    {
                        RealizeGenerateLaneParas(paras_integral_modules);
                    }
                    else if (max == length_adj_lanes)
                    {
                        RealizeGenerateLaneParas(paras_adj_lanes);
                    }
                    else if (max == length_between_two_builds)
                    {
                        RealizeGenerateLaneParas(paras_between_two_builds);
                    }
                    else
                    {
                        RealizeGenerateLaneParas(paras_single_vert_modules);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void GenerateLanesSuperFast()
        {
            SortLaneByDirection(IniLanes, LayoutMode, ParentDir);
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var _paras_module = new GenerateLaneParas();
                var para_lanes_add = new List<LineSegment>();
                var length_module = GenerateIntegralModuleLanesForUniqueLaneOptimizedByRealLength(ref _paras_module, i, ref para_lanes_add, true);
                var _paras_adj = new GenerateLaneParas();
                var length_adj = GenerateAdjacentLanesForUniqueLaneOptimizedByRealLength(ref _paras_adj, i);
                if (Math.Max(length_module, length_adj) > 0)
                {
                    if (length_module >= length_adj)
                    {
                        RealizeGenerateLaneParas(_paras_module);
                    }
                    else
                    {
                        RealizeGenerateLaneParas(_paras_adj);
                    }
                }
            }
        }

        private void RealizeGenerateLaneParas(GenerateLaneParas paras)
        {
            if (paras.SetNotBeMoved != -1) IniLanes[paras.SetNotBeMoved].CanBeMoved = false;
            if (paras.SetGStartAdjLane != -1) IniLanes[paras.SetGStartAdjLane].GStartAdjLine = true;
            if (paras.SetGEndAdjLane != -1) IniLanes[paras.SetGEndAdjLane].GEndAdjLine = true;
            if (paras.LanesToAdd.Count > 0)
            {
                ParentDir = Vector(paras.LanesToAdd[0].Line).GetPerpendicularVector();
                IniLanes.AddRange(paras.LanesToAdd);
                foreach (var lane in paras.LanesToAdd)
                {
                    if (IsConnectedToLaneDouble(lane.Line,IniLanes)) IniLanes.Add(lane);
                    else
                    {
                        if (IsConnectedToLane(lane.Line, false,IniLanes))
                            lane.Line = new LineSegment(lane.Line.P1, lane.Line.P0);
                        //如果只生成一个modulebox，宽度是7850，车位是5300，如果在后续生成车道的过程中有可能碰车位，这时应该缩短车道，作特殊处理
                        var modified_lane = lane.Line;
                        foreach (var box in CarModules)
                        {
                            var cond_dis = box.Box.Contains(lane.Line.P1) || box.Box.ClosestPoint(lane.Line.P1).Distance(box.Box.ClosestPoint(lane.Line.P1)) < 1;
                            var cond_character = box.IsSingleModule;
                            var cond_perp = IsPerpLine(box.Line, modified_lane);
                            if (cond_dis && cond_character && cond_perp)
                            {
                                var end = lane.Line.P1.Translation(-Vector(lane.Line).Normalize() * (DisVertCarLength - DisVertCarLengthBackBack));
                                modified_lane = new LineSegment(lane.Line.P0, end);
                            }
                        }
                        lane.Line = modified_lane;
                        IniLanes.Add(lane);
                    }
                }
            }
            if (paras.CarBoxesToAdd.Count > 0)
            {
                CarBoxes.AddRange(paras.CarBoxesToAdd);
                CarBoxesSpatialIndex.Update(paras.CarBoxesToAdd.Cast<Geometry>().ToList(), new List<Geometry>());
            }
            if (paras.CarBoxPlusToAdd.Count > 0) CarBoxesPlus.AddRange(paras.CarBoxPlusToAdd);
            if (paras.CarModulesToAdd.Count > 0) CarModules.AddRange(paras.CarModulesToAdd);
        }
    }
}
