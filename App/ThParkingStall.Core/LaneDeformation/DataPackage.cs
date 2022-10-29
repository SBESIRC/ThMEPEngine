using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<List<FreeBlock>> FreeBlockList = new List<List<FreeBlock>>();
        public ProcessedData() { }


    }


    public class Parameter 
    {
    
    
    
    }
}
