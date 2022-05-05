using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPStructure.Reinforcement.Service;
using Dreambuild.AutoCAD;
using AcHelper;

namespace ThMEPStructure.Reinforcement.Draw
{
    public class ThReinforceTableBuilder
    {
        private Extents2d extents = new Extents2d();
        private string elevation = "";
        private double tblRowHeight;
        private string drawingScale = "";

        public ThReinforceTableBuilder(Extents2d extents,string elevation, 
            string drawingScale,double tblRowHeight)
        {
            this.extents = extents;
            this.elevation = elevation;
            this.drawingScale = drawingScale;
            this.tblRowHeight = tblRowHeight;
        }

        private DBObjectCollection DrawTableHead(Point3d TableStartPt, double firstRowHeight)
        {
            DBObjectCollection objectCollection = new DBObjectCollection();
            double firstRowWidth = 1500;
            Polyline polyline = new Polyline();
            Point2d point;
            //先画四个角
            //计算总长度
            double totalHeight = 4 * tblRowHeight + firstRowHeight;

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
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight);
            polyline.AddVertexAt(7, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight);
            polyline.AddVertexAt(8, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight * 2);
            polyline.AddVertexAt(9, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight * 2);
            polyline.AddVertexAt(10, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight * 3);
            polyline.AddVertexAt(11, point, 0, 0, 0);
            point = new Point2d(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight * 3);
            polyline.AddVertexAt(12, point, 0, 0, 0);

            polyline.LayerId = DbHelper.GetLayerId("tab-表头");
            //polyline.Linetype = "ByBlock";
            objectCollection.Add(polyline);
            //填装文字
            DBText dBText = new DBText();
            
            dBText.TextString = "截面";
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight, 300, "截面");
            dBText.TextStyleId = DbHelper.GetTextStyleId("BW_Rein");
            dBText.LayerId = DbHelper.GetLayerId("tab-表头");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = "编号";
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight, 300, "编号");
            dBText.TextStyleId = DbHelper.GetTextStyleId("BW_Rein");
            dBText.LayerId = DbHelper.GetLayerId("tab-表头");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = "标高";
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight * 2, 300, "标高");
            dBText.TextStyleId = DbHelper.GetTextStyleId("BW_Rein");
            dBText.LayerId = DbHelper.GetLayerId("tab-表头");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = "纵筋";
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight * 2, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight * 3, 300, "纵筋");
            dBText.TextStyleId = DbHelper.GetTextStyleId("BW_Rein");
            dBText.LayerId = DbHelper.GetLayerId("tab-表头");
            objectCollection.Add(dBText);
            dBText = new DBText();
            dBText.TextString = "箍筋/拉筋";
            dBText.Height = 300;
            dBText.WidthFactor = 0.7;
            dBText.Position = Helper.CalCenterPosition(TableStartPt.X, TableStartPt.Y - firstRowHeight - tblRowHeight * 3, TableStartPt.X + firstRowWidth, TableStartPt.Y - firstRowHeight - tblRowHeight * 4, 300, "箍筋/拉筋");
            dBText.TextStyleId = DbHelper.GetTextStyleId("BW_Rein");
            dBText.LayerId = DbHelper.GetLayerId("tab-表头");
            objectCollection.Add(dBText);
            return objectCollection;
        }

        public DBObjectCollection Build(List<ThEdgeComponent> components)
        {
            DBObjectCollection objectCollection=new DBObjectCollection();
            DBObjectCollection tmpColllection = new DBObjectCollection();
            //整个区域的宽
            double width = extents.MaxPoint.X - extents.MinPoint.X;
            double startY = extents.MaxPoint.Y;
            double startX = extents.MinPoint.X;
            //提取放大尺寸
            double scale = Helper.CalScale(this.drawingScale);
            List<ThEdgeComponent> tmpComponents = new List<ThEdgeComponent>();
            //需要将组件分行，迭代计算一个队列里最大的长和宽，如果宽超过给的尺寸就停止添加到队列里，将最大的长和宽作为表格的截面尺寸
            double firstRowH = 0;
            double tmpH, tmpW;
            List<double> widths = new List<double>();
            double widthSum = 1500;
            int cnt = 0;
            foreach(var component in components)
            {
                component.InitAndCalTableSize(this.elevation, this.tblRowHeight, scale,out tmpH, out tmpW);
                widthSum += tmpW;
                //如果超出范围就绘制
                if(widthSum>width)
                {
                    //绘制表头
                    tmpColllection = DrawTableHead(new Point3d(startX, startY, 0), firstRowH);
                    foreach (DBObject dBObject in tmpColllection)
                    {
                        objectCollection.Add(dBObject);
                    }
                    startX += 1500;
                    //绘制之前存的每一个
                    foreach (var componentRow in tmpComponents)
                    {
                        tmpColllection=componentRow.Draw(firstRowH, widths[cnt], new Point3d(startX, startY, 0));
                        foreach(DBObject dBObject in tmpColllection)
                        {
                            objectCollection.Add(dBObject);
                        }
                        startX += widths[cnt];
                        cnt++;
                    }
                    cnt = 0;
                    widthSum = 1500;
                    widths.Clear();
                    tmpComponents.Clear();
                    startY -= firstRowH + 4 * this.tblRowHeight;
                    startX = extents.MinPoint.X;
                    firstRowH = tmpH;
                    widthSum = tmpW;
                    
                }
                widths.Add(tmpW);
                tmpComponents.Add(component);
                if(firstRowH<tmpH)
                {
                    firstRowH = tmpH;
                }
            }
            //剩下的统一绘制
            //绘制表头
            tmpColllection = DrawTableHead(new Point3d(startX, startY, 0), firstRowH);
            foreach (DBObject dBObject in tmpColllection)
            {
                objectCollection.Add(dBObject);
            }
            startX += 1500;
            foreach (var componentRow in tmpComponents)
            {
                tmpColllection = componentRow.Draw(firstRowH, widths[cnt], new Point3d(startX, startY, 0));
                foreach (DBObject dBObject in tmpColllection)
                {
                    objectCollection.Add(dBObject);
                }
                startX += widths[cnt];
                cnt++;
            }

            return objectCollection;
        }
    }
}
