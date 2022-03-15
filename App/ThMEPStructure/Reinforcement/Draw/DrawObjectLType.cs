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
    class DrawObjectLType:DrawObjectBase
    {
        
        ThLTypeEdgeComponent thLTypeEdgeComponent;
        public override void DrawOutline(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            var pts = new Point3dCollection
            {
                TableStartPt + new Vector3d(450, -1000, 0) * scale,
                TableStartPt + new Vector3d(450, -1000 - thLTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(450, -1000 - thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf, -1000 - thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -1000 - thLTypeEdgeComponent.Hc1 - thLTypeEdgeComponent.Bw, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf + thLTypeEdgeComponent.Hc2, -1000 - thLTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf, -1000 - thLTypeEdgeComponent.Hc1, 0) * scale,
                TableStartPt + new Vector3d(450 + thLTypeEdgeComponent.Bf, -1000, 0) * scale
            };
            Outline = pts.CreatePolyline();
        }

        public override void DrawWall(string drawingScale)
        {
            int scale = 100 / int.Parse(drawingScale.Substring(2));
            if (thLTypeEdgeComponent.Type == "A")
            {
                Point3d pt1 = Outline.GetPoint3dAt(0), pt2 = Outline.GetPoint3dAt(7);
                double bf = thLTypeEdgeComponent.Bf * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(0, bf * 5 / 8.0, 0), out Line line1, out Line line2);

                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
            if (thLTypeEdgeComponent.LinkWallPos == "2" || thLTypeEdgeComponent.Type == "B")
            {
                Point3d pt1 = Outline.GetPoint3dAt(4), pt2 = Outline.GetPoint3dAt(5);
                double bw = thLTypeEdgeComponent.Bw * scale;
                Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);

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
                XLine1Point = Outline.GetPoint3dAt(0),
                XLine2Point = Outline.GetPoint3dAt(1),
                DimLinePoint = Outline.GetPoint3dAt(0) + new Vector3d(-600, 0, 0),
                DimensionText = thLTypeEdgeComponent.Hc1.ToString(),
                Rotation = Math.PI / 2.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(1),
                XLine2Point = Outline.GetPoint3dAt(2),
                DimLinePoint = Outline.GetPoint3dAt(1) + new Vector3d(-600, 0, 0),
                DimensionText = thLTypeEdgeComponent.Bw.ToString(),
                Rotation = Math.PI / 2.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(2),
                XLine2Point = Outline.GetPoint3dAt(3),
                DimLinePoint = Outline.GetPoint3dAt(2) + new Vector3d(0, -600, 0),
                DimensionText = thLTypeEdgeComponent.Bf.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(3),
                XLine2Point = Outline.GetPoint3dAt(4),
                DimLinePoint = Outline.GetPoint3dAt(3) + new Vector3d(0, -600, 0),
                DimensionText = thLTypeEdgeComponent.Hc2.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);
        }

        /// <summary>
        /// L型钢筋确定位置，轮廓上八个点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        void CalReinforcePosition(int pointNum,ThLTypeEdgeComponent thLTypeEdgeComponent,Polyline polyline,double scale)
        {
            //存储结果
            List<Point3d> points = new List<Point3d>();
            //纵筋相对轮廓的偏移值
            double offset = scale * (thLTypeEdgeComponent.C + 5) + thLTypeEdgeComponent.PointReinforceLineWeight + thLTypeEdgeComponent.StirrupLineWeight;
            //根据八个轮廓上的点的位置计算纵筋位置
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i == 0||i==1)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }
                
                //左下角，向右上偏移
                else if (i == 2)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if (i == 3||i==4)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if (i >= 5&&i<=7)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y - offset, 0);
                }
                else
                {
                    break;
                }
                points.Add(tmpPoint);
            }
            
            //底层需要添加额外的纵筋，1，2两点中点，4，5中点
            if (thLTypeEdgeComponent.Bw>=300)
            {
                Point3d tmpPoint = new Point3d((points[1].X + points[2].X) / 2.0, (points[1].Y + points[2].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[4].X + points[5].X) / 2.0, (points[4].Y + points[5].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
            }
            
            //左侧需要添加额外的钢筋,0,7中点，2，3中点
            if(thLTypeEdgeComponent.Bf>=300)
            {
                Point3d tmpPoint = new Point3d((points[0].X + points[7].X) / 2.0, (points[0].Y + points[7].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
                tmpPoint = new Point3d((points[2].X + points[3].X) / 2.0, (points[2].Y + points[3].Y) / 2.0, 0.0);
                points.Add(tmpPoint);
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
            //把一对对点的位置计算出来，计算竖直方向上的位置,0，7号点每次向下偏移deltaY,水平方向3，6号点每次向右偏移deltaX
            for (int i = 0; i < result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[7].X, points[7].Y - deltaY, 0);
                points.Add(tmpPoint2);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[3].X + deltaX, points[3].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[6].X + deltaX, points[6].Y, 0);
                points.Add(tmpPoint2);
            }
        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }


        void CalLinkPosition()
        {

        }

        void CalStirrupPosition()
        {

        }

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
                    CalLinkPosition();
                }
            }
        }
    }
}
