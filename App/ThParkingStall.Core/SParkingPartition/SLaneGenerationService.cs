using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.SParkingPartition.Sparam;

namespace ThParkingStall.Core.SParkingPartition
{
    public class SLaneGenerationService
    {
        public SLaneGenerationService(SPartition spartition)
        {
            SPartition = spartition;
        }
        private SPartition SPartition { get; set; }
        private Vector2D ParentDir = Vector2D.Zero;
        public void Process()
        {
            GeneratePrimaryLanes();
            GenerateMinorLanes();
        }
        void GeneratePrimaryLanes()
        {
            if (QuickCalculate)
                GeneratePrimaryLanesQuickly();
            else
                GeneratePrimaryLanesAccurately();
        }
        void GeneratePrimaryLanesAccurately()
        {

        }
        void GeneratePrimaryLanesQuickly()
        {
            SLane.SortLaneByDirection(SPartition.Lanes, LayoutMode, ParentDir);
        }
        void GenerateMinorLanes()
        {
            
        }
    }
}
