using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Model;

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
            //提取放大尺寸
            double scale = Helper.CalScale(this.drawingScale);
            List<ThEdgeComponent> tmpComponents = new List<ThEdgeComponent>();
            //需要将组件分行，迭代计算一个队列里最大的长和宽，如果宽超过给的尺寸就停止添加到队列里，将最大的长和宽作为表格的截面尺寸
            foreach(var component in components)
            {
                component.Draw(this.elevation, this.tblRowHeight, scale);
            }

            throw new NotImplementedException();
        }
    }
}
