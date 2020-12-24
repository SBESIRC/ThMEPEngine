using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using ThMEPElectrical.EmgLight.Service;
using Linq2Acad;
using ThCADCore.NTS;


namespace ThMEPElectrical.EmgLight
{
    class LayoutWithParkingLineForLight
    {
        //readonly double protectRange = 27000;
        //readonly double oneProtect = 21000;

        readonly int TolLane = 6000;
        readonly int TolLengthSide = 400;
        readonly int TolUniformSideColumnDistVariance = 5000;
        readonly double TolUniformSideLenth = 0.6;
        readonly int TolAvgColumnDist = 7900;
        readonly int TolLightRangeMin = 4000;
        readonly int TolLightRengeMax = 8500;


        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="mainLines"></param>
        /// <param name="otherLines"></param>
        /// <param name="roomPoly"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> LayoutLight(Polyline frame, List<List<Line>> mainLines, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> layoutInfo = new Dictionary<List<Line>, Dictionary<Point3d, Vector3d>>();
            List<Polyline> Layout = new List<Polyline>();

            foreach (var l in mainLines)
            {
                var lines = l.Select(x => x.Normalize()).ToList();

                ThMEPElectrical.Broadcast.ParkingLinesService parkingLinesService = new ThMEPElectrical.Broadcast.ParkingLinesService();
                var handleLines = parkingLinesService.HandleParkingLines(lines, out Point3d sPt, out Point3d ePt);

                //找到构建上可布置面,用第一条车道线的头尾判定,可能有bug
                CheckService checkService = new CheckService();
                var filterColmuns = checkService.FilterColumns(columns, lines.First(), frame);
                var filterWalls = checkService.FilterWalls(walls, lines, frame, TolLane);
                //InsertLightService.ShowGeometry(filterColmuns, 30, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(filterWalls, 210, LineWeight.LineWeight035);

                ////获取该车道线上的构建
                StructureServiceLight structureService = new StructureServiceLight();
                var lineColumn = structureService.GetStruct(lines, filterColmuns, TolLane);
                var lineWall = structureService.GetStruct(lines, filterWalls, TolLane);
                //InsertLightService.ShowGeometry(lineColumn, 142, LineWeight.LineWeight035);
                //InsertLightService.ShowGeometry(lineWall, 11, LineWeight.LineWeight035);

                ////将构建分为上下部分
                var usefulColumns = structureService.SeparateColumnsByLine(lineColumn, lines, TolLane);
                var usefulWalls = structureService.SeparateColumnsByLine(lineWall, lines, TolLane);

                ////for debug
                InsertLightService.ShowGeometry(usefulColumns[0], 142, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulColumns[1], 11, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulWalls[0], 142, LineWeight.LineWeight035);
                InsertLightService.ShowGeometry(usefulWalls[1], 11, LineWeight.LineWeight035);

                ////找出平均的一边. -1:no side 0:left 1:right.
                List<List<double>> distList;
                int nUniformSide = FindUniformDistributionSide(ref usefulColumns, lines, out distList);

                if (nUniformSide == 0 || nUniformSide == 1)
                {
                    LayoutUniformSide(usefulColumns[nUniformSide], lines, distList[nUniformSide], ref Layout);
                }
                else
                {
                    // LayoutBothNonUniformSide();
                }




            }

            return layoutInfo;
        }

        /// <summary>
        /// 找均匀一边
        /// </summary>
        /// <param name="usefulColumns">沿车道线排序后的</param>
        /// <param name="lines">车道线</param>
        /// <param name="distList">车道线方向坐标系里面的距离差</param>
        /// <returns>-1:两边都不均匀,0:车道线方向左侧均匀,1:右侧均匀</returns>
        private int FindUniformDistributionSide(ref List<List<Polyline>> usefulColumns, List<Line> lines, out List<List<double>> distList)
        {
            //上下排序
            usefulColumns[0] = OrderingColumns(usefulColumns[0], lines);
            usefulColumns[1] = OrderingColumns(usefulColumns[1], lines);

            distList = new List<List<double>>();
            distList.Add(GetColumnDistList(usefulColumns[0]));
            distList.Add(GetColumnDistList(usefulColumns[1]));

            double lineLength = 0;
            lines.ForEach(l => lineLength += l.Length);
            bool bLeft = true;
            bool bRight = true;
            double nVarianceLeft = -1;
            double nVarianceRight = -1;
            int nUniformSide = -1; //-1:no side, 0:left, 1:right

            //长度线
            if ((bLeft == true) && distList[0].Sum() / lineLength < TolUniformSideLenth)
            {
                bLeft = false;
            }

            if ((bRight == true) && distList[1].Sum() / lineLength < TolUniformSideLenth)
            {
                bRight = false;
            }

            //柱数量 > ((车道/平均柱距) * 0.5)
            if ((bLeft == true) && (3 < distList[0].Count() && distList[0].Count() < (lineLength / TolAvgColumnDist) * 0.5))
            {
                bLeft = false;
            }

            if ((bRight == true) && (3 < distList[1].Count() && distList[1].Count() < (lineLength / TolAvgColumnDist) * 0.5))
            {
                bRight = false;
            }

            //方差
            if (bLeft == true)
            {
                nVarianceLeft = GetVariance(distList[0]);
            }

            if (bRight == true)
            {
                nVarianceRight = GetVariance(distList[1]);
            }

            if (nVarianceLeft >= 0 && nVarianceLeft <= nVarianceRight)
            {
                nUniformSide = 0;

            }
            else if (nVarianceRight >= 0 && nVarianceRight <= nVarianceLeft)
            {
                nUniformSide = 1;
            }


            return nUniformSide;
        }

        private List<double> GetColumnDistList(List<Polyline> usefulOrderedColumns)
        {
            List<double> distX = new List<double>();
            for (int i = 0; i < usefulOrderedColumns.Count - 1; i++)
            {
                distX.Add((StructUtils.GetStructCenter(usefulOrderedColumns[i]) - StructUtils.GetStructCenter(usefulOrderedColumns[i + 1])).Length);
            }

            return distX;

        }
        private double GetVariance(List<double> distX)
        {

            double avg = 0;
            double variance = 0;

            avg = distX.Sum() / distX.Count;

            for (int i = 0; i < distX.Count - 1; i++)
            {
                variance += Math.Pow(distX[i] - avg, 2);
            }

            variance = Math.Sqrt(variance / distX.Count);


            return variance;

        }

        private List<Polyline> OrderingColumns(List<Polyline> Columns, List<Line> Lines)
        {
            Vector3d xDir = (Lines.First().EndPoint - Lines.First().StartPoint).GetNormal();
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
                });

            var orderColumns = Columns.OrderBy(x => StructUtils.GetStructCenter(x).TransformBy(matrix).X).ToList();

            return orderColumns;
        }

        /// <summary>
        /// using dist between columns directly, may has bug if the angle of line in two columns with lines is too large
        /// </summary>
        /// <param name="Columns"></param>
        /// <param name="Lines"></param>
        /// <param name="distList"></param>
        /// <param name="Layout"></param>
        private void LayoutUniformSide(List<Polyline> Columns, List<Line> Lines, List<double> distList, ref List<Polyline> Layout)
        {
            Polyline FirstLayout = checkIfHasFirstLight(Layout, Lines);
            if (FirstLayout != null)
            {
                ////车线起始点已有布灯,第一个点顺延

            }

            ////车线起始点没有布灯,从均匀侧布灯
            int LastHasNoLightColumn = 0;
            Layout.Add(Columns[0]);
            double sum = 0;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (i < Columns.Count - 1)
                {
                    sum += distList[i];
                    if (sum > TolLightRengeMax)
                    {
                        if (LastHasNoLightColumn != 0)
                        {
                            Layout.Add(Columns[i]);
                            LastHasNoLightColumn = 0;
                        }
                        else
                        {
                            LastHasNoLightColumn = i;
                        }
                        sum = distList[i];
                    }

                    if (distList[i] > TolLightRengeMax)
                    {
                        if (i < Columns.Count - 2)
                        {
                            if (LastHasNoLightColumn != 0)
                            {
                                Layout.Add(Columns[i]);
                                Layout.Add(Columns[i + 1]);
                                LastHasNoLightColumn = 0;
                                sum = 0;
                            }
                            else
                            {
                                Layout.Add(Columns[i + 1]);
                                LastHasNoLightColumn = 0;
                                sum = 0;
                            }

                        }
                    }
                }


                Layout = Layout.Distinct().ToList();
            }
        }



        private Polyline checkIfHasFirstLight(List<Polyline> Layout, List<Line> Lines)
        {
            Polyline first = null;
            if (Layout.Count > 0)
            {

            }
            return first;
        }

        private List<Polyline> everyOtherColumns(List<Polyline> Columns)
        {
            var everyOtherColumns = Columns.Where(x => (Columns.IndexOf(x) % 2) == 0).ToList();

            return everyOtherColumns;
        }



    }
}
