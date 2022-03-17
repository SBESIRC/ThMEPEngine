using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
namespace ThMEPStructure.Reinforcement.Draw
{
    abstract class DrawObjectBase
    {
        public void DrawGangJin()
        {
            foreach(var gangJin in GangJinBases)
            {
                gangJin.Draw();
            }
        }

        //根据轮廓的点来计算表格第一行的长宽
        public void calTableFirstRowHW(Polyline polyline, double scale,out double height,out double width)
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
        public void  DrawTable(Point3d position,double height,double rowHeight,double firstRowHeight,double firstRowWidth)
        {
            Polyline polyline = new Polyline();
            Point2d point;
            //先画四个角
            //计算总长度
            double totalHeight = 4 * rowHeight + firstRowHeight;
            
            polyline.AddVertexAt(0, position.ToPoint2D(), 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y);
            polyline.AddVertexAt(1, point, 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y + totalHeight);
            polyline.AddVertexAt(2, point, 0, 0, 0);
            point = new Point2d(position.X, position.Y + totalHeight);
            polyline.AddVertexAt(3, point, 0, 0, 0);
            polyline.AddVertexAt(4, position.ToPoint2D(), 0, 0, 0);

            //逐步把每行画出来
            point = new Point2d(position.X, position.Y + firstRowHeight);
            polyline.AddVertexAt(5, point, 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y + firstRowHeight);
            polyline.AddVertexAt(6, point, 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y + firstRowHeight+rowHeight);
            polyline.AddVertexAt(7, point, 0, 0, 0);
            point = new Point2d(position.X, position.Y + firstRowHeight + rowHeight);
            polyline.AddVertexAt(8, point, 0, 0, 0);
            point = new Point2d(position.X, position.Y + firstRowHeight + rowHeight*2);
            polyline.AddVertexAt(9, point, 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y + firstRowHeight + rowHeight * 2);
            polyline.AddVertexAt(10, point, 0, 0, 0);
            point = new Point2d(position.X + firstRowWidth, position.Y + firstRowHeight + rowHeight * 3);
            polyline.AddVertexAt(11, point, 0, 0, 0);
            point = new Point2d(position.X , position.Y + firstRowHeight + rowHeight * 3);
            polyline.AddVertexAt(12, point, 0, 0, 0);

            //填装文字，文字行款固定为300，第一行编号，第二行标高，第三行纵筋，第四行箍筋，位置放在 Y从上往上800/2-150的位置上 X计算居中？

        }

        /// <summary>
        /// 绘制轮廓
        /// </summary>
        /// <param name="drawingScale"></param>
        public abstract void DrawOutline(string drawingScale);

        public Point3d TableStartPt;
        public Polyline Outline;
        public List<GangJinBase> GangJinBases;
        

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
    }
}
