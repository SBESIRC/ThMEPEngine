using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.ConnectWiring.Model
{
    public class UcsInfo
    {
        public Point3d ucsInsertPoint;
        public Matrix3d ucsMatrix;
        public double rotateAngle;
        public Vector3d BlockXAxis;
        public Matrix3d OriginMatrix = Matrix3d.Identity; // 插入块的ucs系统

        public UcsInfo(Point3d ucsPoint, Matrix3d matrix, double angle, Vector3d blockXAxis, Matrix3d originMatrix)
        {
            ucsInsertPoint = ucsPoint;
            ucsMatrix = matrix;
            rotateAngle = angle;
            BlockXAxis = blockXAxis;
            OriginMatrix = originMatrix;
        }
    }
}
