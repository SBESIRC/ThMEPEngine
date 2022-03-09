﻿using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Draw
{
    public class ThReinforceTableBuilder
    {
        private string frame = "";
        private string elevation = "";
        private double tblRowHeight;
        private string drawingScale = "";

        public ThReinforceTableBuilder(string frame,string elevation, 
            string drawingScale,double tblRowHeight)
        {
            this.frame = frame;
            this.elevation = elevation;
            this.drawingScale = drawingScale;
            this.tblRowHeight = tblRowHeight;   
        }

        public DBObjectCollection Build(List<ThEdgeComponent> components)
        {
            throw new NotImplementedException();
        }
    }
}
