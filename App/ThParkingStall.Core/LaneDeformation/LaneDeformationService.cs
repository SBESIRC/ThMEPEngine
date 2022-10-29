using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

namespace ThParkingStall.Core.LaneDeformation
{
    public class LaneDeformationService
    {
        public LaneDeformationService(VehicleLaneData data)
        {
            Data = data;
        }
        private VehicleLaneData Data { get; }
        public VehicleLaneData Result { get; set; }
        public void Process()
        {
            //
            DataPreprocess dataPreprocess = new DataPreprocess(Data);
            dataPreprocess.Pipeline();

            //
            

            //
            

            Result = Data;
            return;
        }
    }
}
