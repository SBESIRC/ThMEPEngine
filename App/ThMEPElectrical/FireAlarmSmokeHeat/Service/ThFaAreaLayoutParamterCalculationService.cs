using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPElectrical.FireAlarm;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Service
{
    class ThFaAreaLayoutParamterCalculationService
    {
        /// <summary>
        /// 计算半径
        /// </summary>
        /// <param name="roomArea"></param>
        /// <param name="hightInt"></param>
        /// <param name="thetaInt"></param>
        /// <param name="isSmokeSensor">true:smoke senser, false:heat senser</param>
        /// <returns></returns>
        public static double CalculateRadius(double roomArea, int hightInt, int thetaInt, ThFaSmokeCommon.layoutType layoutType)
        {
            double radius = 5800;
            var i = 0;
            var j = thetaInt;

            if (layoutType == ThFaSmokeCommon.layoutType.smoke)
            {
                //烟感
                if (roomArea <= 80 * 1000 * 1000)
                {
                    i = 0;
                }
                else
                {
                    i = hightInt;
                }
            }
            else if (layoutType == ThFaSmokeCommon.layoutType.heat)
            {
                //温感
                if (roomArea <= 30 * 1000 * 1000)
                {
                    i = 3;
                }
                else
                {
                    i = 4;
                }
            }

            if (i > 4 || i < 0)
            {
                //超出范围默认5800
                i = 2;

            }
            if (j > 2 || j < 0)
            {
                j = 0;
            }
            radius = ThFaSmokeCommon.protectRadius[i, j];

            return radius;
        }


        public static List<Polyline> GetPriorityBoundary(Dictionary<Point3d, Vector3d> layoutPts, double scale, (double, double) size)
        {
            var blkBoundary = new List<Polyline>();

            foreach (var blk in layoutPts)
            {
                var boundary = GetBoundary(blk.Key, blk.Value, scale, size);
                blkBoundary.Add(boundary);
            }

            return blkBoundary;
        }


        private static Polyline GetBoundary(Point3d pt, Vector3d dir, double scale, (double, double) size)
        {
            var xDir = dir.RotateBy(90 * Math.PI / 180, -Vector3d.ZAxis).GetNormal();
            var pt0 = pt + dir * (size.Item2 * scale / 2) - xDir * (size.Item1 * scale / 2);
            var pt1 = pt + dir * (size.Item2 * scale / 2) + xDir * (size.Item1 * scale / 2);
            var pt2 = pt - dir * (size.Item2 * scale / 2) + xDir * (size.Item1 * scale / 2);
            var pt3 = pt - dir * (size.Item2 * scale / 2) - xDir * (size.Item1 * scale / 2);

            var boundray = new Polyline();
            boundray.AddVertexAt(boundray.NumberOfVertices, pt0.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt1.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt2.ToPoint2D(), 0, 0, 0);
            boundray.AddVertexAt(boundray.NumberOfVertices, pt3.ToPoint2D(), 0, 0, 0);
            boundray.Closed = true;

            return boundray;
        }


        public static double GetPriorityExtendValue(List<string> blkNameList, double scale)
        {
            double extend = -1;
            var size = new List<double>();
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item1));
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item2));

            extend = size.OrderByDescending(x => x).First();
            extend = extend * scale / 2;
            return extend;
        }

    }
}
