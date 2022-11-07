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
    public class FreeAreaRec
    {
        public Coordinate LeftUpPoint = new Coordinate(0, 0);
        public Coordinate LeftDownPoint = new Coordinate(0, 0);
        public Coordinate RightDownPoint = new Coordinate(0, 0);
        public Coordinate RightUpPoint = new Coordinate(0, 0);

        public Polygon Obb;
        public double FreeLength = 0;
        public double Width = 0;

        public FreeAreaRec(Coordinate leftDown,  Coordinate rightDown, Coordinate rightUp, Coordinate leftUp) 
        {
            LeftDownPoint = leftDown;
            LeftUpPoint = leftUp;
            RightDownPoint = rightDown;
            RightUpPoint = rightUp;
            FreeLength = new Vector2D(rightDown,rightUp).Length();
            Width = new Vector2D(leftDown,rightDown).Length();  

            List<Coordinate> pointList = new List<Coordinate>();
            pointList.Add(LeftDownPoint);
            pointList.Add(RightDownPoint);
            pointList.Add(RightUpPoint);
            pointList.Add(LeftUpPoint);
            pointList.Add(LeftDownPoint);
            Obb = new Polygon(new LinearRing(pointList.ToArray()));
        }
    }
}
