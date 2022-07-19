using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    public class ZongjinPoint
    {
        public Point3d position;//纵筋位置
        public int size;//纵筋直径
        public bool hasUse;//是否使用过
        public bool hasCheck;//非C筋会使用
    }

    class DrawObjectLType : DrawObjectBase
    {

        ThLTypeEdgeComponent thLTypeEdgeComponent;
        /// <summary>
        /// 标记两个方向是否接墙
        /// </summary>
        private bool top = false, right = false;
        public override void DrawOutline()
        {
            double width = (thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2) * scale;
            double height = (thLTypeEdgeComponent.Bw + thLTypeEdgeComponent.Hc1) * scale;
            Point3d startPt = TableStartPt + new Vector3d((FirstRowWidth - width) / 2, -FirstRowHeight + height + 1500, 0);
            var pts = new Point3dCollection
            {
                startPt,
                startPt + new Vector3d(0, -thLTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(0, -thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thLTypeEdgeComponent.Bf, -thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                startPt + new Vector3d(thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -thLTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(thLTypeEdgeComponent.Bf, -thLTypeEdgeComponent.Hc1, 0) * scale,
                startPt + new Vector3d(thLTypeEdgeComponent.Bf, 0, 0) * scale
            };
            Outline = pts.CreatePolyline();
        }

        public override void DrawWall()
        {
            LinkedWallLines = new List<Curve>();
            if (thLTypeEdgeComponent.Type == "A")
            {
                top = true;
                if (thLTypeEdgeComponent.LinkWallPos == "2") right = true;
            }
            else if (thLTypeEdgeComponent.Type == "B") right = true;

            if (top)
            {
                Point3d pt1 = Outline.GetPoint3dAt(0), pt2 = Outline.GetPoint3dAt(7);
                double bf = thLTypeEdgeComponent.Bf * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(0, bf * 5 / 8.0, 0), out Line line1, out Line line2);

                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (right)
            {
                Point3d pt1 = Outline.GetPoint3dAt(4), pt2 = Outline.GetPoint3dAt(5);
                double bw = thLTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);

                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
        }
        public override void DrawDim()
        {
            rotatedDimensions = new List<RotatedDimension>();
            double bw = thLTypeEdgeComponent.Bw * 5 / 8.0 * scale;
            RotatedDimension rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(2),
                XLine2Point = Outline.GetPoint3dAt(3),
                DimLinePoint = Outline.GetPoint3dAt(2) + new Vector3d(0, -800, 0),
                DimensionText = thLTypeEdgeComponent.Bf.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(3),
                XLine2Point = Outline.GetPoint3dAt(4),
                DimLinePoint = Outline.GetPoint3dAt(3) + new Vector3d(0, -800, 0),
                DimensionText = thLTypeEdgeComponent.Hc2.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(4),
                XLine2Point = Outline.GetPoint3dAt(5),
                DimLinePoint = Outline.GetPoint3dAt(4) + new Vector3d(800, 0, 0),
                DimensionText = thLTypeEdgeComponent.Bw.ToString(),
                Rotation = Math.PI / 2
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
                XLine1Point = Outline.GetPoint3dAt(5),
                XLine2Point = Outline.GetPoint3dAt(5) + (Outline.GetPoint3dAt(7) - Outline.GetPoint3dAt(6)),
                DimLinePoint = Outline.GetPoint3dAt(4) + new Vector3d(800, 0, 0),
                DimensionText = thLTypeEdgeComponent.Hc1.ToString(),
                Rotation = Math.PI / 2
            };
            if (right)
            {
                rotatedDimension.XLine1Point += new Vector3d(bw, 0, 0);
                rotatedDimension.XLine2Point += new Vector3d(bw, 0, 0);
                rotatedDimension.DimLinePoint += new Vector3d(bw, 0, 0);
            }
            rotatedDimensions.Add(rotatedDimension);
        }

        /// <summary>
        /// L型钢筋确定位置，轮廓上八个点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        protected override void CalReinforcePosition(int pointNum, Polyline polyline)
        {
            points = new List<Point3d>();
            pointsFlag = new List<int>();
            //纵筋相对轮廓的偏移值
            double offset = scale * (thLTypeEdgeComponent.C ) + thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight;
            //根据八个轮廓上的点的位置计算纵筋位置
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i == 0 || i == 1)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }

                //左下角，向右上偏移
                else if (i == 2)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if (i == 3 || i == 4)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if (i >= 5 && i <= 7)
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

            //底层需要添加额外的纵筋，1，2两点中点，4，5中点
            if (thLTypeEdgeComponent.Bw >= 300)
            {
                Point3d tmpPoint = new Point3d((points[1].X + points[2].X) / 2.0, (points[1].Y + points[2].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[4].X + points[5].X) / 2.0, (points[4].Y + points[5].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                pointsFlag.Add(4);
                pointsFlag.Add(4);
            }

            //左侧需要添加额外的钢筋,0,7中点，2，3中点
            if (thLTypeEdgeComponent.Bf >= 300)
            {
                Point3d tmpPoint = new Point3d((points[0].X + points[7].X) / 2.0, (points[0].Y + points[7].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[2].X + points[3].X) / 2.0, (points[2].Y + points[3].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                pointsFlag.Add(4);
                pointsFlag.Add(4);
            }

            int needLayoutPointsNum = pointNum - points.Count;
            //需要排布的点的对数
            int pointsPair = needLayoutPointsNum / 2;
            //遍历所有排布可能性，选取方差最小的,bw是竖直方向上的，bf是水平方向，先从竖直方向0个布点开始计算
            List<double> numbers = new List<double>();
            //记录在bw方向上有几对点
            int result = 0;
            double minVar = 100000000;

            //计算竖直方向最远两个纵筋的距离
            double disY = points[0].Y - points[1].Y;
            //水平方向最远纵筋距离
            double disX = points[4].X - points[3].X;
            for (int i = 0; i <= pointsPair; i++)
            {
                numbers.Clear();
                for (int j = 0; j <= i; j++)
                {
                    numbers.Add(disY / (double)(i + 1));
                }
                for (int k = 0; k <= pointsPair - i; k++)
                {
                    numbers.Add(disX / (double)(pointsPair - i + 1));
                }
                double tmp = Helper.CalVariance(numbers);
                if (tmp < minVar)
                {
                    result = i;
                    minVar = tmp;
                }
            }
            //每次新增的间隔距离
            double deltaY = disY / (double)(result + 1);
            double deltaX = disX / (double)(pointsPair - result + 1);
            //把一对对点的位置计算出来，计算竖直方向上的位置,0，7号点每次向下偏移deltaY,水平方向3，6号点每次向右偏移deltaX
            for (int i = 0; i < result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY * (i + 1), 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[7].X, points[7].Y - deltaY * (i + 1), 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[3].X + deltaX * (i + 1), points[3].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[6].X + deltaX * (i + 1), points[6].Y, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(3);
                pointsFlag.Add(3);
            }
        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }

        protected void L_FindCJin(List<Point3d> points, StrToReinforce strToRein, List<ZongjinPoint> ZongjinPoint_list)
        {
            //从解析出字符串的strToRein里将C筋信息找出来，再将其每个纵筋的信息标记到ZongjinPoint_list里
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

                //不是CAL型
                if (thLTypeEdgeComponent.Bw < 300 && thLTypeEdgeComponent.Bf < 300)
                {
                    if (thLTypeEdgeComponent.Type == "A")
                    {
                        //bw<300,bf<300,A型
                        if (num == 2)
                        {
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 4)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 5)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);

                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                        }
                        else if (num == 7)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(7);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx != -1)
                            {
                                CIndexList.Add(idx);
                                CIndexList.Add(Helper.FindPairPoint(points, idx, 2));
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx != -1)
                            {
                                CIndexList.Add(idx);
                                CIndexList.Add(Helper.FindPairPoint(points, idx, 2));
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(0);
                            CIndexList.Add(7);
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx != -1)
                            {
                                CIndexList.Add(idx);
                                CIndexList.Add(Helper.FindPairPoint(points, idx, 2));
                            }
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(0);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                                CIndexList.Add(Helper.FindPairPoint(points, idx1, 1));
                            }
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                        if (num > 12)
                        {
                            int other = num - 12;
                            //将剩余的C筋分配，这种情况为墙出现更多的纵筋                         
                            int rightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int topNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            int topIdx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (other > 0)
                            {
                                if (rightNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newRightIdx = Helper.FindClosePoint(points, 6, rightIdx, rightIdx);
                                        ZongjinPoint_list[newRightIdx].hasUse = true;
                                        ZongjinPoint_list[newRightIdx].size = dim;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].hasUse = true;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].size = dim;
                                        other = other - 2;
                                        rightNum--;
                                        rightIdx = newRightIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (topNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newTopIdx = Helper.FindClosePoint(points, 1, topIdx, topIdx);
                                        int newTopPair = Helper.FindPairPoint(points, newTopIdx, 1);
                                        ZongjinPoint_list[newTopIdx].hasUse = true;
                                        ZongjinPoint_list[newTopIdx].size = dim;
                                        ZongjinPoint_list[newTopPair].hasUse = true;
                                        ZongjinPoint_list[newTopPair].size = dim;
                                        topNum--;
                                        other = other - 2;
                                        topIdx = newTopIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }




                        }


                    }
                    else
                    {
                        //B型
                        if (num == 2)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(7);
                        }
                        else if (num == 4)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                        }
                        else if (num == 5)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                            CIndexList.Add(1);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                        }
                        else if (num == 7)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                                CIndexList.Add(Helper.FindPairPoint(points, idx1, 1));
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                                CIndexList.Add(Helper.FindPairPoint(points, idx1, 1));
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(0);
                            int idx1 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                                CIndexList.Add(Helper.FindPairPoint(points, idx1, 1));
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            int idx2 = Helper.FindClosePoint(points, 5, 6, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                                CIndexList.Add(Helper.FindPairPoint(points, idx2, 1));
                            }
                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }
                        if (num > 12)
                        {
                            int other = num - 12;
                            //将剩余的C筋分配，这种情况为墙出现更多的纵筋                         
                            int rightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int topNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            int topIdx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (other > 0)
                            {
                                if (topNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newTopIdx = Helper.FindClosePoint(points, 1, topIdx, topIdx);
                                        int newTopPair = Helper.FindPairPoint(points, newTopIdx, 1);
                                        ZongjinPoint_list[newTopIdx].hasUse = true;
                                        ZongjinPoint_list[newTopIdx].size = dim;
                                        ZongjinPoint_list[newTopPair].hasUse = true;
                                        ZongjinPoint_list[newTopPair].size = dim;
                                        topNum--;
                                        other = other - 2;
                                        topIdx = newTopIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (rightNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newRightIdx = Helper.FindClosePoint(points, 6, rightIdx, rightIdx);
                                        ZongjinPoint_list[newRightIdx].hasUse = true;
                                        ZongjinPoint_list[newRightIdx].size = dim;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].hasUse = true;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].size = dim;
                                        other = other - 2;
                                        rightNum--;
                                        rightIdx = newRightIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                            }
                        }

                    }


                }
                else if (thLTypeEdgeComponent.Bf >= 300 && thLTypeEdgeComponent.Bw >= 300)
                {
                    if (thLTypeEdgeComponent.Type == "A")
                    {
                        //bw>=300,bf>=300
                        if (num == 2)
                        {
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 4)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                        }
                        else if (num == 5)
                        {
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            CIndexList.Add(4);
                            int idx1 = Helper.FindMidPoint(points, 4, 5);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(5);
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            CIndexList.Add(4);
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(5);

                        }
                        else if (num == 7)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(5);
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
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
                        }
                        else if (num == 9)
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
                            CIndexList.Add(4);
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(7);
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(1);
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            int idx2 = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                                CIndexList.Add(Helper.FindPairPoint(points, idx2, 2));
                            }
                            CIndexList.Add(4);
                            int idx3 = Helper.FindMidPoint(points, 4, 5);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);

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
                            int idx2 = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                                CIndexList.Add(Helper.FindPairPoint(points, idx2, 2));
                            }
                            CIndexList.Add(4);
                            int idx3 = Helper.FindMidPoint(points, 4, 5);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(0);
                            CIndexList.Add(7);

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
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);
                            }
                            CIndexList.Add(3);
                            int idx3 = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                                CIndexList.Add(Helper.FindPairPoint(points, idx3, 2));
                            }
                            CIndexList.Add(4);
                            int idx4 = Helper.FindMidPoint(points, 4, 5);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx6 = Helper.FindMidPoint(points, 7, 0);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
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
                            int idx3 = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                                CIndexList.Add(Helper.FindPairPoint(points, idx3, 2));
                            }
                            CIndexList.Add(4);
                            int idx4 = Helper.FindMidPoint(points, 4, 5);
                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx6 = Helper.FindMidPoint(points, 7, 0);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
                            int idx7 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx7 != -1)
                            {
                                CIndexList.Add(idx7);
                                CIndexList.Add(Helper.FindPairPoint(points, idx7, 1));
                            }
                        }

                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }

                        if (num > 16)
                        {
                            int other = num - 16;
                            //将剩余的C筋分配，这种情况为墙出现更多的纵筋                         
                            int rightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int topNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            int topIdx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (other > 0)
                            {
                                if (rightNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newRightIdx = Helper.FindClosePoint(points, 6, rightIdx, rightIdx);
                                        ZongjinPoint_list[newRightIdx].hasUse = true;
                                        ZongjinPoint_list[newRightIdx].size = dim;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].hasUse = true;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].size = dim;
                                        other = other - 2;
                                        rightNum--;
                                        rightIdx = newRightIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (topNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newTopIdx = Helper.FindClosePoint(points, 1, topIdx, topIdx);
                                        int newTopPair = Helper.FindPairPoint(points, newTopIdx, 1);
                                        ZongjinPoint_list[newTopIdx].hasUse = true;
                                        ZongjinPoint_list[newTopIdx].size = dim;
                                        ZongjinPoint_list[newTopPair].hasUse = true;
                                        ZongjinPoint_list[newTopPair].size = dim;
                                        topNum--;
                                        other = other - 2;
                                        topIdx = newTopIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        //B型，bw，bf>=300
                        if (num == 2)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(7);
                        }
                        else if (num == 4)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                        }
                        else if (num == 5)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(2);
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 6)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 7)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 8)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 9)
                        {
                            CIndexList.Add(0);
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 10)
                        {
                            CIndexList.Add(0);
                            int idx3 = Helper.FindMidPoint(points, 0, 1);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(6);
                            int idx4 = Helper.FindMidPoint(points, 6, 7);

                            if (idx4 != -1)
                            {
                                CIndexList.Add(idx4);
                            }

                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 12)
                        {
                            CIndexList.Add(0);
                            int idx3 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                                CIndexList.Add(Helper.FindPairPoint(points, idx3, 1));
                            }
                            CIndexList.Add(1);
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            CIndexList.Add(5);
                            CIndexList.Add(6);
                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 14)
                        {
                            CIndexList.Add(0);
                            int idx3 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                                CIndexList.Add(Helper.FindPairPoint(points, idx3, 1));
                            }
                            CIndexList.Add(1);
                            int idx5 = Helper.FindMidPoint(points, 1, 2);
                            if (idx5 != -1)
                            {
                                CIndexList.Add(idx5);
                            }
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            int idx6 = Helper.FindMidPoint(points, 4, 5);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);


                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }
                        }
                        else if (num == 16)
                        {
                            CIndexList.Add(0);
                            int idx3 = Helper.FindClosePoint(points, 0, 1, 0);
                            if (idx3 != -1)
                            {
                                CIndexList.Add(idx3);
                                CIndexList.Add(Helper.FindPairPoint(points, idx3, 1));
                            }
                            CIndexList.Add(1);
                            int idx5 = Helper.FindMidPoint(points, 1, 2);
                            if (idx5 != -1)
                            {
                                CIndexList.Add(idx5);
                            }
                            CIndexList.Add(2);
                            int idx2 = Helper.FindMidPoint(points, 2, 3);
                            if (idx2 != -1)
                            {
                                CIndexList.Add(idx2);

                            }
                            CIndexList.Add(3);
                            CIndexList.Add(4);
                            int idx6 = Helper.FindMidPoint(points, 4, 5);
                            if (idx6 != -1)
                            {
                                CIndexList.Add(idx6);
                            }
                            CIndexList.Add(5);
                            CIndexList.Add(6);


                            CIndexList.Add(7);
                            int idx1 = Helper.FindMidPoint(points, 0, 7);
                            if (idx1 != -1)
                            {
                                CIndexList.Add(idx1);
                            }

                            int idx7 = Helper.FindClosePoint(points, 3, 4, 4);
                            if (idx7 != -1)
                            {
                                CIndexList.Add(idx7);
                                CIndexList.Add(Helper.FindPairPoint(points, idx7, 2));
                            }

                        }
                        for (int i = 0; i < CIndexList.Count; i++)
                        {
                            ZongjinPoint_list[CIndexList[i]].hasUse = true;
                            ZongjinPoint_list[CIndexList[i]].size = dim;
                        }

                        if (num > 16)
                        {
                            int other = num - 16;
                            //将剩余的C筋分配，这种情况为墙出现更多的纵筋                         
                            int rightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int topNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            int topIdx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (other > 0)
                            {
                                if (topNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newTopIdx = Helper.FindClosePoint(points, 1, topIdx, topIdx);
                                        int newTopPair = Helper.FindPairPoint(points, newTopIdx, 1);
                                        ZongjinPoint_list[newTopIdx].hasUse = true;
                                        ZongjinPoint_list[newTopIdx].size = dim;
                                        ZongjinPoint_list[newTopPair].hasUse = true;
                                        ZongjinPoint_list[newTopPair].size = dim;
                                        topNum--;
                                        other = other - 2;
                                        topIdx = newTopIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (rightNum > 1)
                                {
                                    if (other > 0)
                                    {
                                        int newRightIdx = Helper.FindClosePoint(points, 6, rightIdx, rightIdx);
                                        ZongjinPoint_list[newRightIdx].hasUse = true;
                                        ZongjinPoint_list[newRightIdx].size = dim;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].hasUse = true;
                                        ZongjinPoint_list[Helper.FindPairPoint(points, newRightIdx, 2)].size = dim;
                                        other = other - 2;
                                        rightNum--;
                                        rightIdx = newRightIdx;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                            }
                        }

                    }


                }


                if (thLTypeEdgeComponent.IsCalculation == true && !thLTypeEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
                {
                    //先判断是第几次迭代
                    //这里的Reinforce默认有两种以上的纵筋规格
                    StrToReinforce CalStrToRein = new StrToReinforce();
                    CalStrToRein = Helper.StrToRein(thLTypeEdgeComponent.EnhancedReinforce);
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
                    if (thLTypeEdgeComponent.Type == "A")
                    {
                        if (Step == 1)
                        {
                            //L型：A型迭代1
                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].hasUse = true;
                            ZongjinPoint_list[5].hasUse = true;
                        }
                        else if (Step == 2)
                        {
                            //迭代2

                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].hasUse = true;
                            ZongjinPoint_list[5].hasUse = true;

                            ZongjinPoint_list[1].hasUse = true;
                            ZongjinPoint_list[2].hasUse = true;
                        }
                        else if (Step == 3)
                        {
                            //迭代3
                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[1].TypeDist;

                            ZongjinPoint_list[4].hasUse = true;
                            ZongjinPoint_list[5].hasUse = true;

                            ZongjinPoint_list[1].hasUse = true;
                            ZongjinPoint_list[2].hasUse = true;
                        }
                        else if (Step == 4)
                        {
                            //迭代4

                            ZongjinPoint_list[4].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[5].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[4].hasUse = true;
                            ZongjinPoint_list[5].hasUse = true;

                            ZongjinPoint_list[1].hasUse = true;
                            ZongjinPoint_list[2].hasUse = true;
                        }
                        else if (Step == 5)
                        {
                            //迭代5
                            //在4,5的位置增加一根纵筋
                            Point3d pt1 = new Point3d(ZongjinPoint_list[4].position.X - 100, ZongjinPoint_list[5].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[5].position.X - 100, ZongjinPoint_list[5].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);
                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);

                        }
                        else if (Step == 6)
                        {
                            //迭代6
                            //在1,2,4,5的位置增加一根纵筋
                            Point3d pt1 = new Point3d(ZongjinPoint_list[4].position.X - 100, ZongjinPoint_list[4].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[5].position.X - 100, ZongjinPoint_list[5].position.Y, 0);

                            Point3d pt3 = new Point3d(ZongjinPoint_list[1].position.X + 100, ZongjinPoint_list[1].position.Y, 0);
                            Point3d pt4 = new Point3d(ZongjinPoint_list[2].position.X + 100, ZongjinPoint_list[2].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);
                            points.Add(pt3);
                            pointsFlag.Add(1);
                            points.Add(pt4);
                            pointsFlag.Add(1);

                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint p3 = new ZongjinPoint();
                            p3.position = pt3;
                            p3.hasUse = true;
                            ZongjinPoint p4 = new ZongjinPoint();
                            p4.position = pt4;
                            p4.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);
                            ZongjinPoint_list.Add(p3);
                            ZongjinPoint_list.Add(p4);

                        }
                    }
                    else if (thLTypeEdgeComponent.Type == "B")
                    {
                        if (Step == 1)
                        {
                            //L型：A型迭代1
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 2)
                        {
                            //迭代2
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                        }
                        else if (Step == 3)
                        {
                            //迭代3
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                        }
                        else if (Step == 4)
                        {
                            //迭代4
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[7].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        }
                        else if (Step == 5)
                        {
                            //迭代5
                            //在4,5的位置增加一根纵筋
                            Point3d pt1 = new Point3d(ZongjinPoint_list[0].position.X + 100, ZongjinPoint_list[0].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[7].position.X - 100, ZongjinPoint_list[7].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);
                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);

                        }
                        else if (Step == 6)
                        {
                            //迭代6
                            //在1,2,4,5的位置增加一根纵筋
                            Point3d pt1 = new Point3d(ZongjinPoint_list[0].position.X + 100, ZongjinPoint_list[0].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[7].position.X - 100, ZongjinPoint_list[7].position.Y, 0);

                            Point3d pt3 = new Point3d(ZongjinPoint_list[2].position.X + 100, ZongjinPoint_list[2].position.Y, 0);
                            Point3d pt4 = new Point3d(ZongjinPoint_list[3].position.X - 100, ZongjinPoint_list[3].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);
                            points.Add(pt3);
                            pointsFlag.Add(1);
                            points.Add(pt4);
                            pointsFlag.Add(1);

                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint p3 = new ZongjinPoint();
                            p3.position = pt3;
                            p3.hasUse = true;
                            ZongjinPoint p4 = new ZongjinPoint();
                            p4.position = pt4;
                            p4.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);
                            ZongjinPoint_list.Add(p3);
                            ZongjinPoint_list.Add(p4);
                        }
                    }






                }

                if (strToRein.Rein_Detail_list.Count == 2)
                {
                    int NoCdim = strToRein.Rein_Detail_list[1].TypeDist;//原始C筋的直径

                    for (int i = 0; i < ZongjinPoint_list.Count; i++)
                    {
                        if (ZongjinPoint_list[i].hasUse == false)
                        {
                            ZongjinPoint_list[i].size = NoCdim;
                        }
                    }

                }
                else if (strToRein.Rein_Detail_list.Count == 1)
                {
                    for (int i = 0; i < ZongjinPoint_list.Count; i++)
                    {
                        if (ZongjinPoint_list[i].hasUse == false)
                        {
                            ZongjinPoint_list[i].size = strToRein.Rein_Detail_list[0].TypeDist;
                        }
                    }
                }



            }
        }
        public override void DrawCJin()
        {
            try
            {
            StrToReinforce LReinStr = new StrToReinforce();
            LReinStr = Helper.StrToRein(thLTypeEdgeComponent.Reinforce);
            if (LReinStr.Rein_Detail_list.Count == 0)
            {
                return;
            }
            List<ZongjinPoint> ZongjinPoints = new List<ZongjinPoint>();
            L_FindCJin(points, LReinStr, ZongjinPoints); 
            if (thLTypeEdgeComponent.Bf < 300 && thLTypeEdgeComponent.Bw < 300)
            {
                int Cnum = LReinStr.Rein_Detail_list[0].TypeNum;
                int Csize = LReinStr.Rein_Detail_list[0].TypeDist;
                bool isCal = false;
                int Step = 0;
                if (thLTypeEdgeComponent.IsCalculation == true && !thLTypeEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
                {
                    isCal = true;
                    StrToReinforce enhanceRein = new StrToReinforce();
                    enhanceRein = Helper.StrToRein(thLTypeEdgeComponent.EnhancedReinforce);
                    int FirstNum = enhanceRein.Rein_Detail_list[0].TypeNum;
                    int FirstDim = enhanceRein.Rein_Detail_list[0].TypeDist;
                    int dim = LReinStr.Rein_Detail_list[0].TypeDist;
                    if (enhanceRein.num == LReinStr.num)
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
                        if (enhanceRein.num - LReinStr.num == 2)
                        {
                            Step = 5;
                        }
                        else if (enhanceRein.num - LReinStr.num == 4)
                        {
                            Step = 6;
                        }
                    }
                }
                if (thLTypeEdgeComponent.Type == "A")
                {
                    if (LReinStr.Rein_Detail_list[0].TypeNum <= LReinStr.num / 2)
                    {
                        //标记C筋
                        if (Cnum == 2)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            }
                        }
                        else if (Cnum == 4)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 300);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[2], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 300);
                            }
                        }
                        else if (Cnum == 6)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 6, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else if (Step > 1)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[6], points[3], 2, ZongjinPoints[6].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                        }
                        else if (Cnum == 5)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                        }
                        else if (Cnum == 7)
                        {
                            Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 4, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }
                        }
                        else if (Cnum == 8)
                        {
                            if (Step == 5)
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[4], 6, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                    Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                }


                            }
                            else if (Step == 6)
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[4], 6, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                    Helper.CreateRectAndLabel(points[1], points[3], 6, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                }
                            }
                            else if (isCal == true)
                            {
                                if (Step == 1)
                                {
                                    int rightnum = Helper.NumOfTwoPoint(points, 5, 6);
                                    if (rightnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                        int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                        Helper.CreateRectAndLabel(points[1], points[Helper.FindPairPoint(points, idx, 2)], 6, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);

                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                        int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 2)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                        Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                                    }
                                }
                                else
                                {
                                    int rightnum = Helper.NumOfTwoPoint(points, 5, 6);
                                    if (rightnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                        int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                        Helper.CreateRectAndLabel(points[6], points[Helper.FindPairPoint(points, idx, 2)], 4, ZongjinPoints[6].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                        Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);

                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                        int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 2)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                        Helper.CreateRectAndLabel(points[6], points[3], 2, ZongjinPoints[6].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                        Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                    }
                                }
                            }
                            else
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[4], 4, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                    Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                }
                            }
                        }


                    }
                    else if (Cnum != LReinStr.num)
                    {
                        //分成T型的上面、左面、右面进行框选
                        int TopNum = 0;
                        int RightNum = 0;
                        //上面
                        if (ZongjinPoints[0].hasUse == false)
                        {
                            int idx = Helper.FindClosePoint(points, 7, 6, 6);
                            Helper.CreateRectAndLabel(points[0], points[idx], Helper.NumOfTwoPoint(points, 6, 7) * 2 + 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                        }
                        else
                        {
                            TopNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int Topidx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (TopNum > 0)
                            {
                                if (ZongjinPoints[Topidx].hasUse == false)
                                {

                                    int idx2 = Helper.FindClosePoint(points, 1, Topidx, 1);
                                    if (idx2 != -1)
                                    {
                                        Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, idx2, 1)], Helper.NumOfTwoPoint(points, Topidx, 1) * 2 + 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);

                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, Topidx, 1)], 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                    }
                                    break;
                                }
                                else
                                {
                                    Topidx = Helper.FindClosePoint(points, 1, Topidx, Topidx);
                                    TopNum = TopNum - 1;
                                }

                            }
                        }
                        //右面
                        RightNum = Helper.NumOfTwoPoint(points, 5, 6);
                        int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                        while (RightNum > 0)
                        {

                            if (ZongjinPoints[rightIdx].hasUse == false)
                            {

                                int idx = Helper.FindClosePoint(points, rightIdx, 6, 6);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, rightIdx, 2)], Helper.NumOfTwoPoint(points, rightIdx, idx) * 2 + 4, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[rightIdx], points[Helper.FindPairPoint(points, rightIdx, 2)], 2, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                }
                                break;
                            }
                            else
                            {
                                rightIdx = Helper.FindClosePoint(points, rightIdx, 6, rightIdx);
                                RightNum = RightNum - 1;
                            }
                        }

                        //检查6号位置，特殊情况
                        if (ZongjinPoints[6].hasUse == false)
                        {
                            Helper.CreateRectAndLabel(points[6], points[6], 1, ZongjinPoints[6].size, LabelAndRect, CJintText, 200, 1000, 2, 200);

                        }

                        //计算纵筋的情况
                        if (Step == 1)
                        {
                            Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                        }
                        else if (Step == 2 || Step == 3 || Step == 4)
                        {
                            Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 4, 200);
                        }
                    }
                }
                else
                {
                    if (LReinStr.Rein_Detail_list[0].TypeNum <= LReinStr.num / 2)
                    {

                        if (Cnum == 2)
                        {
                                if (Step == 5)
                                {
                                    Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                }

                        }
                        else if (Cnum == 4)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[2], points[3], 4, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                        }
                        else if (Cnum == 6)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 6, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);

                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[1], points[6], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 3, 400, 1000, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 300);
                            }
                        }
                        else if (Cnum == 5)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 4, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);
                            }
                        }
                        else if (Cnum == 7)
                        {
                            Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 4, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 800, 3, 200);
                            }
                        }
                        else if (Cnum == 8)
                        {
                            if (Step == 5)
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 6, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                            }
                            else if (Step == 6)
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 6, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 6, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                            }
                            else if (isCal == true)
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                if (Step == 1)
                                {
                                    int topnum = Helper.NumOfTwoPoint(points, 0, 1);
                                    if (topnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[idx], points[3], 6, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);

                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 1)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                    }
                                }
                                else
                                {
                                    int topnum = Helper.NumOfTwoPoint(points, 0, 1);
                                    if (topnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[idx], points[6], 4, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                        Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 1)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                        Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                        Helper.CreateRectAndLabel(points[1], points[6], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                    }
                                }
                            }
                            else
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 4, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                            }
                        }
                    }
                    else if (Cnum != LReinStr.num)
                    {
                        //分成T型的上面、左面、右面进行框选
                        int TopNum = 0;
                        int RightNum = 0;
                        //上面

                        TopNum = Helper.NumOfTwoPoint(points, 0, 1);
                        int Topidx = Helper.FindClosePoint(points, 0, 1, 0);
                        while (TopNum > 0)
                        {
                            if (ZongjinPoints[Topidx].hasUse == false)
                            {

                                int idx2 = Helper.FindClosePoint(points, 1, Topidx, 1);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, idx2, 1)], Helper.NumOfTwoPoint(points, Topidx, 1) * 2 + 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);

                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, Topidx, 1)], 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                }
                                break;
                            }
                            else
                            {
                                Topidx = Helper.FindClosePoint(points, 1, Topidx, Topidx);
                                TopNum = TopNum - 1;
                            }

                        }

                        //右面
                        if (ZongjinPoints[4].hasUse == false)
                        {
                            int idx = Helper.FindClosePoint(points, 5, 6, 6);
                            if (idx != -1)
                            {
                                Helper.CreateRectAndLabel(points[idx], points[4], Helper.NumOfTwoPoint(points, 5, 6) * 2 + 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }

                        }
                        else
                        {
                            RightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            while (RightNum > 0)
                            {

                                if (ZongjinPoints[rightIdx].hasUse == false)
                                {

                                    int idx = Helper.FindClosePoint(points, rightIdx, 6, 6);
                                    if (idx != -1)
                                    {
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, rightIdx, 2)], Helper.NumOfTwoPoint(points, rightIdx, idx) * 2 + 4, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[rightIdx], points[Helper.FindPairPoint(points, rightIdx, 2)], 2, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                    }
                                    break;
                                }
                                else
                                {
                                    rightIdx = Helper.FindClosePoint(points, rightIdx, 6, rightIdx);
                                    RightNum = RightNum -1;
                                }
                            }
                        }


                        //检查6号位置，特殊情况
                        if (ZongjinPoints[6].hasUse == false)
                        {
                            Helper.CreateRectAndLabel(points[6], points[6], 1, ZongjinPoints[6].size, LabelAndRect, CJintText, 200, 1000, 2, 200);

                        }

                        //计算纵筋的情况
                        if (Step == 1)
                        {
                            Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                        }
                        else if (Step == 2 || Step == 3 || Step == 4)
                        {
                            Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                        }
                    }
                }
            }
            else
            {

                
                int Cnum = LReinStr.Rein_Detail_list[0].TypeNum;
                int Csize = LReinStr.Rein_Detail_list[0].TypeDist;
                bool isCal = false;
                int Step = 0;
                if (thLTypeEdgeComponent.IsCalculation == true && !thLTypeEdgeComponent.EnhancedReinforce.IsNullOrEmpty())//修改
                {
                    isCal = true;
                    StrToReinforce enhanceRein = new StrToReinforce();
                    enhanceRein = Helper.StrToRein(thLTypeEdgeComponent.EnhancedReinforce);
                    int FirstNum = enhanceRein.Rein_Detail_list[0].TypeNum;
                    int FirstDim = enhanceRein.Rein_Detail_list[0].TypeDist;
                    int dim = LReinStr.Rein_Detail_list[0].TypeDist;
                    if (enhanceRein.num == LReinStr.num)
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
                        if (enhanceRein.num - LReinStr.num == 2)
                        {
                            Step = 5;
                        }
                        else if (enhanceRein.num - LReinStr.num == 4)
                        {
                            Step = 6;
                        }
                    }
                }
                if (thLTypeEdgeComponent.Type == "A")
                {
                    if (LReinStr.Rein_Detail_list[0].TypeNum <= LReinStr.num / 2)
                    {
                        if (Cnum == 2)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[4], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1500, 6, 200);
                            }
                        }
                        else if (Cnum == 4)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[4], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 1500, 5, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[4], points[4], 2, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 1500, 5, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 1500, 5, 200);
                            }

                        }
                        else if (Cnum == 5)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                        }
                        else if (Cnum == 6)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 300);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 300);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 300);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                int idx2 = Helper.FindMidPoint(points, 1, 2);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 300);
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            }
                        }
                        else if (Cnum == 7)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                int idx2 = Helper.FindMidPoint(points, 1, 2);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                        }
                        else if (Cnum == 8)
                        {
                            if (Step == 5)
                            {

                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 6, 300);
                            }
                            else if (Step == 6)
                            {

                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                                Helper.CreateRectAndLabel(points[1], points[3], 7, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 6, 300);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);

                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                int idx = Helper.FindMidPoint(points, 4, 5);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[idx], 1, ZongjinPoints[idx].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                int idx2 = Helper.FindMidPoint(points, 1, 2);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1000, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[6], points[3], 2, ZongjinPoints[6].size, LabelAndRect, CJintText, 1500, 1000, 6, 200);

                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                        }
                        else if (Cnum == 9)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 6, 200);


                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                                Helper.CreateRectAndLabel(points[1], points[2], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 5, ZongjinPoints[5].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                            }
                            else if (isCal == true)
                            {
                                if (Step == 1)
                                {
                                    Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                    Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                                    Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                    Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                    int idx2 = Helper.FindMidPoint(points, 4, 5);
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 1200, 800, 8, 200);

                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                    Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                                    Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                    Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                    Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                    int idx1 = Helper.FindMidPoint(points, 1, 2);
                                    int idx2 = Helper.FindMidPoint(points, 4, 5);
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 200, 800, 7, 200);
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 1200, 800, 8, 200);
                                }
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 7, 200);
                                Helper.CreateRectAndLabel(points[1], points[2], 3, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 200, 800, 2, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                            }
                        }
                        else if (Cnum == 10)
                        {
                            if (Step == 5)
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                Helper.CreateRectAndLabel(points[idx], points[4], 7, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                            else if (Step == 6)
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                Helper.CreateRectAndLabel(points[idx], points[4], 7, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 7, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                if (Step == 1)
                                {
                                    int rightnum = Helper.NumOfTwoPoint(points, 3, 4);
                                    if (rightnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 800, 6, 200);
                                        int idx1 = Helper.FindMidPoint(points, 4, 5);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1200, 800, 7, 200);
                                        int idx = Helper.FindClosePoint(points, 3, 4, 4);
                                        Helper.CreateRectAndLabel(points[1], points[idx], 7, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 800, 6, 200);
                                        int idx1 = Helper.FindMidPoint(points, 4, 5);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1200, 800, 7, 200);
                                        int idx = Helper.FindClosePoint(points, 3, 4, 4);
                                        Helper.CreateRectAndLabel(points[Helper.FindPairPoint(points, idx, 2)], points[idx], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                }
                                else
                                {
                                    int rightnum = Helper.NumOfTwoPoint(points, 3, 4);
                                    if (rightnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 800, 6, 200);
                                        int idx1 = Helper.FindMidPoint(points, 4, 5);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1200, 800, 7, 200);
                                        int idx = Helper.FindClosePoint(points, 3, 4, 4);
                                        Helper.CreateRectAndLabel(points[6], points[idx], 4, ZongjinPoints[6].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                        int idx2 = Helper.FindMidPoint(points, 1, 2);
                                        Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 800, 800, 3, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 400, 800, 6, 200);
                                        int idx1 = Helper.FindMidPoint(points, 4, 5);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1200, 800, 7, 200);
                                        int idx = Helper.FindClosePoint(points, 3, 4, 4);
                                        Helper.CreateRectAndLabel(points[Helper.FindPairPoint(points, idx, 2)], points[idx], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 800, 2, 200);
                                        Helper.CreateRectAndLabel(points[6], points[3], 2, ZongjinPoints[6].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                        int idx2 = Helper.FindMidPoint(points, 1, 2);
                                        Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 800, 800, 3, 200);
                                    }
                                }
                            }
                            else
                            {
                                int idx = Helper.FindClosePoint(points, 5, 6, 5);
                                Helper.CreateRectAndLabel(points[idx], points[4], 5, ZongjinPoints[4].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                        }
                    }
                    else if (Cnum != LReinStr.num)
                    {
                        //分成T型的上面、左面、右面进行框选
                        int TopNum = 0;
                        int RightNum = 0;
                        //上面
                        if (ZongjinPoints[0].hasUse == false)
                        {
                            int idx = Helper.FindClosePoint(points, 7, 6, 6);
                            Helper.CreateRectAndLabel(points[0], points[idx], Helper.NumOfTwoPoint(points, 6, 7) * 2 + 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 3, 200);

                            int idx3 = Helper.FindMidPoint(points, 2, 3);
                            if (ZongjinPoints[idx3].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx3], points[idx3], 1, ZongjinPoints[idx3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                        }
                        else
                        {
                            int idx3 = Helper.FindMidPoint(points, 2, 3);
                            if (ZongjinPoints[idx3].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx3], points[idx3], 1, ZongjinPoints[idx3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                            int idx4 = Helper.FindMidPoint(points, 0, 7);
                            if (ZongjinPoints[idx4].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx4], points[idx4], 1, ZongjinPoints[idx4].size, LabelAndRect, CJintText, 1000, 800, 1, 200);
                            }
                            TopNum = Helper.NumOfTwoPoint(points, 0, 1);
                            int Topidx = Helper.FindClosePoint(points, 0, 1, 0);
                            while (TopNum > 0)
                            {
                                if (ZongjinPoints[Topidx].hasUse == false)
                                {

                                    int idx2 = Helper.FindClosePoint(points, 1, Topidx, 1);
                                    if (idx2 != -1)
                                    {
                                        Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, idx2, 1)], Helper.NumOfTwoPoint(points, Topidx, 1) * 2 + 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);

                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, Topidx, 1)], 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                    }
                                    break;
                                }
                                else
                                {
                                    Topidx = Helper.FindClosePoint(points, 1, Topidx, Topidx);
                                    TopNum = TopNum - 1;
                                }

                            }
                        }
                        //右面
                        RightNum = Helper.NumOfTwoPoint(points, 5, 6);
                        int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                        while (RightNum > 0)
                        {

                            if (ZongjinPoints[rightIdx].hasUse == false)
                            {

                                int idx = Helper.FindClosePoint(points, rightIdx, 6, 6);
                                if (idx != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, rightIdx, 2)], Helper.NumOfTwoPoint(points, rightIdx, idx) * 2 + 4, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[rightIdx], points[Helper.FindPairPoint(points, rightIdx, 2)], 2, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                }
                                break;
                            }
                            else
                            {
                                rightIdx = Helper.FindClosePoint(points, rightIdx, 6, rightIdx);
                                RightNum = RightNum - 1;
                            }
                        }

                        //检查6号位置，特殊情况
                        if (ZongjinPoints[6].hasUse == false)
                        {
                            Helper.CreateRectAndLabel(points[6], points[6], 1, ZongjinPoints[6].size, LabelAndRect, CJintText, 200, 1000, 2, 200);

                        }

                        //计算纵筋的情况
                        if (Step == 1)
                        {
                            Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                        }
                        else if (Step == 2 || Step == 3 || Step == 4)
                        {
                            Helper.CreateRectAndLabel(points[5], points[4], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            Helper.CreateRectAndLabel(points[1], points[2], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 4, 200);
                        }
                    }

                }
                else
                {
                    if (LReinStr.Rein_Detail_list[0].TypeNum <= LReinStr.num / 2)
                    {
                        if (Cnum == 2)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 2, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                            }
                        }
                        else if (Cnum == 4)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 2, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 2, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 2, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }

                        }
                        else if (Cnum == 5)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 2, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }

                        }
                        else if (Cnum == 6)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 5, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 1)
                            {

                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                int idx2 = Helper.FindMidPoint(points, 2, 3);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1500, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                        }
                        else if (Cnum == 7)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 5, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                int idx2 = Helper.FindMidPoint(points, 2, 3);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1500, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                            }
                        }
                        else if (Cnum == 8)
                        {
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 7, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                int idx2 = Helper.FindMidPoint(points, 2, 3);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1500, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                                Helper.CreateRectAndLabel(points[1], points[6], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1500, 1, 200);

                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                        }
                        else if (Cnum == 9)
                        {
                            Helper.CreateRectAndLabel(points[5], points[5], 1, ZongjinPoints[5].size, LabelAndRect, CJintText, 800, 800, 2, 200);
                            Helper.CreateRectAndLabel(points[4], points[4], 1, ZongjinPoints[4].size, LabelAndRect, CJintText, 800, 800, 6, 200);
                            if (Step == 5)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);

                            }
                            else if (Step == 6)
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 5, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (Step == 1)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                                int idx1 = Helper.FindMidPoint(points, 0, 7);
                                if (idx1 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 1000, 1000, 1, 200);

                                }
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                int idx2 = Helper.FindMidPoint(points, 2, 3);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 200, 1500, 1, 200);
                                }
                                Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1500, 5, 200);
                                Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 1500, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[0], points[7], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 200, 1000, 3, 200);
                                Helper.CreateRectAndLabel(points[2], points[3], 3, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                            }
                        }
                        else if (Cnum == 10)
                        {
                            if (Step == 5)
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 7, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                            else if (Step == 6)
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 7, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 7, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                            else if (isCal == true)
                            {
                                if (Step == 1)
                                {
                                    int topnum = Helper.NumOfTwoPoint(points, 0, 1);
                                    if (topnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                        Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        int idx1 = Helper.FindMidPoint(points, 0, 7);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 800, 800, 1, 200);
                                        int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                        Helper.CreateRectAndLabel(points[idx], points[3], 7, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                        Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        int idx1 = Helper.FindMidPoint(points, 0, 7);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 800, 800, 1, 200);
                                        int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 1)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                        Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                }
                                else
                                {
                                    int topnum = Helper.NumOfTwoPoint(points, 0, 1);
                                    if (topnum == 1)
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                        Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        int idx1 = Helper.FindMidPoint(points, 0, 7);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 800, 800, 1, 200);
                                        int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                        Helper.CreateRectAndLabel(points[idx], points[6], 4, ZongjinPoints[idx].size, LabelAndRect, CJintText, 400, 800, 4, 200);
                                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 200, 800, 4, 200);
                                        Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                        int idx2 = Helper.FindMidPoint(points, 2, 3);
                                        Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                        Helper.CreateRectAndLabel(points[7], points[7], 1, ZongjinPoints[7].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                                        int idx1 = Helper.FindMidPoint(points, 0, 7);
                                        Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 800, 800, 1, 200);
                                        int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, idx, 1)], 2, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1500, 800, 7, 200);
                                        Helper.CreateRectAndLabel(points[1], points[6], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 400, 800, 3, 200);
                                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 200, 800, 4, 200);
                                        Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                                        int idx2 = Helper.FindMidPoint(points, 2, 3);
                                        Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                                    }
                                }
                            }
                            else
                            {
                                int idx = Helper.FindClosePoint(points, 0, 1, 0);
                                Helper.CreateRectAndLabel(points[0], points[Helper.FindPairPoint(points, idx, 1)], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 800, 1, 200);
                                Helper.CreateRectAndLabel(points[1], points[3], 5, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                            }
                        }
                    }
                    else if (Cnum != LReinStr.num)
                    {
                        //分成T型的上面、左面、右面进行框选
                        int TopNum = 0;
                        int RightNum = 0;
                        //上面

                        TopNum = Helper.NumOfTwoPoint(points, 0, 1);
                        int Topidx = Helper.FindClosePoint(points, 0, 1, 0);
                        while (TopNum > 0)
                        {
                            if (ZongjinPoints[Topidx].hasUse == false)
                            {

                                int idx2 = Helper.FindClosePoint(points, 1, Topidx, 1);
                                if (idx2 != -1)
                                {
                                    Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, idx2, 1)], Helper.NumOfTwoPoint(points, Topidx, 1) * 2 + 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);

                                }
                                else
                                {
                                    Helper.CreateRectAndLabel(points[Topidx], points[Helper.FindPairPoint(points, Topidx, 1)], 2, ZongjinPoints[Topidx].size, LabelAndRect, CJintText, 400, 1000, 3, 200);
                                }
                                break;
                            }
                            else
                            {
                                Topidx = Helper.FindClosePoint(points, 1, Topidx, Topidx);
                                TopNum = TopNum - 1;
                            }

                        }

                        //右面
                        if (ZongjinPoints[4].hasUse == false)
                        {
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (ZongjinPoints[idx1].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 400, 800, 4, 200);

                            }
                            int idx = Helper.FindClosePoint(points, 5, 6, 6);
                            if (idx != -1)
                            {
                                Helper.CreateRectAndLabel(points[idx], points[4], Helper.NumOfTwoPoint(points, 5, 6) * 2 + 3, ZongjinPoints[idx].size, LabelAndRect, CJintText, 1000, 800, 6, 200);
                            }
                            else
                            {
                                Helper.CreateRectAndLabel(points[5], points[4], 3, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            }

                        }
                        else
                        {
                            int idx1 = Helper.FindMidPoint(points, 1, 2);
                            if (ZongjinPoints[idx1].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 400, 800, 4, 200);

                            }
                            int idx2 = Helper.FindMidPoint(points, 4, 5);
                            if (ZongjinPoints[idx2].hasUse == false)
                            {
                                Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 1000, 800, 7, 200);

                            }
                            RightNum = Helper.NumOfTwoPoint(points, 5, 6);
                            int rightIdx = Helper.FindClosePoint(points, 5, 6, 5);
                            while (RightNum > 0)
                            {

                                if (ZongjinPoints[rightIdx].hasUse == false)
                                {

                                    int idx = Helper.FindClosePoint(points, rightIdx, 6, 6);
                                    if (idx != -1)
                                    {
                                        Helper.CreateRectAndLabel(points[idx], points[Helper.FindPairPoint(points, rightIdx, 2)], Helper.NumOfTwoPoint(points, rightIdx, idx) * 2 + 4, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                    }
                                    else
                                    {
                                        Helper.CreateRectAndLabel(points[rightIdx], points[Helper.FindPairPoint(points, rightIdx, 2)], 2, ZongjinPoints[rightIdx].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                                    }
                                    break;
                                }
                                else
                                {
                                    rightIdx = Helper.FindClosePoint(points, rightIdx, 6, rightIdx);
                                    RightNum = RightNum - 1;
                                }
                            }
                        }


                        //检查6号位置，特殊情况
                        if (ZongjinPoints[6].hasUse == false)
                        {
                            Helper.CreateRectAndLabel(points[6], points[6], 1, ZongjinPoints[6].size, LabelAndRect, CJintText, 200, 1000, 2, 200);

                        }

                        //计算纵筋的情况
                        if (Step == 1)
                        {
                            Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                        }
                        else if (Step == 2 || Step == 3 || Step == 4)
                        {
                            Helper.CreateRectAndLabel(points[0], points[7], 2, ZongjinPoints[5].size, LabelAndRect, CJintText, 400, 800, 2, 200);
                            Helper.CreateRectAndLabel(points[2], points[3], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 800, 5, 200);
                        }
                    }
                }

            }
            }
            catch (Exception ex)
            {
                string message= ex.Message;
                return;
            }

        }

        protected override void CalLinkPosition()
        {
            //遍历所有点，找出2，3,4类型的钢筋，钢筋,同时查表,因为是一对对的点，所以每次加两个点
            for (int i = 0; i < points.Count; i += 2)
            {
                if (pointsFlag[i] == 2)
                {
                    if (thLTypeEdgeComponent.Link2.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else if (pointsFlag[i] == 3)
                {
                    if (thLTypeEdgeComponent.Link3.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else if (pointsFlag[i] == 4)
                {
                    if (thLTypeEdgeComponent.Link4.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else continue;
                double r = thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight / 2;
                Polyline link = GangJinLink.DrawLink(points[i], points[i + 1], r, thLTypeEdgeComponent.StirrupLineWeight, scale);
                Links.Add(link);
                LinksFlag.Add(pointsFlag[i]);
            }
        }

        protected override void CalStirrupPosition()
        {
            GangJinStirrup stirrup = new GangJinStirrup
            {
                Outline = Outline,
                scale = scale,
                GangjinType = 1
            };
            stirrup.CalPositionL(thLTypeEdgeComponent);
            foreach (var polyline in stirrup.stirrups)
            {
                Links.Add(polyline);
                LinksFlag.Add(1);
            }
        }




        public override void init(ThEdgeComponent component, string elevation, double tblRowHeight, double scale, Point3d position)
        {
            this.thLTypeEdgeComponent = component as ThLTypeEdgeComponent;
            this.elevation = elevation;
            this.tblRowHeight = tblRowHeight;
            this.scale = scale;
            this.number = thLTypeEdgeComponent.Number;
            TableStartPt = position;
            if (thLTypeEdgeComponent.IsCalculation && !thLTypeEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
            {
                this.Reinforce = thLTypeEdgeComponent.EnhancedReinforce;
            }
            else
            {
                this.Reinforce = thLTypeEdgeComponent.Reinforce;
            }
            this.Stirrup = thLTypeEdgeComponent.Stirrup;
        }

        public override void CalExplo()
        {
            Point2d p1 = Outline.GetPoint2dAt(2), p2 = Outline.GetPoint2dAt(6);
            Vector2d vec = new Vector2d(p2.X - p1.X, Outline.GetPoint2dAt(7).Y - p1.Y + 2000);
            Point2d centrePt = p1 + (p2 - p1) / 2;
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
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 400), centrePt);
                        plinks[3].Add(tmp);
                    }
                    else
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(400, 0), centrePt);
                        plinks[4].Add(tmp);
                    }
                }
                tmp.LayerId = DbHelper.GetLayerId("LINK");
                objectCollection.Add(tmp);
            }
            if (!thLTypeEdgeComponent.Link2.IsNullOrEmpty() &&
                thLTypeEdgeComponent.Link2.Substring(1) != thLTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[1].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 1, i));
                }
                DrawLinkLabel(tmpList, true, thLTypeEdgeComponent.Link2, 200, -2000);
            }
            if (!thLTypeEdgeComponent.Link3.IsNullOrEmpty() &&
                thLTypeEdgeComponent.Link3.Substring(1) != thLTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[2].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 2, i));
                }
                DrawLinkLabel(tmpList, false, thLTypeEdgeComponent.Link3, 200, 2000);
            }
            if (!thLTypeEdgeComponent.Link4.IsNullOrEmpty() &&
                thLTypeEdgeComponent.Link4.Substring(1) != thLTypeEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[3].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 3, i));
                }
                DrawLinkLabel(tmpList, false, thLTypeEdgeComponent.Link4, -400, -2000);

                tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[4].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 4, i));
                }
                DrawLinkLabel(tmpList, true, thLTypeEdgeComponent.Link4, -600, 2000);
            }
        }
    }
}
