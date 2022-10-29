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
    public class FreeArea
    {
        //
        public Polygon FreeAreaObb;
        public Point LeftUpPoint = new Point(0, 0);
        public Point LeftRightPoint = new Point(0,0);
        public double FreeLength = 0;
        

        public FreeArea() 
        {
            


        
        }
    }
}
