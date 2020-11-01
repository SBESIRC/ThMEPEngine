﻿using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public static class ThFanSelectionGeExtension
    {
        public static double Width(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.X - extents.MinPoint.X);
        }

        public static double Height(this Extents3d extents)
        {
            return Math.Abs(extents.MaxPoint.Y - extents.MinPoint.Y);
        }
    }
}
