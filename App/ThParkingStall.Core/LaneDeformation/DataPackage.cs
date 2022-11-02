using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;

namespace ThParkingStall.Core.LaneDeformation
{
    internal class DataPackage
    {
    }

    public class RawData
    {
        static public VehicleLaneData rawData = new VehicleLaneData();
    }

    public class ProcessedData
    {
        //临时做个示例
        static public List<List<FreeAreaRec>> FreeBlockList = new List<List<FreeAreaRec>>();
        public ProcessedData() { }


    }


    public class Parameter
    {



    }

    public class LDOutput
    {
        public static DrawTmpOutPut DrawTmpOutPut0 = new DrawTmpOutPut();

    }

}
