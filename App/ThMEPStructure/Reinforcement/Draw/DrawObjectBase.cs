using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    abstract class DrawObjectBase
    {
        
        public double scale;
        public string elevation;
        public double tblRowHeight;
        public Point3d TableStartPt;
        public Polyline Outline;
        public List<Curve> LinkedWallLines;
        public List<RotatedDimension> rotatedDimensions;
        public List<GangJinBase> GangJinBases;
        public DBObjectCollection objectCollection;

        //根据轮廓的点来计算表格第一行的长宽
        public void calTableFirstRowHW(Polyline polyline,out double height,out double width)
        {
            //遍历所有点，找到最小的矩形包围盒，再计算图上大小之后，再对宽度放大三倍，高度放大四倍
            double xMin = 1e10;
            double yMin = 1e10;
            double xMax = -1e10;
            double yMax = -1e10;

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                if (point.X > xMax) xMax = point.X;
                if (point.X < xMin) xMin = point.X;
                if (point.Y > yMax) yMax = point.Y;
                if (point.Y < yMin) yMin = point.Y;
            }
            height = (yMax - yMin) * scale * 4;
            width = (xMax - xMin) * scale * 3;
        }


        /// <summary>
        /// 绘制表格,输入起始点位置，行宽，第一个行装截面的表格的长款
        /// 输出：绘制13个点的多段线表格,填装文字
        /// </summary>
        public void  DrawTable(double rowHeight,double firstRowHeight,double firstRowWidth)
        {
            Polyline polyline = new Polyline();
            Point2d point;
            //先画四个角
            //计算总长度
            double totalHeight = 4 * rowHeight + firstRowHeight;
            
            polyline.AddVertexAt(0, TableStartPt.ToPoint2D(), 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y);
            polyline.AddVertexAt(1, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y + totalHeight);
            polyline.AddVertexAt(2, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y + totalHeight);
            polyline.AddVertexAt(3, point, 0, 0, 0);
            polyline.AddVertexAt(4, TableStartPt.ToPoint2D(), 0, 0, 0);

            //逐步把每行画出来
            point = new Point2d(TableStartPt.X, TableStartPt.Y + firstRowHeight);
            polyline.AddVertexAt(5, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y + firstRowHeight);
            polyline.AddVertexAt(6, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y + firstRowHeight+rowHeight);
            polyline.AddVertexAt(7, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y + firstRowHeight + rowHeight);
            polyline.AddVertexAt(8, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y + firstRowHeight + rowHeight*2);
            polyline.AddVertexAt(9, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y + firstRowHeight + rowHeight * 2);
            polyline.AddVertexAt(10, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y + firstRowHeight + rowHeight * 3);
            polyline.AddVertexAt(11, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X , TableStartPt.Y + firstRowHeight + rowHeight * 3);
            polyline.AddVertexAt(12, point, 0, 0, 0);

            //填装文字，文字行款固定为300，第一行编号，第二行标高，第三行纵筋，第四行箍筋，位置放在 Y从上往上800/2-150的位置上 X计算居中？

        }

        /// <summary>
        /// 绘制轮廓
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawOutline();

       
        
        /// <summary>
        /// 绘制轮廓尺寸
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawDim();
        /// <summary>
        /// 绘制连接墙体
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawWall();

        


        //初始化钢筋列表
        public void Init(ThLTypeEdgeComponent thLTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThRectangleEdgeComponent thRectangleEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public void Init(ThTTypeEdgeComponent thTTypeEdgeComponent)
        {
            GangJinBases = new List<GangJinBase>();
            GangJinStirrup gangJinStirrup = new GangJinStirrup();
            GangJinBases.Add(gangJinStirrup);
            GangJinReinforce gangJinReinforce = new GangJinReinforce();
            GangJinBases.Add(gangJinReinforce);
        }

        public Polyline GenPouDuan(Point3d pt1, Point3d pt2, Point3d linePt, out Line line1, out Line line2)
        {
            double length = pt1.DistanceTo(pt2);
            Vector3d direction = linePt - (pt1 + (pt2 - pt1).GetNormal().DotProduct(linePt - pt1) * (pt2 - pt1).GetNormal());
            line1 = new Line
            {
                StartPoint = pt1,
                EndPoint = pt1 + direction.GetNormal() * length * 5 / 8.0
            };
            line2 = new Line
            {
                StartPoint = pt2,
                EndPoint = pt2 + direction.GetNormal() * length * 5 / 8.0
            };
            var pts = new Point3dCollection
            {
                pt1 + direction.GetNormal() * length * 5 / 8.0 - (pt2 - pt1) * 3 / 8.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 3 / 8.0,
                pt1 + direction.GetNormal() * length * 7 / 8.0 + (pt2 - pt1) * 7 / 16.0,
                pt1 + direction.GetNormal() * length * 3 / 8.0 + (pt2 - pt1) * 9 / 16.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 5 / 8.0,
                pt1 + direction.GetNormal() * length * 5 / 8.0 + (pt2 - pt1) * 11 / 8.0
            };
            return pts.CreatePolyline();
        }
    }
}
