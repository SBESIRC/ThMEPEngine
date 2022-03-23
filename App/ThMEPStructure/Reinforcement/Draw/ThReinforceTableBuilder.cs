using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Model;
using Autodesk.AutoCAD.Geometry;
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
            double firstRowH = 0, firstRowW = 0;
            double tmpH, tmpW;
            int cnt = 0;
            foreach(var component in components)
            {
                component.InitAndCalTableSize(this.elevation, this.tblRowHeight, scale,out tmpH, out tmpW);
                if(firstRowH<tmpH)
                {
                    firstRowH = tmpH;
                }
                if(firstRowW<tmpW)
                {
                    firstRowW = tmpW;
                }
                cnt++;
                //如果超出范围就绘制
                if(cnt*firstRowW>width)
                {
                    foreach(var componentRow in tmpComponents)
                    {
                        tmpColllection=componentRow.Draw(firstRowH, firstRowW, new Point3d(startX, startY, 0));
                        foreach(DBObject dBObject in tmpColllection)
                        {
                            objectCollection.Add(dBObject);
                        }
                        startX += firstRowW;
                    }
                    cnt = 0;
                    tmpComponents.Clear();
                    startY -= firstRowH + 4 * this.tblRowHeight;
                    startX = extents.MinPoint.X;
                }
                //否则就加入队列中
                else
                {
                    tmpComponents.Add(component);

                }
                
            }

            return objectCollection;
            throw new NotImplementedException();
        }
    }
}
