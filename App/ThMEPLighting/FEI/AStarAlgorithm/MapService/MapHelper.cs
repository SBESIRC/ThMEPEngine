using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.AStarAlgorithm.AStarModel;

namespace ThMEPLighting.FEI.AStarAlgorithm.MapService
{
    public abstract class MapHelper
    {
        public double step = 800; //步长
        public Matrix3d moveMatrix;   //位移矩阵
        public Matrix3d ucsMatrix;    //坐标系转换矩阵

        public MapHelper() : this(800)
        { }

        public MapHelper(double _step)
        {
            step = _step;
        }

        public abstract Point SetStartAndEndInfo(Point3d _startPt, EndModel endInfo);

        public abstract List<int> SetMapServiceInfo(Point3d transSP, EndModel endInfo);

        public abstract Point3d TransformMapPoint(Point point);
    }
}
