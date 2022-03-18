using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectRectangle:DrawObjectBase
    {
        private ThRectangleEdgeComponent thRectangleEdgeComponent;
        private List<Point3d> points;
        //记录添加的纵筋点能组成哪种拉筋，1是link1箍筋轮廓，2是link2是拉筋水平，3是link3竖向，4是link4 >=300增加的点
        private List<int> pointsFlag;
        /// <summary>
        /// 一型钢筋确定点的位置,先去掉四角的点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        void CalReinforcePosition(int pointNum,Polyline polyline,ThRectangleEdgeComponent thRectangleEdgeComponent,double scale)
        {
            
            //纵筋相对轮廓的偏移值
            double offset = scale * (thRectangleEdgeComponent.C + 5) + thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight;
            //根据点的对数所有点的位置确定位置
            //polyline从左上角逆时针旋转，四个点先获取
            points=new List<Point3d>();
            for(int i=0;i<polyline.NumberOfVertices;i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i==0)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }
                //左下角，向右上偏移
                else if(i==1)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if(i==2)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if(i==3)
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
                double tmp = Helper.calVariance(numbers);
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
            for (int i=0;i<result;i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[3].X, points[3].Y - deltaY, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X+deltaX, points[0].Y , 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[1].X + deltaX, points[1].Y, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(3);
                pointsFlag.Add(3);
            }


        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }


        void CalLinkPosition(ThRectangleEdgeComponent thRectangleEdgeComponent)
        {
            //遍历所有点，找出2，3类型的钢筋，钢筋,同时查表,因为是一对对的点，所以每次加两个点
            for(int i=0;i<points.Count;i+=2)
            {
                if(pointsFlag[i]==2||pointsFlag[i]==3)
                {
                    if(thRectangleEdgeComponent.Link2.IsNullOrEmpty())
                    {
                        //不需要解析有几个，只有T字型Link3可能有选择的情况，直接绘制，利用第i，i+1个点
                        
                    }

                }
            }
        }

        void CalStirrupPosition()
        {

        }

        /// <summary>
        /// 计算得到的都是本地坐标默认，
        /// </summary>
        public void CalGangjinPosition()
        {
            foreach (var gangJin in GangJinBases)
            {
                //如果是纵筋
                if (gangJin.GangjinType == 0)
                {
                    //更新gangjin的值
                    //CalReinforcePosition();
                }
                //如果是箍筋
                else if (gangJin.GangjinType == 1)
                {
                    CalStirrupPosition();
                }
                //如果是拉筋
                else if (gangJin.GangjinType == 2)
                {
                    //CalLinkPosition();
                }
            }
        }

        public override void DrawOutline()
        {
            var pts = new Point3dCollection
            {
                TableStartPt + new Vector3d(450, -1500, 0) * scale,
                TableStartPt + new Vector3d(450, -1500 - thRectangleEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thRectangleEdgeComponent.Hc, -1500 - thRectangleEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thRectangleEdgeComponent.Hc, -1500, 0) * scale
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
    }
}
