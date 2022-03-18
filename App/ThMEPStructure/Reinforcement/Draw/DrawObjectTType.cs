using System;
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

        public override void DrawOutline(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            var pts = new Point3dCollection
            {
                TableStartPt + new Vector3d(200, -1500, 0)* scale,
                TableStartPt + new Vector3d(200, -1500 - thTTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s, -1500 - thTTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf, -1500 - thTTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf + thTTypeEdgeComponent.Hc2l, -1500 - thTTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf + thTTypeEdgeComponent.Hc2l, -1500, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf, -1500, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s + thTTypeEdgeComponent.Bf, -1500 + thTTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s, -1500 + thTTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(200 + thTTypeEdgeComponent.Hc2s, -1500, 0) * scale
            };
            Outline = pts.CreatePolyline();
        }
        public override void DrawWall(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
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
                Point3d pt1 = Outline.GetPoint3dAt(0), pt2 = Outline.GetPoint3dAt(1);
                double bw = thTTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(-bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (right)
            {
                Point3d pt1 = Outline.GetPoint3dAt(4), pt2 = Outline.GetPoint3dAt(5);
                double bw = thTTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (top)
            {
                Point3d pt1 = Outline.GetPoint3dAt(7), pt2 = Outline.GetPoint3dAt(8);
                double bf = thTTypeEdgeComponent.Bf * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(0, bf * 5 / 8.0, 0), out Line line1, out Line line2);
                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
        }
        public override void DrawDim(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            RotatedDimension rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(1),
                XLine2Point = Outline.GetPoint3dAt(2),
                DimLinePoint = Outline.GetPoint3dAt(1) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Hc2s.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(2),
                XLine2Point = Outline.GetPoint3dAt(3),
                DimLinePoint = Outline.GetPoint3dAt(2) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Bf.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(3),
                XLine2Point = Outline.GetPoint3dAt(4),
                DimLinePoint = Outline.GetPoint3dAt(3) + new Vector3d(0, -600, 0),
                DimensionText = thTTypeEdgeComponent.Hc2l.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            double bw = thTTypeEdgeComponent.Bw * 5 / 8.0 * scale;
            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(4),
                XLine2Point = Outline.GetPoint3dAt(5),
                DimLinePoint = Outline.GetPoint3dAt(4) + new Vector3d(600, 0, 0),
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
                XLine1Point = Outline.GetPoint3dAt(5),
                XLine2Point = Outline.GetPoint3dAt(5) + (Outline.GetPoint3dAt(7) - Outline.GetPoint3dAt(6)),
                DimLinePoint = Outline.GetPoint3dAt(4) + new Vector3d(600, 0, 0),
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

        /// <summary>
        /// T型钢筋确定位置，轮廓上10个点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        void CalReinforcePosition(int pointNum, ThTTypeEdgeComponent thTTypeEdgeComponent, Polyline polyline, double scale)
        {
            //存储结果
            List<Point3d> points = new List<Point3d>();
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
            }

            //底层需要添加额外的纵筋，2，3两点中点，6，7中点
            if (thTTypeEdgeComponent.Bw >= 300)
            {
                Point3d tmpPoint = new Point3d((points[2].X + points[3].X) / 2.0, (points[2].Y + points[3].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[6].X + points[7].X) / 2.0, (points[6].Y + points[7].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
            }

            //竖向需要添加额外的钢筋,0,9中点，4，5中点
            if (thTTypeEdgeComponent.Bf >= 300)
            {
                Point3d tmpPoint = new Point3d((points[0].X + points[9].X) / 2.0, (points[0].Y + points[9].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[4].X + points[5].X) / 2.0, (points[4].Y + points[5].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
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
                    double tmp = Helper.calVariance(numbers);
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
            }
            for (int i = 0; i < resultX1; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[2].X + deltaX1, points[2].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[3].X + deltaX1, points[3].Y, 0);
                points.Add(tmpPoint2);
            }
            for (int i = 0; i < pointsPair - resultX1 - resultY; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[5].X + deltaX2, points[5].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[8].X + deltaX2, points[8].Y, 0);
                points.Add(tmpPoint2);
            }

        }
    }
}
