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
    public class FreeAreaRec:BlockNode
    {
        public Coordinate LeftUpPoint = new Coordinate(0, 0);
        public Coordinate LeftDownPoint = new Coordinate(0, 0);
        public Coordinate RightDownPoint = new Coordinate(0, 0);
        public Coordinate RightUpPoint = new Coordinate(0, 0);

        public Polygon Obb;
        public double FreeLength = 0;        

        public FreeAreaRec(Coordinate leftDown, Coordinate leftUp, Coordinate rightDown, Coordinate rightUp) 
        {
            LeftDownPoint = leftDown;
            LeftUpPoint = leftUp;
            RightDownPoint = rightDown;
            RightUpPoint = rightUp;
            FreeLength = new Vector2D(rightDown,rightUp).Length();

            List<Coordinate> pointList = new List<Coordinate>();
            pointList.Add(LeftDownPoint);
            pointList.Add(LeftUpPoint);
            pointList.Add(RightUpPoint);
            pointList.Add(LeftDownPoint);
            Obb = new Polygon(new LinearRing(pointList.ToArray()));
        }

        public void UpdataBase() 
        {

        }
    }
}
