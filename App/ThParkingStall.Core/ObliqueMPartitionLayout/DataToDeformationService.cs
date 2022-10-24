using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.LaneDeformation;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public class DataToDeformationService
    {
        public DataToDeformationService(List<OSubArea> subAreas)
        {
            SubAreas = subAreas;
            InitLaneDeformationParas();
        }
        List<OSubArea> SubAreas { get; set; }
        public List<ParkingPlaceBlock> GetParkingPlaceBlocks()
        {
            var result = new List<ParkingPlaceBlock>();
            return result;
        }
        void InitLaneDeformationParas()
        {
            LaneDeformationParas.VehicleLaneWidth = ObliqueMPartition.DisLaneWidth / 2;
            LaneDeformationParas.Boundary = SubAreas[0].OutBound;
            LaneDeformationParas.Blocks = SubAreas[0].Buildings;
        }
    }
}
