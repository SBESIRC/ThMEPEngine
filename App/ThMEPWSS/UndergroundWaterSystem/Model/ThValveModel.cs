﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public class ThValveModel
    {
        public ThValveModel(BlockReference valve)
        {
            Valve = valve;
            if (valve.Bounds is Extents3d ex)
            {
                Point = ex.CenterPoint();
            }
        }
        public ThValveModel()
        {

        }
        public BlockReference Valve;
        public Point3d Point;
        public double CorrespondingPipeLineLength = 400;
        public bool Existed = false;
    }
}