﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectTType : DrawObjectBase
    {
        ThTTypeEdgeComponent thTTypeEdgeComponent;
        /// <summary>
        /// 标记三个方向是否接墙
        /// </summary>
        private bool top = false, left = false, right = false;
        public override void DrawOutline()
        {
            double width = (thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Hc2l + thTTypeEdgeComponent.Bf) * scale;
            double height = (thTTypeEdgeComponent.Bw + thTTypeEdgeComponent.Hc1) * scale;
            Point3d startPt = TableStartPt + new Vector3d((FirstRowWidth - width) / 2 + thTTypeEdgeComponent.Hc2s * scale, -FirstRowHeight + height + 1500, 0);
            var pts = new Point3dCollection
            {
                startPt,
                startPt + new Vector3d(0, -thTTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(-thTTypeEdgeComponent.Hc2s, -thTTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(-thTTypeEdgeComponent.Hc2s, -thTTypeEdgeComponent.Hc1 - thTTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(0, -thTTypeEdgeComponent.Hc1 - thTTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thTTypeEdgeComponent.Bf, -thTTypeEdgeComponent.Hc1 - thTTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thTTypeEdgeComponent.Bf + thTTypeEdgeComponent.Hc2l, -thTTypeEdgeComponent.Hc1 - thTTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thTTypeEdgeComponent.Bf + thTTypeEdgeComponent.Hc2l, -thTTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(thTTypeEdgeComponent.Bf, -thTTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(thTTypeEdgeComponent.Bf, 0, 0) * scale
            };
            Outline = pts.CreatePolyline();
        }
        public override void DrawWall()
        {
            LinkedWallLines = new List<Curve>();
            //根据信息判断三个方向上分别是否需要接墙
            if (thTTypeEdgeComponent.Type == "A")
            {
                top = true;
                if (thTTypeEdgeComponent.LinkWallPos != "1")
                {
                    if (thTTypeEdgeComponent.LinkWallPos != "2L") left = true;
                    if (thTTypeEdgeComponent.LinkWallPos != "2S") right = true;
                }
            }
            else if (thTTypeEdgeComponent.Type == "B")
            {
                if (thTTypeEdgeComponent.LinkWallPos != "1L") left = true;
                if (thTTypeEdgeComponent.LinkWallPos != "1S") right = true;
            }
            //分别画出三个方向的墙线
            if (left)
            {
                Point3d pt1 = Outline.GetPoint3dAt(2), pt2 = Outline.GetPoint3dAt(3);
                double bw = thTTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(-bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (right)
            {
                Point3d pt1 = Outline.GetPoint3dAt(6), pt2 = Outline.GetPoint3dAt(7);
                double bw = thTTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (top)
            {
                Point3d pt1 = Outline.GetPoint3dAt(9), pt2 = Outline.GetPoint3dAt(0);
                double bf = thTTypeEdgeComponent.Bf * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(0, bf * 5 / 8.0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
        }
        public override void DrawDim()
        {
            rotatedDimensions = new List<RotatedDimension>();
            RotatedDimension rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(3),
                XLine2Point = Outline.GetPoint3dAt(4),
                DimLinePoint = Outline.GetPoint3dAt(3) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Hc2s.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(4),
                XLine2Point = Outline.GetPoint3dAt(5),
                DimLinePoint = Outline.GetPoint3dAt(4) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Bf.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(5),
                XLine2Point = Outline.GetPoint3dAt(6),
                DimLinePoint = Outline.GetPoint3dAt(5) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Hc2l.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            double bw = thTTypeEdgeComponent.Bw * 5 / 8.0 * scale;
            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(6),
                XLine2Point = Outline.GetPoint3dAt(7),
                DimLinePoint = Outline.GetPoint3dAt(6) + new Vector3d(600, 0, 0),
                DimensionText = thTTypeEdgeComponent.Bw.ToString(),
                Rotation = Math.PI / 2.0
            };
            if (right)
            {
                rotatedDimension.XLine1Point += new Vector3d(bw, 0, 0);
                rotatedDimension.XLine2Point += new Vector3d(bw, 0, 0);
                rotatedDimension.DimLinePoint += new Vector3d(bw, 0, 0);
            }
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(7),
                XLine2Point = Outline.GetPoint3dAt(7) + (Outline.GetPoint3dAt(9) - Outline.GetPoint3dAt(8)),
                DimLinePoint = Outline.GetPoint3dAt(6) + new Vector3d(600, 0, 0),
                DimensionText = thTTypeEdgeComponent.Hc1.ToString(),
                Rotation = Math.PI / 2.0
            };
            if (right)
            {
                rotatedDimension.XLine1Point += new Vector3d(bw, 0, 0);
                rotatedDimension.XLine2Point += new Vector3d(bw, 0, 0);
                rotatedDimension.DimLinePoint += new Vector3d(bw, 0, 0);
            }
            rotatedDimensions.Add(rotatedDimension);
        }
        protected override void CalStirrupPosition()
        {
            GangJinStirrup stirrup = new GangJinStirrup
            {
                Outline = Outline,
                scale = scale,
                GangjinType = 1
            };
            stirrup.CalPositionT(thTTypeEdgeComponent);
            foreach (var polyline in stirrup.stirrups)
            {
                Links.Add(polyline);
                LinksFlag.Add(1);
            }
        }


        /// <summary>
        /// T型钢筋确定位置，轮廓上10个点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        protected override void CalReinforcePosition(int pointNum, Polyline polyline)
        {
            //存储结果
            points = new List<Point3d>();
            pointsFlag = new List<int>();
            //纵筋相对轮廓的偏移值
            double offset = scale * (thTTypeEdgeComponent.C + 5) + thTTypeEdgeComponent.PointReinforceLineWeight + thTTypeEdgeComponent.StirrupLineWeight;
            //根据八个轮廓上的点的位置计算纵筋位置
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i == 0 || i == 1 || i == 2)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }

                //左下角，向右上偏移
                else if (i == 3 || i == 4)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if (i == 5 || i == 6)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if (i >= 7 && i <= 9)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y - offset, 0);
                }
                else
                {
                    break;
                }
                points.Add(tmpPoint);
                pointsFlag.Add(1);
            }

            //底层需要添加额外的纵筋，2，3两点中点，6，7中点
            if (thTTypeEdgeComponent.Bw >= 300)
            {
                Point3d tmpPoint = new Point3d((points[2].X + points[3].X) / 2.0, (points[2].Y + points[3].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[6].X + points[7].X) / 2.0, (points[6].Y + points[7].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                pointsFlag.Add(4);
                pointsFlag.Add(4);
            }

            //竖向需要添加额外的钢筋,0,9中点，4，5中点
            if (thTTypeEdgeComponent.Bf >= 300)
            {
                Point3d tmpPoint = new Point3d((points[0].X + points[9].X) / 2.0, (points[0].Y + points[9].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[4].X + points[5].X) / 2.0, (points[4].Y + points[5].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                pointsFlag.Add(4);
                pointsFlag.Add(4);
            }

            int needLayoutPointsNum = pointNum - points.Count;
            //需要排布的点的对数
            int pointsPair = needLayoutPointsNum / 2;

            //遍历所有排布可能性，选取方差最小的,竖直方向有一个区域，水平方向有两个区域
            //此处需要三重循环，先从竖直方向0个布点开始计算，再是左侧水平方向，最后是右侧水平方向
            List<double> numbers = new List<double>();
            //记录在bw方向上有几对点
            int resultY = 0;
            int resultX1 = 0;
            double minVar = 100000000;

            //计算竖直方向非核心区两个纵筋的距离
            double disY = points[0].Y - points[1].Y;
            //水平方向非核心区的纵筋距离，左侧1，右侧2
            double disX1 = points[4].X - points[3].X;
            double disX2 = points[6].X - points[5].X;
            for (int i = 0; i <= pointsPair; i++)
            {
                numbers.Clear();
                for (int w = 0; w <= pointsPair - i; w++)
                {
                    //竖直方向有i对点，水平方向左侧有w对点，水平方向右侧有pointsPair-i-w对点
                    for (int j = 0; j <= i; j++)
                    {
                        numbers.Add(disY / (double)(i + 1));
                    }
                    for (int l = 0; l <= w; l++)
                    {
                        numbers.Add(disX1 / (double)(w + 1));
                    }
                    for (int k = 0; k <= pointsPair - i - w; k++)
                    {
                        numbers.Add(disX2 / (pointsPair - i - w + 1));
                    }
                    double tmp = Helper.CalVariance(numbers);
                    if (tmp < minVar)
                    {
                        resultY = i;
                        resultX1 = w;
                        minVar = tmp;
                    }

                }
            }
            //每次新增的间隔距离
            double deltaY = disY / (double)(resultY + 1);
            double deltaX1 = disX1 / (double)(resultX1 + 1);
            double deltaX2 = disX2 / (double)(pointsPair - resultX1 - resultY + 1);

            //把一对对点的位置计算出来，计算竖直方向上的位置,0，9号点每次向下偏移deltaY,水平方向2，3号点每次向右偏移deltaX1,  5,8号点向右侧偏移deltaX2
            for (int i = 0; i < resultY; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[9].X, points[9].Y - deltaY, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }
            //间距大的先安排拉筋
            if (deltaX1 > deltaX2)
            {
                for (int i = 0; i < resultX1; i++)
                {
                    Point3d tmpPoint1 = new Point3d(points[2].X + deltaX1, points[2].Y, 0);
                    points.Add(tmpPoint1);
                    Point3d tmpPoint2 = new Point3d(points[3].X + deltaX1, points[3].Y, 0);
                    points.Add(tmpPoint2);
                    pointsFlag.Add(3);
                    pointsFlag.Add(3);
                }
                for (int i = 0; i < pointsPair - resultX1 - resultY; i++)
                {
                    Point3d tmpPoint1 = new Point3d(points[5].X + deltaX2, points[5].Y, 0);
                    points.Add(tmpPoint1);
                    Point3d tmpPoint2 = new Point3d(points[8].X + deltaX2, points[8].Y, 0);
                    points.Add(tmpPoint2);
                    pointsFlag.Add(3);
                    pointsFlag.Add(3);
                }
            }
            else
            {
                for (int i = 0; i < pointsPair - resultX1 - resultY; i++)
                {
                    Point3d tmpPoint1 = new Point3d(points[5].X + deltaX2, points[5].Y, 0);
                    points.Add(tmpPoint1);
                    Point3d tmpPoint2 = new Point3d(points[8].X + deltaX2, points[8].Y, 0);
                    points.Add(tmpPoint2);
                    pointsFlag.Add(3);
                    pointsFlag.Add(3);
                }
                for (int i = 0; i < resultX1; i++)
                {
                    Point3d tmpPoint1 = new Point3d(points[2].X + deltaX1, points[2].Y, 0);
                    points.Add(tmpPoint1);
                    Point3d tmpPoint2 = new Point3d(points[3].X + deltaX1, points[3].Y, 0);
                    points.Add(tmpPoint2);
                    pointsFlag.Add(3);
                    pointsFlag.Add(3);
                }
            }

        }

        protected void T_FindCJin(List<Point3d> points, StrToReinforce strToRein, List<ZongjinPoint> ZongjinPoint_list)
        {
            if (strToRein.Rein_Detail_list.Count >= 1)
            {

                int dim = strToRein.Rein_Detail_list[0].TypeDist;//原始C筋的直径
                int num = strToRein.Rein_Detail_list[0].TypeNum;//原始C筋的数量
                for (int i = 0; i < points.Count; i++)
                {
                    //先将所有点装进ZongjinPoint_list
                    ZongjinPoint tmpZ = new ZongjinPoint();
                    tmpZ.position = points[i];
                    ZongjinPoint_list.Add(tmpZ);
                }
                List<int> CIndexList = new List<int>();
                if (thTTypeEdgeComponent.Bw < 300 && thTTypeEdgeComponent.Bf < 300)
                {
                    if (thTTypeEdgeComponent.Type == "A")
                    {
                        if (num == 4)
                        {
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx2 = Helper.FindMidPoint(points, 3, 4);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            int idx3 = Helper.FindMidPoint(points, 5, 6);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx4 = Helper.FindMidPoint(points, 7, 8);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(8);
                        }
                        else if (num == 14)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx2 = Helper.FindMidPoint(points, 3, 4);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            int idx3 = Helper.FindMidPoint(points, 5, 6);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx4 = Helper.FindMidPoint(points, 7, 8);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                    }
                    else if (thTTypeEdgeComponent.Type == "B")
                    {
                        if (num == 4)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(9);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindMidPoint(points, 0, 1);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(8);
                            int idx2 = Helper.FindMidPoint(points, 8, 9);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(9);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindMidPoint(points, 0, 1);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            int idx2 = Helper.FindMidPoint(points, 8, 9);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(9);
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                    }
                }
                else if (thTTypeEdgeComponent.Bf >= 300 && thTTypeEdgeComponent.Bf >= 300)
                {
                    if (thTTypeEdgeComponent.Type == "A")
                    {
                        if (num == 4)
                        {
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(2);
                            int idx1 = Helper.FindMidPoint(points, 2, 3);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            int idx2 = Helper.FindMidPoint(points, 6, 7);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(7);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(2);
                            int idx1 = Helper.FindMidPoint(points, 2, 3);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            int idx2 = Helper.FindMidPoint(points, 6, 7);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(7);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx1 = Helper.FindMidPoint(points, 2, 3);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            int idx2 = Helper.FindMidPoint(points, 6, 7);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx1 = Helper.FindMidPoint(points, 2, 3);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            int idx2 = Helper.FindMidPoint(points, 6, 7);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        else if (num == 14)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(3);
                            int idx3 = Helper.FindMidPoint(points, 3, 4);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            int idx4 = Helper.FindMidPoint(points, 5, 6);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(6);
                            int idx5 = Helper.FindMidPoint(points, 6, 7);
                            if (idx5 != -1)
                            {
                                CIndexList.Add(idx5);
                            }
                            CIndexList.Add(7);
                            int idx6 = Helper.FindMidPoint(points, 7, 8);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
                            CIndexList.Add(8);
                        }
                        else if (num == 16)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(3);
                            int idx3 = Helper.FindMidPoint(points, 3, 4);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            int idx4 = Helper.FindMidPoint(points, 5, 6);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(6);
                            int idx5 = Helper.FindMidPoint(points, 6, 7);
                            if (idx5 != -1)
                            {
                                CIndexList.Add(idx5);
                            }
                            CIndexList.Add(7);
                            int idx6 = Helper.FindMidPoint(points, 7, 8);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                    }
                    else if (thTTypeEdgeComponent.Type == "B")
                    {
                        if (num == 4)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(9);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(4);
                            int idx1 = Helper.FindMidPoint(points, 4, 5);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(9);
                            int idx2 = Helper.FindMidPoint(points, 9, 0);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(4);
                            int idx1 = Helper.FindMidPoint(points, 4, 5);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                            int idx2 = Helper.FindMidPoint(points, 9, 0);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindMidPoint(points, 0, 1);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(4);
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(8);
                            int idx3 = Helper.FindMidPoint(points, 8, 9);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(9);
                            int idx4 = Helper.FindMidPoint(points, 9, 0);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            int idx1 = Helper.FindMidPoint(points, 4, 5);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);                       
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            CIndexList.Add(9);
                            int idx2 = Helper.FindMidPoint(points, 9, 0);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                        }
                        else if (num == 14)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindMidPoint(points, 0, 1);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(8);
                            int idx3 = Helper.FindMidPoint(points, 8, 9);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(9);
                            int idx4 = Helper.FindMidPoint(points, 9, 0);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                    }
                }

                if (thTTypeEdgeComponent.IsCalculation == true)
                {
                    //先判断是第几次迭代
                    //这里的Reinforce默认有两种以上的纵筋规格
                    StrToReinforce CalStrToRein = new StrToReinforce();
                    CalStrToRein = Helper.StrToRein(thTTypeEdgeComponent.EnhancedReinforce);
                    int Step = 0;//迭代步数 
                    int FirstNum = CalStrToRein.Rein_Detail_list[0].TypeNum;
                    int FirstDim = CalStrToRein.Rein_Detail_list[0].TypeDist;
                    if (CalStrToRein.num == strToRein.num)
                    {
                        //迭代1-4

                        if (FirstNum == 2 && FirstDim - dim == 2)
                        {
                            Step = 1;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 2)
                        {
                            Step = 2;
                        }
                        else if (FirstNum == 2 && FirstDim - dim == 4)
                        {
                            Step = 3;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 4)
                        {
                            Step = 4;
                        }


                    }
                    else
                    {
                        if (CalStrToRein.num - strToRein.num == 2)
                        {
                            Step = 5;
                        }
                        else if (CalStrToRein.num - strToRein.num == 4)
                        {
                            Step = 6;
                        }
                    }


                    //分A,B型
                    //根据迭代步数，确定修改的C筋规格
                    //A型
                    if (thTTypeEdgeComponent.Type == "A")
                    {
                        if (Step == 1)
                        {
                            //L型：A型迭代1
                            ZongjinPoint_list[6].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 2)
                        {
                            //迭代2

                            ZongjinPoint_list[6].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;


                        }
                        else if (Step == 3)
                        {
                            //迭代3
                            ZongjinPoint_list[6].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                        }
                        else if (Step == 4)
                        {
                            //迭代4

                            ZongjinPoint_list[6].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 5)
                        {
                            //迭代5
                            //在4,5的位置增加一根纵筋

                        }
                        else if (Step == 6)
                        {
                            //迭代6
                            //在1,2,4,5的位置增加一根纵筋

                        }
                    }
                    else if (thTTypeEdgeComponent.Type == "B")
                    {
                        if (Step == 1)
                        {
                            //L型：A型迭代1
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[9].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 2)
                        {
                            //迭代2
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[9].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                        }
                        else if (Step == 3)
                        {
                            //迭代3
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[9].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                        }
                        else if (Step == 4)
                        {
                            //迭代4
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[9].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 5)
                        {
                            //迭代5
                            //在4,5的位置增加一根纵筋

                        }
                        else if (Step == 6)
                        {
                            //迭代6
                            //在1,2,4,5的位置增加一根纵筋

                        }
                    }






                }





            }
        }

        public override void DrawCJin()
        {
            StrToReinforce TReinStr = new StrToReinforce();
            TReinStr = Helper.StrToRein(thTTypeEdgeComponent.Reinforce);
            List<ZongjinPoint> ZongjinPoints = new List<ZongjinPoint>();
            T_FindCJin(points, TReinStr, ZongjinPoints);
            for (int i = 0; i < ZongjinPoints.Count; i++)
            {
                if (ZongjinPoints[i].hasUse == true)
                {
                    Point3d pos = ZongjinPoints[i].position;
                    var pts = new Point3dCollection {

                        pos+new Vector3d(-150,150,0),
                        pos+new Vector3d(150,150,0),
                        pos+new Vector3d(150,-150,0),
                        pos+new Vector3d(-150,-150,0)
                    };
                    Polyline rect = new Polyline();
                    rect = pts.CreatePolyline();
                    LabelAndRect.Add(rect);
                    Polyline label = new Polyline();
                    label.AddVertexAt(0, new Point2d((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2), 0, 0, 0);
                    label.AddVertexAt(1, new Point2d((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2 + 100), 0, 0, 0);
                    label.AddVertexAt(2, new Point2d((pts[0].X + pts[1].X) / 2 - 500, (pts[0].Y + pts[1].Y) / 2 + 100), 0, 0, 0);
                    LabelAndRect.Add(label);
                    DBText txt = new DBText();
                    txt.TextString = "1C" + ZongjinPoints[i].size;
                    txt.Height = 150;
                    txt.Position = label.GetPoint3dAt(2) + new Vector3d(0, 50, 0);
                    CJintText.Add(txt);
                }
            }
        }

        protected override void CalLinkPosition()
        {
            List<Point3d> tmpPoints = new List<Point3d>();
            //需要解析link3有几个,需要优先选择间隔大的
            LinkDetail linkDetail = Helper.StrToLinkDetail(thTTypeEdgeComponent.Link3);
            int cnt = 0;
            //遍历所有点，找出2，3，4类型的钢筋，钢筋,同时查表
            for (int i = 0; i < points.Count; i += 2)
            {
                if (pointsFlag[i] == 3)
                {
                    if (cnt >= linkDetail.num)
                    {
                        continue;
                    }
                    else
                    {
                        cnt++;
                    }
                }
                //只有一种情况直接绘制
                else if (pointsFlag[i] == 2)
                {
                    if (thTTypeEdgeComponent.Link2.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else if (pointsFlag[i] == 4)
                {
                    if (thTTypeEdgeComponent.Link4.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else continue;
                double r = thTTypeEdgeComponent.PointReinforceLineWeight + thTTypeEdgeComponent.StirrupLineWeight / 2;
                Polyline link = GangJinLink.DrawLink(points[i], points[i + 1], r, thTTypeEdgeComponent.StirrupLineWeight, scale);
                Links.Add(link);
                LinksFlag.Add(pointsFlag[i]);
            }
        }

        public override void init(ThEdgeComponent component, string elevation, double tblRowHeight, double scale, Point3d position)
        {
            this.thTTypeEdgeComponent = component as ThTTypeEdgeComponent;
            this.elevation = elevation;
            this.tblRowHeight = tblRowHeight;
            this.scale = scale;
            this.number = thTTypeEdgeComponent.Number;
            TableStartPt = position;
            if (thTTypeEdgeComponent.IsCalculation)
            {
                this.Reinforce = thTTypeEdgeComponent.EnhancedReinforce;
            }
            else
            {
                this.Reinforce = thTTypeEdgeComponent.Reinforce;
            }
            this.Stirrup = thTTypeEdgeComponent.Stirrup;
        }

        public override void CalExplo()
        {
            Vector2d vec = new Vector2d(0, (Outline.GetPoint2dAt(0) - Outline.GetPoint2dAt(3)).Y + 1200);
            Point2d centrePt = Outline.GetPoint2dAt(4) + (Outline.GetPoint2dAt(8) - Outline.GetPoint2dAt(4)) / 2;
            bool flag = false;
            //生成的拉筋：link1,link2,link3,竖向link4,横向link4
            List<List<Polyline>> plinks = new List<List<Polyline>>
            {
                new List<Polyline>(),
                new List<Polyline>(),
                new List<Polyline>(),
                new List<Polyline>(),
                new List<Polyline>()
            };
            //生成拉筋线
            for (int i = 0; i < Links.Count; i++)
            {
                Polyline tmp = new Polyline();
                if (LinksFlag[i] == 1)
                {
                    if (flag)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 0), centrePt);
                    }
                    else
                    {
                        flag = true;
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 200), centrePt);
                    }
                    plinks[0].Add(tmp);
                }
                else if (LinksFlag[i] == 2)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 200), centrePt);
                    plinks[1].Add(tmp);
                }
                else if (LinksFlag[i] == 3)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 200), centrePt);
                    plinks[2].Add(tmp);
                }
                else if (LinksFlag[i] == 4)
                {
                    if (Math.Abs((Links[i].GetPoint2dAt(1) - Links[i].GetPoint2dAt(0)).X) < 1)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 300), centrePt);
                        plinks[3].Add(tmp);
                    }
                    else
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(300, 0), centrePt);
                        plinks[4].Add(tmp);
                    }
                }
                tmp.Layer = "LINK";
                objectCollection.Add(tmp);
            }
            //绘制必要的标注
            if (!thTTypeEdgeComponent.Link2.IsNullOrEmpty() &&
                thTTypeEdgeComponent.Link2.Substring(1) != thTTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[1].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 1, i));
                }
                DrawLinkLabel(tmpList, true, thTTypeEdgeComponent.Link2, 200, -2000);
            }
            if (!thTTypeEdgeComponent.Link3.IsNullOrEmpty() &&
                thTTypeEdgeComponent.Link3.Substring(1) != thTTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[2].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 2, i));
                }
                DrawLinkLabel(tmpList, false, thTTypeEdgeComponent.Link3, 200, 2000);
            }
            if (!thTTypeEdgeComponent.Link4.IsNullOrEmpty() &&
                thTTypeEdgeComponent.Link4.Substring(1) != thTTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[3].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 3, i));
                }
                DrawLinkLabel(tmpList, false, thTTypeEdgeComponent.Link4, 400, 2000);
                tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[4].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 4, i));
                }
                DrawLinkLabel(tmpList, true, thTTypeEdgeComponent.Link4, -600, 2000);
            }
        }
    }
}
