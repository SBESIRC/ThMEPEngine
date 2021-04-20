using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.MapService
{
    public abstract class MapHelper<T>
    {
        public double step = 800; //步长
        public Matrix3d moveMatrix;   //位移矩阵
        public Matrix3d ucsMatrix;    //坐标系转换矩阵
        public AStarEntity endEntity;

        public MapHelper() : this(800)
        { }

        public MapHelper(double _step)
        {
            step = _step;
        }

        public abstract Point SetStartAndEndInfo(Point3d _startPt, T endInfo);

        public abstract List<int> SetMapServiceInfo(Point3d transSP, T endInfo);

        public abstract Point3d TransformMapPoint(Point point);
    }
}
