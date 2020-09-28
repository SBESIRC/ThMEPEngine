﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThCADExtension;
using ThMEPEngineCore.Operation;

namespace ThMEPWSS.Bussiness.LayoutBussiness
{
    public class RayLayoutService
    {
        protected readonly double sideLength = 3400;
        protected readonly double sideMinLength = 0;
        protected readonly double maxLength = 1800;
        protected readonly double minLength = 100;
        protected readonly double raduisLength = 1800;
        protected readonly double moveLength = 200;
        protected readonly double spacing = 100;

        public List<SprayLayoutData> LayoutSpray(Polyline polyline, List<Polyline> colums)
        {
            //获取柱轴网
            GridService gridService = new GridService();
            var allGrids = gridService.CreateGrid(polyline, colums);

            //计算布置网格线
            CalLayoutGrid(polyline, allGrids, out List<List<Polyline>> tLines, out List<List<Polyline>> vLines, out Vector3d tDir, out Vector3d vDir);

            //计算喷淋布置点
            var sprays = SprayDataOperateService.CalSprayPoint(tLines, vLines, vDir, tDir, sideLength);

            //边界保护
            CheckProtectService checkProtectService = new CheckProtectService();
            checkProtectService.CheckBoundarySprays(polyline, sprays, sideLength);

            //躲次梁
            AvoidBeamService beamService = new AvoidBeamService();
            //beamService.AvoidBeam(polyline, sprays);

            //打印布置网格线
            InsertSprayLinesService.InsertSprayLines(SprayDataOperateService.CalAllSprayLines(sprays));

            return null;
        }

        /// <summary>
        /// 计算布置网格线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columnGrids"></param>
        /// <param name="tLines"></param>
        /// <param name="vLines"></param>
        private void CalLayoutGrid(Polyline polyline, List<KeyValuePair<Vector3d, List<Polyline>>> columnGrids, out List<List<Polyline>> tLines,
            out List<List<Polyline>> vLines, out Vector3d tDir, out Vector3d vDir)
        {
            Matrix3d layoutMatrix = new Matrix3d(new double[]{
                    columnGrids[0].Key.X, columnGrids[1].Key.X, Vector3d.ZAxis.X, 0,
                    columnGrids[0].Key.Y, columnGrids[1].Key.Y, Vector3d.ZAxis.Y, 0,
                    columnGrids[0].Key.Z, columnGrids[1].Key.Z, Vector3d.ZAxis.Z, 0,
                    0.0, 0.0, 0.0, 1.0});

            //计算可布置区域
            var range = CalLayoutRange(columnGrids[0].Value, columnGrids[1].Value, layoutMatrix);
            List<double> sLongValue = range[0];
            List<double> eLongValue = range[1];
            List<double> sShortValue = range[2];
            List<double> eShortValue = range[3];

            var p1 = new Point3d(sLongValue[0], sShortValue[0], 0);
            var p2 = new Point3d(eLongValue[0], sShortValue[0], 0);
            var p3 = new Point3d(sLongValue[0], eShortValue[0], 0);
            //计算排布方向
            tDir = (p2 - p1).GetNormal();   //横向方向
            vDir = -(p3 - p1).GetNormal();   //纵向方向 
            //计算排布长宽
            double length = columnGrids[1].Value[0].Length;      //横向长度
            double width = columnGrids[0].Value[0].Length;        //纵向长度

            List<Polyline> resVLines = new List<Polyline>();
            List<Polyline> resTLines = new List<Polyline>();
            //计算横向布置线
            for (int i = 0; i < sLongValue.Count; i++)
            {
                Point3d pt = new Point3d(sLongValue[i], eShortValue[0], 0);
                var resLines = LayoutPoints(pt, tDir, vDir, Math.Abs(sLongValue[i] - eLongValue[i]), width);
                resLines.ForEach(x => x.TransformBy(layoutMatrix));
                resTLines.AddRange(resLines);
            }
            //计算竖向布置线
            for (int i = 0; i < sShortValue.Count; i++)
            {
                Point3d pt = new Point3d(sLongValue[0], eShortValue[i], 0);
                var resLines = LayoutPoints(pt, vDir, tDir, Math.Abs(sShortValue[i] - eShortValue[i]), length);
                resLines.ForEach(x => x.TransformBy(layoutMatrix));
                resVLines.AddRange(resLines);
            }

            //校验喷淋布置线
            CheckLayoutLine(resTLines);
            CheckLayoutLine(resVLines);

            //调整线间距
            var sp = new Point3d(sLongValue[0], eShortValue[0], 0).TransformBy(layoutMatrix);
            AdjustmentLayoutLines(resTLines, sp);
            AdjustmentLayoutLines(resVLines, sp);

            tLines = resTLines.Select(x => polyline.Trim(x).Cast<Polyline>().ToList()).ToList();
            vLines = resVLines.Select(x => polyline.Trim(x).Cast<Polyline>().ToList()).ToList();
        }

        /// <summary>
        /// 校验防止喷淋布置太近
        /// </summary>
        /// <param name="polylines"></param>
        private void CheckLayoutLine(List<Polyline> polylines)
        {
            List<int> removeNum = new List<int>();
            for (int i = 1; i < polylines.Count - 1; i++)
            {
                var prevPoly = polylines[i - 1];
                var thisPoly = polylines[i];
                var nextPoly = polylines[i + 1];

                var closetPt = prevPoly.GetClosestPointTo(nextPoly.StartPoint, true);
                if (closetPt.DistanceTo(nextPoly.StartPoint) < sideLength)
                {
                    removeNum.Add(i);
                    i++;
                }
            }

            foreach (var num in removeNum)
            {
                polylines.RemoveAt(num);
            }
        }

        /// <summary>
        /// 调整布置线间距为100的倍数
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="sPt"></param>
        private void AdjustmentLayoutLines(List<Polyline> polylines, Point3d sPt)
        {
            for (int i = 0; i < polylines.Count; i++)
            {
                var closetPt = polylines[i].GetClosestPointTo(sPt, true);
                double length = closetPt.DistanceTo(sPt);
                double newLength = Math.Round(length / spacing) * spacing;
                if (length != newLength)
                {
                    double moveLength = length - newLength;
                    Vector3d moveDir = (sPt - closetPt).GetNormal();
                    var sp = polylines[i].GetPoint3dAt(0) + moveLength * moveDir;
                    var ep = polylines[i].GetPoint3dAt(polylines[i].NumberOfVertices - 1) + moveLength * moveDir;
                    Polyline polyline = new Polyline();
                    polyline.AddVertexAt(0, sp.ToPoint2D(), 0, 0, 0);
                    polyline.AddVertexAt(1, ep.ToPoint2D(), 0, 0, 0);
                    polylines[i] = polyline;
                    sPt = sp;
                }
                else
                {
                    sPt = closetPt;
                }
            }
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
        private List<Polyline> LayoutPoints(Point3d pt, Vector3d dir, Vector3d otherDir, double length, double width)
        {
            double remainder, num, moveLength;
            //竖向排布条件
            CalLayoutWay(length, out remainder, out num, out moveLength);

            List<Polyline> lines = new List<Polyline>();
            Point3d tSPt = pt + remainder * dir;
            for (int i = 0; i <= num; i++)
            {
                Point3d tEPt = tSPt + otherDir * width;
                Polyline polyline = new Polyline();
                polyline.AddVertexAt(0, tSPt.ToPoint2D(), 0, 0, 0);
                polyline.AddVertexAt(1, tEPt.ToPoint2D(), 0, 0, 0);
                lines.Add(polyline);
                tSPt = tSPt + dir * moveLength;
            }
            return lines;
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
