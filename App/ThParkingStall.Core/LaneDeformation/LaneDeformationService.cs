using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;
using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;

namespace ThParkingStall.Core.LaneDeformation
{
    public class LaneDeformationService
    {
        public LaneDeformationService(VehicleLaneData data)
        {
            Data = data;
            LDOutput.DrawTmpOutPut0 = new DrawTmpOutPut();
        }
        private VehicleLaneData Data { get; }
        public VehicleLaneData Result { get; set; }

        public DrawTmpOutPut DrawTmpOutPut0 = new DrawTmpOutPut();

        public void Process()
        {
            //
            DataPreprocess dataPreprocess = new DataPreprocess(Data);
            dataPreprocess.Pipeline();

            //


            //
            GetResult();
            return;
        }

        public void GetResult() 
        {
            DrawTmpOutPut0 = LDOutput.DrawTmpOutPut0;

            Result = Data;
        }
    }
}
