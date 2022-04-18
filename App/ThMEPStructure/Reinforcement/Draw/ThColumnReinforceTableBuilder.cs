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
    public class ThColumnReinforceTableBuilder
    {
        private Extents2d extents = new Extents2d();
        private string elevation = "";
        private double tblRowHeight;
        private string drawingScale = "";

        public ThColumnReinforceTableBuilder(Extents2d extents,string elevation, 
            string drawingScale,double tblRowHeight)
        {
            this.extents = extents;
            this.elevation = elevation;
            this.drawingScale = drawingScale;
            this.tblRowHeight = tblRowHeight;
        }
       
        public DBObjectCollection Build(List<ThColumnComponent> components)
        {
            DBObjectCollection objectCollection = new DBObjectCollection();
            DBObjectCollection tmpColllection = new DBObjectCollection();
            //整个区域的宽
            double width = extents.MaxPoint.X - extents.MinPoint.X;
            double startY = extents.MaxPoint.Y;
            double startX = extents.MinPoint.X;
            //提取放大尺寸
            double scale = Helper.CalScale(this.drawingScale);
            List<ThColumnComponent> tmpComponents = new List<ThColumnComponent>();
            //需要将组件分行，迭代计算一个队列里最大的长和宽，如果宽超过给的尺寸就停止添加到队列里，将最大的长和宽作为表格的截面尺寸
            double firstRowH = 0;
            double tmpH, tmpW;
            List<double> widths = new List<double>();
            double widthSum = 0;
            int cnt = 0;
            foreach (var component in components)
            {
                component.InitAndCalTableSize(this.elevation, this.tblRowHeight, scale, out tmpH, out tmpW);
                widthSum += tmpW;
                //如果超出范围就绘制
                if (widthSum > width)
                {
                    //绘制之前存的每一个
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
                    cnt = 0;
                    widthSum = 0;
                    widths.Clear();
                    tmpComponents.Clear();
                    startY -= firstRowH + 4 * this.tblRowHeight;
                    startX = extents.MinPoint.X;
                    firstRowH = tmpH;
                    widthSum = tmpW;

                }
                widths.Add(tmpW);
                tmpComponents.Add(component);
                if (firstRowH < tmpH)
                {
                    firstRowH = tmpH;
                }
            }
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
            throw new NotImplementedException();
        }
    }
}
