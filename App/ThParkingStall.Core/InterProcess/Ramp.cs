using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public class Ramp
    {
        //插入点
        public Point InsertPt { get; set; }
        //坡道的面域
        public Polygon Area { get; set; }
        public Ramp(Point insertPt, Polygon area)
        {
            InsertPt = insertPt;
            Area = area;
        }
    }
}
