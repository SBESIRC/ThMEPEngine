using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThMEPWSS.Model;

namespace ThMEPWSS.Bussiness.LayoutBussiness
{
    public class LayoutService
    {
        protected double sideLength = 3400;
        protected double sideMinLength = 0;
        protected double maxLength = 1800;
        protected double minLength = 100;
        protected double raduisLength = 1800;
        protected double moveLength = 200;

        public List<List<SprayLayoutData>> LayoutSpray(Polyline polyline, List<Polyline> colums)
        {
            //获取柱轴网
            GridService gridService = new GridService();
            var allGrid = gridService.CreateGrid(polyline, colums);
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in allGrid)
                {
                    foreach (var sss in item.Value)
                    {
                        acdb.ModelSpace.Add(sss);
                    }
                }
            }

            Matrix3d layoutMatrix = new Matrix3d(new double[]{
                    allGrid[0].Key.X, allGrid[1].Key.X, Vector3d.ZAxis.X, 0,
                    allGrid[0].Key.Y, allGrid[1].Key.Y, Vector3d.ZAxis.Y, 0,
                    allGrid[0].Key.Z, allGrid[1].Key.Z, Vector3d.ZAxis.Z, 0,
                    0.0, 0.0, 0.0, 1.0});

            //计算可布置区域
            var range = CalLayoutRange(allGrid[0].Value, allGrid[1].Value, layoutMatrix);
            List<double> sLongValue = range[0];
            List<double> eLongValue = range[1];
            List<double> sShortValue = range[2];
            List<double> eShortValue = range[3];
            List<List<Point3d>> resPts = new List<List<Point3d>>();

            //计算排布方向
            var p1 = new Point3d(sLongValue[0], eShortValue[0], 0);
            var p2 = new Point3d(sLongValue[sLongValue.Count - 1], eShortValue[0], 0);
            var p3 = new Point3d(sLongValue[0], eShortValue[eShortValue.Count - 1], 0);
            Vector3d tDir = (p2 - p1).GetNormal();   //横向方向
            Vector3d vDir = (p3 - p1).GetNormal();   //纵向方向 

            //计算布置点
            for (int i = 0; i < sLongValue.Count; i++)
            {
                for (int j = 0; j < sShortValue.Count; j++)
                {
                    Point3d pt = new Point3d(sLongValue[i], eShortValue[j], 0);
                    resPts.AddRange(LayoutPoints(pt, tDir, vDir, Math.Abs(sLongValue[i] - eLongValue[i]), Math.Abs(eShortValue[j] - sShortValue[j])));
                }
            }
            var sresPts = resPts.Select(x => x.Select(y => y.TransformBy(layoutMatrix)).ToList()).ToList();
            return SprayDataService.CreateSprayModels(sresPts, tDir, vDir, sideLength); 
        }

        /// <summary>
        /// 计算排布区域
        /// </summary>
        /// <param name="longPoly"></param>
        /// <param name="shortPoly"></param>
        /// <param name="pt"></param>
        /// <param name="matrix"></param>
        private List<List<double>> CalLayoutRange(List<Polyline> longPoly, List<Polyline> shortPoly, Matrix3d matrix)
        {
            List<double> sLongValue = new List<double>();
            List<double> eLongValue = new List<double>();
            foreach (var lPoly in longPoly)
            {
                List<Point3d> polyPts = new List<Point3d>();
                for (int i = 0; i < lPoly.NumberOfVertices; i++)
                {
                    polyPts.Add(lPoly.GetPoint3dAt(i));
                }
                polyPts = polyPts.Select(x => x.TransformBy(matrix.Inverse())).ToList();
                double maxX = polyPts.Max(x => x.X);
                double minX = polyPts.Min(x => x.X);

                sLongValue.Add(minX);
                eLongValue.Add(maxX);
            }
            eLongValue.RemoveAt(0);
            sLongValue.RemoveAt(sLongValue.Count - 1);

            List<double> sShortValue = new List<double>();
            List<double> eShortValue = new List<double>();
            foreach (var lPoly in shortPoly)
            {
                List<Point3d> polyPts = new List<Point3d>();
                for (int i = 0; i < lPoly.NumberOfVertices; i++)
                {
                    polyPts.Add(lPoly.GetPoint3dAt(i));
                }
                polyPts = polyPts.Select(x => x.TransformBy(matrix.Inverse())).ToList();
                double maxY = polyPts.Max(x => x.Y);
                double minY = polyPts.Min(x => x.Y);

                sShortValue.Add(maxY);
                eShortValue.Add(minY);
            }
            sShortValue.RemoveAt(0);
            eShortValue.RemoveAt(eShortValue.Count - 1);

            return new List<List<double>>() { sLongValue, eLongValue, sShortValue, eShortValue };
        }

        /// <summary>
        /// 按正方形保护排布点
        /// </summary>
        /// <param name="roomLines"></param>
        /// <param name="pt"></param>
        /// <param name="transverseDir"></param>
        /// <param name="verticalDir"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private List<List<Point3d>> LayoutPoints(Point3d pt, Vector3d transverseDir, Vector3d verticalDir, double length, double width)
        {
            double tRemainder, tNum, tMoveLength, vRemainder, vNum, vMoveLength;
            //竖向排布条件
            CalLayoutWay(length, out tRemainder, out tNum, out tMoveLength);
            //横向排布条件
            CalLayoutWay(width, out vRemainder, out vNum, out vMoveLength);

            List<List<Point3d>> allP = new List<List<Point3d>>();
            pt = pt + tRemainder * transverseDir + vRemainder * verticalDir;
            for (int i = 0; i <= tNum; i++)
            {
                List<Point3d> p = new List<Point3d>();
                Point3d tsp = pt;
                for (int j = 0; j <= vNum; j++)
                {
                    p.Add(tsp);
                    tsp = tsp + vMoveLength * verticalDir;
                }
                allP.Add(p);
                pt = pt + tMoveLength * transverseDir;
            }
            return allP;
        }

        /// <summary>
        /// 计算排布规则(边界距离,步长等)
        /// </summary>
        /// <param name="length"></param>
        /// <param name="remainder"></param>
        /// <param name="num"></param>
        /// <param name="moveLength"></param>
        private void CalLayoutWay(double length, out double remainder, out double num, out double moveLength)
        {
            num = Math.Floor(length / sideLength);
            if (num >= 1)
            {
                moveLength = length / (num + 1);
                //间距是50的倍数
                moveLength = Math.Ceiling(moveLength / 50) * 50;
                remainder = (length - moveLength * num) / 2;
                if (remainder > raduisLength)
                {
                    while (true)
                    {
                        moveLength = (length - raduisLength * 2) / num;
                        //间距是50的倍数
                        moveLength = Math.Floor(moveLength / 50) * 50;
                        remainder = (length - moveLength * num) / 2;
                        if (remainder > raduisLength || moveLength > sideLength)
                        {
                            num += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                remainder = length / 2;
                moveLength = 0;
            }
        }
    }
}
