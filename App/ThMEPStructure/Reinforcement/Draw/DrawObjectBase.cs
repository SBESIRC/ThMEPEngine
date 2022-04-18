using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Model;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
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
        public List<int> LinksFlag = new List<int>();        //标记不同LINK的类型
        public List<Point3d> points;    //纵筋位置
        //记录添加的纵筋点能组成哪种拉筋，1是link1箍筋轮廓，2是link2是拉筋水平，3是link3竖向，4是link4 >=300增加的点
        protected List<int> pointsFlag = new List<int>();
        public DBObjectCollection objectCollection = new DBObjectCollection();
        public double FirstRowHeight = 0;
        public double FirstRowWidth = 0;
        public string Reinforce;
        public string Stirrup;
        public List<Polyline> LabelAndRect=new List<Polyline>();//存放C筋标注的多段线以及引线
        public List<DBText> CJintText = new List<DBText>();//存放C筋标注的文本信息
        



        //根据轮廓的点来计算表格第一行的长宽
        protected void CalTableFirstRowHW(Polyline polyline, out double height, out double width)
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
            height = (yMax - yMin) * 2 + 5000;
            width = Math.Max((xMax - xMin) * 1.5 + 2500, 5000);
        }


        /// <summary>
        /// 绘制表格,输入起始点位置，行宽，第一个行装截面的表格的长款
        /// 输出：绘制13个点的多段线表格,填装文字
        /// </summary>
        protected DBObjectCollection DrawTable(double rowHeight, double firstRowHeight, double firstRowWidth)
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
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 2);
            polyline.AddVertexAt(9, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 2);
            polyline.AddVertexAt(10, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 3);
            polyline.AddVertexAt(11, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 3);
            polyline.AddVertexAt(12, point, 0, 0, 0);
            
            polyline.LayerId = DbHelper.GetLayerId("TAB");
            objectCollection.Add(polyline);
            //填装文字，文字行款固定为300，第一行编号，第二行标高，第三行纵筋，第四行箍筋，位置放在 Y从上往下800/2-150的位置上 X计算居中？
            //第一个文字编号
            DBText dBText = new DBText();
            dBText.TextString = number;
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight, 300, number);
            dBText.TextStyleId = DbHelper.GetTextStyleId("TSSD_REIN");
            dBText.LayerId = DbHelper.GetLayerId("TAB_TEXT");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = elevation;
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 2, 300, elevation);
            dBText.TextStyleId = DbHelper.GetTextStyleId("TSSD_REIN");
            dBText.LayerId = DbHelper.GetLayerId("TAB_TEXT");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = Reinforce.Replace("C", "%%132");
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 2, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 3, 300, Reinforce);
            dBText.TextStyleId = DbHelper.GetTextStyleId("TSSD_REIN");
            dBText.LayerId = DbHelper.GetLayerId("TAB_TEXT");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = Stirrup.Replace("C", "%%132");
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - rowHeight * 3, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - rowHeight * 4, 300, Stirrup);
            dBText.TextStyleId = DbHelper.GetTextStyleId("TSSD_REIN");
            dBText.LayerId = DbHelper.GetLayerId("TAB_TEXT");
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
        /// <summary>
        /// 绘制爆炸图
        /// </summary>
        public abstract void CalExplo();
        protected abstract void CalReinforcePosition(int pointNum, Polyline polyline);
        protected abstract void CalLinkPosition();
        protected abstract void CalStirrupPosition();

        public abstract void DrawCJin();
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

        public void CalAndDrawGangJin(double H, double W, ThEdgeComponent component, Point3d point)
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
            else if (component is ThRectangleEdgeComponent)
            {
                ThRectangleEdgeComponent thRectangleEdgeComponent = component as ThRectangleEdgeComponent;
                strToReinforce = Helper.StrToRein(thRectangleEdgeComponent.Reinforce);
            }
            else if (component is ThTTypeEdgeComponent)
            {
                ThTTypeEdgeComponent thTTypeEdgeComponent = component as ThTTypeEdgeComponent;
                strToReinforce = Helper.StrToRein(thTTypeEdgeComponent.Reinforce);
            }

            int pointNum = strToReinforce.num;

            

            //计算纵筋位置
            CalReinforcePosition(pointNum, Outline);
            DrawCJin();

            //计算箍筋位置
            CalStirrupPosition();
            //计算拉筋位置
            CalLinkPosition();
            //计算并绘制爆炸图
            CalExplo();
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
                link.LayerId = DbHelper.GetLayerId("LINK");
                objectCollection.Add(link);
            }

            //轮廓、墙体、尺寸
            Outline.LayerId = DbHelper.GetLayerId("COLU_DE_TH");
            objectCollection.Add(Outline);
            DrawWall();
            foreach (var wallLine in LinkedWallLines)
            {
                wallLine.LayerId = DbHelper.GetLayerId("THIN");
                objectCollection.Add(wallLine);
            }
            DrawDim();
            foreach (var dimension in rotatedDimensions)
            {
                dimension.LayerId = DbHelper.GetLayerId("COLU_DE_DIM");
                dimension.DimensionStyle = DbHelper.GetDimstyleId("TSSD_25_100");
                objectCollection.Add(dimension);
            }
            
            foreach (var label in LabelAndRect)
            {
                //label.Layer = "CJLaebl";
                objectCollection.Add(label);             
            }

            foreach(var txt in CJintText)
            {
                //txt.Layer = "CJText";
                objectCollection.Add(txt);
            }

        }
        /// <summary>
        /// 绘制小图标注
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="flag">拉筋方向，true表示横向，false表示纵向</param>
        /// <param name="text">文字标注</param>
        /// <param name="length1">标注连线出头长度，负值表示负方向</param>
        /// <param name="length2">标注折线长度，负值表示负方向</param>
        protected void DrawLinkLabel(List<Point2d> pts, bool flag, string text, double length1, double length2)
        {
            DBText dbText = new DBText();
            string s;
            if (pts.Count == 1) s = text.Substring(1);
            else s = text;
            dbText.TextString = s.Replace("C", "%%132");
            dbText.Height = 300;
            if (pts.Count != 1)
            {
                Helper.Sort(pts, flag);
                if (length1 < 0) pts.Reverse();
            }
            Polyline label = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                Polyline tmp = Helper.LinkMark(pts[i]);
                tmp.LayerId = DbHelper.GetLayerId("LABEL");
                objectCollection.Add(tmp);
                label.AddVertexAt(i, pts[i], 0, 0, 0);
            }
            if (pts.Count == 0)
            {
                return;//修改
            }
            Point2d pt = pts[pts.Count - 1];
            if (flag)
            {
                pt += new Vector2d(0, length1);
            }
            else
            {
                pt += new Vector2d(length1, 0);
            }
            label.AddVertexAt(pts.Count, pt, 0, 0, 0);
            if (flag)
            {
                pt += new Vector2d(length2, 0);
                if (length2 > 0)
                {
                    dbText.Position = new Point3d(pt.X - 1400, pt.Y, 0);
                }
                else
                {
                    dbText.Position = new Point3d(pt.X, pt.Y, 0);
                }
                dbText.Rotation = 0;
            }
            else
            {
                pt += new Vector2d(0, length2);
                if (length2 > 0)
                {
                    dbText.Position = new Point3d(pt.X, pt.Y - 1400, 0);
                }
                else
                {
                    dbText.Position = new Point3d(pt.X, pt.Y, 0);
                }
                dbText.Rotation = Math.PI / 2;
            }
            label.AddVertexAt(pts.Count + 1, pt, 0, 0, 0);
            label.LayerId = DbHelper.GetLayerId("LABEL");
            objectCollection.Add(label);
            dbText.TextStyleId = DbHelper.GetTextStyleId("TSSD_REIN");
            dbText.LayerId = DbHelper.GetLayerId("TAB_TEXT");
            dbText.WidthFactor = 0.7;
            objectCollection.Add(dbText);
        }
        /// <summary>
        /// 调整小图拉筋标注引线起点位置
        /// </summary>
        /// <param name="path">每次调整的步幅</param>
        /// <param name="plinks">所有拉筋</param>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        protected Point2d AdjustPos(Vector2d path, List<List<Polyline>> plinks, int index1, int index2)
        {
            Point2d p1 = plinks[index1][index2].GetPoint2dAt(2),
                p2 = plinks[index1][index2].GetPoint2dAt(3);
            var pt = Helper.MidPointOf(p1, p2);
            List<int> step = new List<int>();
            for(int i = 0; i < p1.GetDistanceTo(p2) / path.Length / 2; i++)
            {
                step.Add(i);
                step.Add(-i);
            }
            bool flag = false;
            for(int i = 0; i < step.Count; i++)
            {
                for(int j = 0; j < plinks.Count; j++)
                {
                    if (index1 == j) continue;
                    foreach(var link in plinks[j])
                    {
                        if (Helper.IsIncluded(pt, link))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag) break;
                }
                if (flag)
                {
                    flag = false;
                    pt = Helper.MidPointOf(p1, p2) + path * step[i];
                }
                else break;
            }
            return pt;
        }
    }
}
