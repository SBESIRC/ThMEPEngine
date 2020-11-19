﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHAVC.Duct
{
    public enum TypeOfThDraught
    {
        OnSide = 0,
        OnBelow = 1
    }
    public class ThDraughtParameters
    {
        public int DraughtWidth { get; set; }
        public int DraughtLength { get; set; }
        public double DraughtVolume { get; set; }
        public Point3d CenterPosition { get; set; }
        public double RotateAngle { get; set; }
        public TypeOfThDraught DraughtType { get; set; }
    }
    public class ThDraught
    {
        public DBObjectCollection Geometries { get; set; }
        public ThDraughtParameters Parameters { get; set; }

        public ThDraught(ThDraughtParameters parameters)
        {
            Parameters = parameters;
        }
    }
}
