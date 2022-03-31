﻿using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectRectangle : DrawObjectBase
    {
        ThRectangleEdgeComponent thRectangleEdgeComponent;


        /// <summary>
        /// 一型钢筋确定点的位置,先去掉四角的点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        protected override void CalReinforcePosition(int pointNum, Polyline polyline)
        {
            points = new List<Point3d>();
            pointsFlag = new List<int>();
            //纵筋相对轮廓的偏移值
            double offset = scale * (thRectangleEdgeComponent.C + 5) + thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight;
            //根据点的对数所有点的位置确定位置
            //polyline从左上角逆时针旋转，四个点先获取
            points = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i == 0)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }
                //左下角，向右上偏移
                else if (i == 1)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if (i == 2)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if (i == 3)
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
            //计算竖直方向最远两个纵筋的距离
            double disY = points[0].Y - points[1].Y;
            //水平方向最远纵筋距离
            double disX = points[3].X - points[0].X;

            int needLayoutPointsNum = pointNum - 4;
            //需要排布的点的对数
            int pointsPair = needLayoutPointsNum / 2;
            //遍历所有排布可能性，选取方差最小的,bw是竖直方向上的，hc是水平方向，先从竖直方向0个布点开始计算
            List<double> numbers = new List<double>();
            //记录在bw方向上有几对点
            int result = 0;
            double minVar = 100000000;
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
            //把一对对点的位置计算出来，计算竖直方向BW上的位置,0，3号点每次向下偏移deltaY,水平方向0，1号点每次向右偏移deltaX
            for (int i = 0; i < result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[3].X, points[3].Y - deltaY, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(3);
                pointsFlag.Add(3);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X + deltaX, points[0].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[1].X + deltaX, points[1].Y, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }


        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }

        public override void DrawCJin()
        {

        }
        protected override void CalLinkPosition()
        {
            //遍历所有点，找出2，3类型的钢筋，钢筋,同时查表,因为是一对对的点，所以每次加两个点
            for (int i = 0; i < points.Count; i += 2)
            {
                if (pointsFlag[i] == 2)
                {
                    if (thRectangleEdgeComponent.Link2.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else if (pointsFlag[i] == 3)
                {
                    if (thRectangleEdgeComponent.Link3.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else continue;
                double r = thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight / 2;
                Polyline link = GangJinLink.DrawLink(points[i], points[i + 1], r, thRectangleEdgeComponent.StirrupLineWeight, scale);
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
            stirrup.CalPositionR(thRectangleEdgeComponent);
            foreach (var polyline in stirrup.stirrups)
            {
                Links.Add(polyline);
                LinksFlag.Add(1);
            }
        }



        public override void DrawOutline()
        {
            double width = thRectangleEdgeComponent.Hc * scale;
            double height = thRectangleEdgeComponent.Bw * scale;
            Point3d startPt = TableStartPt + new Vector3d((FirstRowWidth - width) / 2, -FirstRowHeight + height + 1500, 0);
            var pts = new Point3dCollection
            {
                startPt + new Vector3d(0, 0, 0) * scale,
                startPt + new Vector3d(0, -height, 0),
                startPt + new Vector3d(width, -height, 0),
                startPt + new Vector3d(width, 0, 0)
            };
            Outline = pts.CreatePolyline();
        }

        public override void DrawWall()
        {
            LinkedWallLines = new List<Curve>();
            double bw = thRectangleEdgeComponent.Bw * scale;
            Point3d pt1 = Outline.GetPoint3dAt(2), pt2 = Outline.GetPoint3dAt(3);
            Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);

            LinkedWallLines.Add(line1);
            LinkedWallLines.Add(line2);
            LinkedWallLines.Add(polyline);

            if (thRectangleEdgeComponent.LinkWallPos == "2")
            {
                pt1 = Outline.GetPoint3dAt(0);
                pt2 = Outline.GetPoint3dAt(1);
                polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(-bw * 5 / 8.0, 0, 0), out line1, out line2);

                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
        }

        public override void DrawDim()
        {
            rotatedDimensions = new List<RotatedDimension>();
            double bw = thRectangleEdgeComponent.Bw * scale;
            RotatedDimension rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(1),
                XLine2Point = Outline.GetPoint3dAt(2),
                DimLinePoint = Outline.GetPoint3dAt(1) + new Vector3d(0, -600, 0),
                DimensionText = thRectangleEdgeComponent.Hc.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            Point3d pt1 = Outline.GetPoint3dAt(0), pt2 = Outline.GetPoint3dAt(1);
            if (thRectangleEdgeComponent.LinkWallPos == "2")
            {
                pt1 += new Vector3d(-bw * 5 / 8.0, 0, 0);
                pt2 += new Vector3d(-bw * 5 / 8.0, 0, 0);
            }
            rotatedDimension = new RotatedDimension
            {
                XLine1Point = pt1,
                XLine2Point = pt2,
                DimLinePoint = pt1 + new Vector3d(-600, 0, 0),
                DimensionText = thRectangleEdgeComponent.Bw.ToString(),
                Rotation = Math.PI / 2.0
            };
            rotatedDimensions.Add(rotatedDimension);
        }

        public override void init(ThEdgeComponent component, string elevation, double tblRowHeight, double scale, Point3d position)
        {
            this.thRectangleEdgeComponent = component as ThRectangleEdgeComponent;
            this.elevation = elevation;
            this.tblRowHeight = tblRowHeight;
            this.scale = scale;
            this.number = thRectangleEdgeComponent.Number;
            TableStartPt = position;
            if (thRectangleEdgeComponent.IsCalculation)
            {
                this.Reinforce = thRectangleEdgeComponent.EnhancedReinforce;
            }
            else
            {
                this.Reinforce = thRectangleEdgeComponent.Reinforce;
            }
            this.Stirrup = thRectangleEdgeComponent.Stirrup;
        }

        public override void CalExplo()
        {
            Point2d p1 = Outline.GetPoint2dAt(1), p2 = Outline.GetPoint2dAt(3);
            Vector2d vec = new Vector2d(0, p2.Y - p1.Y + 1200);
            Point2d centrePt = p1 + (p2 - p1) / 2;
            //生成的拉筋：link1,link2,link3
            List<List<Polyline>> plinks = new List<List<Polyline>>
            {
                new List<Polyline>(),
                new List<Polyline>(),
                new List<Polyline>(),
            };
            for (int i = 0; i < Links.Count; i++)
            {
                Polyline tmp = new Polyline();
                if (LinksFlag[i] == 1)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec, centrePt);
                    plinks[0].Add(tmp);
                }
                else
                {
                    if (LinksFlag[i] == 2)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 200), centrePt);
                        plinks[1].Add(tmp);
                    }
                    else if (LinksFlag[i] == 3)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 0), centrePt);
                        plinks[2].Add(tmp);
                    }
                }
                tmp.Layer = "LINK";
                objectCollection.Add(tmp);
            }
            if (!thRectangleEdgeComponent.Link2.IsNullOrEmpty() &&
                thRectangleEdgeComponent.Link2.Substring(1) != thRectangleEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for(int i = 0; i < plinks[1].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 1, i));
                }
                DrawLinkLabel(tmpList, false, thRectangleEdgeComponent.Link2, 200, 2000);
            }
            if (!thRectangleEdgeComponent.Link3.IsNullOrEmpty() &&
                thRectangleEdgeComponent.Link3.Substring(1) != thRectangleEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[2].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 2, i));
                }
                DrawLinkLabel(tmpList, true, thRectangleEdgeComponent.Link3, -200, 2000);
            }
        }
    }
}
