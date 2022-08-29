using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimReferencePoint
    {
        public int Type = -1; // 1 为相交， 2为不相交，3没找到
        public Point3d SprinklerDimPt = new Point3d(); // 喷淋标注点
        public Point3d ReferencePt = new Point3d(); // 参考点
        public long DimensionDistance = 0; // 标注方向距离
        public long VerticalDistance = 0; // 与标注方向垂直的距离

        public ThSprinklerDimReferencePoint(int type, Point3d sprinklerDimPt, Point3d referencePt,  double dimensionDistance, double verticalDistance)
        {
            Type = type;
            ReferencePt = referencePt;
            SprinklerDimPt = sprinklerDimPt;
            DimensionDistance = (long)dimensionDistance/10; //精确到10mm
            VerticalDistance = (long)verticalDistance/10;//精确到10mm
        }

        public ThSprinklerDimReferencePoint(int type)
        {
            Type = type;
        }


    }
}
