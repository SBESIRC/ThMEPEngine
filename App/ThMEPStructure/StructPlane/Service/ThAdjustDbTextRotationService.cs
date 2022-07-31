using System;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThAdjustDbTextRotationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">按0度创建的文字</param>
        /// <param name="direction">文字方向</param>
        public static void Adjust(DBText text, Vector3d direction,double angTolerance=1.0)
        {
            text.Rotation = GetRotation(direction, angTolerance);
        }

        public static double GetRotation(Vector3d direction, double angTolerance = 1.0)
        {
            var ang = Vector3d.XAxis.GetAngleTo(direction).RadToAng();
            ang %= 180.0;
            if (Math.Abs(ang) <= angTolerance || Math.Abs(ang - 180.0) <= angTolerance)
            {
                // 水平
                return 0.0;
            }
            else if (Math.Abs(ang - 90.0) <= angTolerance)
            {
                // 水平
                return Math.PI / 2.0;
            }
            else if ((direction.X > 0 && direction.Y > 0.0) || (direction.X < 0 && direction.Y < 0.0))
            {
                // 1、3象限
                if(ang < 90) // 锐角
                {
                    return ang.AngToRad();
                }
                else
                {
                    return Math.PI - ang.AngToRad();
                }
            }
            else
            {
                //文字2、4象限
                return Math.PI * 2.0 - ang.AngToRad();
            }
        }
    }
}
