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
    public class DataPreprocess
    {

        public DataPreprocess(VehicleLaneData data) 
        {
            RawData.rawData = data;

        }
        public void Pipeline()
        {
            //Polygon b;
            //var a = b.Difference(b);
            //LineString c;
            //c.Buffer();
            
            

        }
    }

   
}
