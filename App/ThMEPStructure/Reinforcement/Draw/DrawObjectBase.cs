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
        public string number;
        public Point3d TableStartPt;
        public Polyline Outline;
        public List<Curve> LinkedWallLines;
        public List<RotatedDimension> rotatedDimensions;
        public List<Polyline> Links = new List<Polyline>();

        public List<Point3d> points;    //纵筋位置
        //记录添加的纵筋点能组成哪种拉筋，1是link1箍筋轮廓，2是link2是拉筋水平，3是link3竖向，4是link4 >=300增加的点
        protected List<int> pointsFlag = new List<int>();
        public DBObjectCollection objectCollection;
        public double FirstRowHeight = 0;
        public double FirstRowWidth = 0;
        public string Reinforce;
        public string Stirrup;

        



        //根据轮廓的点来计算表格第一行的长宽
        protected void CalTableFirstRowHW(Polyline polyline,out double height,out double width)
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
            height = (yMax - yMin) * 3 + 1000;
            width = Math.Max((xMax - xMin) * 1.2 + 1000, 5000);
        }


        /// <summary>
        /// 绘制表格,输入起始点位置，行宽，第一个行装截面的表格的长款
        /// 输出：绘制13个点的多段线表格,填装文字
        /// </summary>
        protected DBObjectCollection DrawTable(double rowHeight,double firstRowHeight,double firstRowWidth)
        {
            DBObjectCollection objectCollection = new DBObjectCollection();
            Polyline polyline = new Polyline();
            Point2d point;
            //先画四个角
            //计算总长度
            double totalHeight = 4 * rowHeight + firstRowHeight;
            
            polyline.AddVertexAt(0, TableStartPt.ToPoint2D(), 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y);
            polyline.AddVertexAt(1, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - totalHeight);
            polyline.AddVertexAt(2, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - totalHeight);
            polyline.AddVertexAt(3, point, 0, 0, 0);
            polyline.AddVertexAt(4, TableStartPt.ToPoint2D(), 0, 0, 0);

            //逐步把每行画出来
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight);
            polyline.AddVertexAt(5, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight);
            polyline.AddVertexAt(6, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight);
            polyline.AddVertexAt(7, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight);
            polyline.AddVertexAt(8, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight*2);
            polyline.AddVertexAt(9, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 2);
            polyline.AddVertexAt(10, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 3);
            polyline.AddVertexAt(11, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X , TableStartPt.Y - firstRowHeight - rowHeight * 3);
            polyline.AddVertexAt(12, point, 0, 0, 0);
            objectCollection.Add(polyline);
            //填装文字，文字行款固定为300，第一行编号，第二行标高，第三行纵筋，第四行箍筋，位置放在 Y从上往下800/2-150的位置上 X计算居中？
            //第一个文字编号
            DBText dBText = new DBText();
            dBText.TextString = number;
            dBText.Height = 300;
            
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight, 300, number);
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = elevation;
            dBText.Height = 300;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 2, 300, elevation);
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = Reinforce;
            dBText.Height = 300;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 2, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 3, 300, Reinforce);
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = Stirrup;
            dBText.Height = 300;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 3, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight  - rowHeight * 4, 300, Stirrup);
            objectCollection.Add(dBText);
            return objectCollection;
        }

        /// <summary>
        /// 绘制轮廓
        /// </summary>
        public abstract void DrawOutline();

       
        
        /// <summary>
        /// 绘制轮廓尺寸
        /// </summary>
        public abstract void DrawDim();
        /// <summary>
        /// 绘制连接墙体
        /// </summary>
        public abstract void DrawWall();
        
        protected abstract void CalReinforcePosition(int pointNum, Polyline polyline);
        protected abstract void CalLinkPosition();
        protected abstract void CalStirrupPosition();
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
            Polyline tmp = pts.CreatePolyline();
            tmp.Closed = false;
            return tmp;
        }

        public void CalAndDrawGangJin(double H,double W, ThEdgeComponent component,Point3d point)
        {
            this.TableStartPt = point;
            //init(component,elevation,tblRowHeight,scale,position);
            SetFirstRowH(H);
            CalGangjinPosition(component);
            DrawGangJin(component);
        }

        public abstract void init(ThEdgeComponent component, string elevation, double tblRowHeight, double scale, Point3d position);

        protected void CalGangjinPosition(ThEdgeComponent component)
        {

            //重新计算轮廓得到polyline
            DrawOutline();
            ////计算表格轮廓
            //CalTableFirstRowHW(Outline, out firstRowHeight, out firstRowWidth);

            //统计纵筋的数量
            StrToReinforce strToReinforce = new StrToReinforce();
            if (component is ThLTypeEdgeComponent)
            {
                ThLTypeEdgeComponent thLTypeEdgeComponent = component as ThLTypeEdgeComponent;
                strToReinforce = Helper.StrToRein(thLTypeEdgeComponent.Reinforce);
            }
            else if(component is ThRectangleEdgeComponent)
            {
                ThRectangleEdgeComponent thRectangleEdgeComponent = component as ThRectangleEdgeComponent;
                strToReinforce = Helper.StrToRein(thRectangleEdgeComponent.Reinforce);
            }
            else if(component is ThTTypeEdgeComponent)
            {
                ThTTypeEdgeComponent thTTypeEdgeComponent = component as ThTTypeEdgeComponent;
                strToReinforce = Helper.StrToRein(thTTypeEdgeComponent.Reinforce);
            }
            
            int pointNum = strToReinforce.num;
            //计算纵筋位置
            CalReinforcePosition(pointNum, Outline);
            //计算箍筋位置
            CalStirrupPosition();
            //计算拉筋位置
            CalLinkPosition();

        }


        /// <summary>
        /// 获得表格第一行（截面）长和宽
        /// </summary>
        public void GetTableFirstRowHW(out double firstRowHeight, out double firstRowWidth)
        {
            //计算轮廓得到polyline
            DrawOutline();
            CalTableFirstRowHW(Outline, out firstRowHeight, out firstRowWidth);
            //this.FirstRowHeight = firstRowHeight;
            this.FirstRowWidth = firstRowWidth;
        }

        public void SetFirstRowH(double firstRowHeight)
        {
            this.FirstRowHeight = firstRowHeight;
            //this.FirstRowWidth = firstRowWidth;
        }

        protected void DrawGangJin(ThEdgeComponent component)
        {
            objectCollection = new DBObjectCollection();

            //绘制表格
            DBObjectCollection tableCollection = new DBObjectCollection();
            tableCollection = DrawTable(tblRowHeight, FirstRowHeight, FirstRowWidth);
            foreach (DBObject element in tableCollection)
            {
                objectCollection.Add(element);
            }
            //绘制纵筋
            foreach (var point in points)
            {
                GangJinReinforce gangJinReinforce = new GangJinReinforce
                {
                    GangjinType = 0,
                    point = point,
                    r = component.PointReinforceLineWeight
                };
                var rein = gangJinReinforce.DrawReinforce();
                rein.Layer = "REIN";
                objectCollection.Add(rein);
            }
            //绘制箍筋、拉筋
            foreach (var link in Links)
            {
                link.Layer = "LINK";
                objectCollection.Add(link);
            }

            //轮廓、尺寸、墙体
            Outline.Layer = "COLU_DE_TH";
            objectCollection.Add(Outline);
            DrawDim();
            foreach (var dimension in rotatedDimensions)
            {
                dimension.Layer = "COLU_DE_DIM";
                objectCollection.Add(dimension);
            }
            DrawWall();
            foreach (var wallLine in LinkedWallLines)
            {
                wallLine.Layer = "THIN";
                objectCollection.Add(wallLine);
            }

        }


    }
}
