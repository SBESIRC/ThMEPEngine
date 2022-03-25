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
            double offset = scale * (thLTypeEdgeComponent.C + 5) + thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight;
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
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[7].X, points[7].Y - deltaY, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[3].X + deltaX, points[3].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[6].X + deltaX, points[6].Y, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(3);
                pointsFlag.Add(3);
            }
        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

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
            if (thLTypeEdgeComponent.IsCalculation)
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
            Vector2d vec = new Vector2d(p2.X - p1.X, Outline.GetPoint2dAt(7).Y - p1.Y + 1200);
            Point2d centrePt = p1 + (p2 - p1) / 2;
            bool flag = false;
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
                }
                else if (LinksFlag[i] == 2)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 200), centrePt);
                }
                else if (LinksFlag[i] == 3)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 200), centrePt);
                }
                else if (LinksFlag[i] == 4)
                {
                    if (Math.Abs((Links[i].GetPoint2dAt(1) - Links[i].GetPoint2dAt(0)).X) < 1)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 300), centrePt);
                    }
                    else
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(300, 0), centrePt);
                    }
                }
                SmallLinks.Add(tmp);
            }
        }
    }
}
