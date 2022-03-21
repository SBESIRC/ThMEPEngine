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
            foreach(var component in components)
            {
                component.Draw("1.0-2.0", 800, scale);

            }

            throw new NotImplementedException();
        }
    }
}
