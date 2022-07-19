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
        public void GenerateCarsOnRestLanes()
        {
            CarSpatialIndex = new MNTSSpatialIndex(Cars.Select(e => e.Polyline));
            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            var vertlanes = GeneratePerpModuleLanes(DisVertCarLengthBackBack + DisLaneWidth / 2, DisVertCarWidth, false, null, true);
            SortLaneByDirection(vertlanes, LayoutMode);
            var align_backback_for_align_rest = false;
            for (int i = 0; i < vertlanes.Count; i++)
            {
                var k = vertlanes[i];
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = TranslateReservedConnection(line, k.Vec.Normalize() * DisLaneWidth / 2, false);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisVertCarWidth, DisVertCarLength
                    , ref line_align_backback_rest, true, false, false, false, true, true, true, align_backback_for_align_rest, false, true, false, true, false, true);
                align_backback_for_align_rest = false;
                if (line_align_backback_rest.Length > 0)
                {
                    vertlanes.Insert(i + 1, new Lane(line_align_backback_rest, k.Vec));
                    align_backback_for_align_rest = true;
                }
            }

            UpdateLaneBoxAndSpatialIndexForGenerateVertLanes();
            vertlanes = GeneratePerpModuleLanes(DisParallelCarWidth + DisLaneWidth / 2, DisParallelCarLength, false);
            SortLaneByDirection(vertlanes, LayoutMode);
            foreach (var k in vertlanes)
            {
                var vl = k.Line;
                UnifyLaneDirection(ref vl, IniLanes);
                var line = new LineSegment(vl);
                line = TranslateReservedConnection(line, k.Vec.Normalize() * DisLaneWidth / 2, false);
                var line_align_backback_rest = new LineSegment();
                GenerateCarsAndPillarsForEachLane(line, k.Vec, DisParallelCarLength, DisParallelCarWidth
                    , ref line_align_backback_rest, true, false, false, false, true, true, false);
            }
        }
        public void ProcessLanes(ref List<Lane> Lanes, bool preprocess = false)
        {

        }
        public void PostProcess()
        {
            RemoveDuplicateCars();
            RemoveCarsIntersectedWithBoundary();
            RemoveInvalidPillars();
            ReDefinePillarDimensions();
            InsuredForTheCaseOfoncaveBoundary();
        }
    }
}
