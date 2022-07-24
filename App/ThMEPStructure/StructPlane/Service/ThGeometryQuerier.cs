﻿using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThMEPStructure.StructPlane.Service
{
    internal static class ThGeometryQuerier
    {
        private const string UpperFloorColumnColor = "#7f3f3f";
        private const string BelowFloorColumnColor1 = "#ff0000";
        private const string BelowFloorColumnColor2 = "Red";

        private const string UpperFloorShearWallColor = "#ff7f00";
        private const string BelowFloorShearWallColor1 = "#ffff00";
        private const string BelowFloorShearWallColor2 = "Yellow";

        public static List<string> GetSlabElevations(this List<ThGeometry> geos)
        {
            var groups = GetSlabGeos(geos)
                .Select(g => g.Properties.GetElevation())
                .Where(g => !string.IsNullOrEmpty(g))
                .GroupBy(o => o);
            return groups.OrderByDescending(o => o.Count()).Select(o => o.Key).ToList();
        }
        public static List<ThGeometry> GetWallGeos(this List<ThGeometry> geos)
        {
            // 获取IfcWall几何物体
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.WallCategory)
                .ToList();
        }
        public static List<ThGeometry> GetSlabGeos(this List<ThGeometry> geos)
        {
            // 获取IfcSlab几何物体
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.SlabCategory && !(g.Boundary is DBText))
                .ToList();
        }
        public static List<ThGeometry> GetSlabMarks(this List<ThGeometry> geos)
        {
            // 获取IfcSlab标注
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.SlabCategory && g.Boundary is DBText)
                .ToList();
        }

        public static List<ThGeometry> GetBeamGeos(this List<ThGeometry> geos)
        {
            // 获取IfcBeam几何物体
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.BeamCategory && !(g.Boundary is DBText))
                .ToList();
        }

        public static List<ThGeometry> GetBeamMarks(this List<ThGeometry> geos)
        {
            // 获取IfcBeam标注
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.BeamCategory && g.Boundary is DBText)
                .ToList();
        }

        public static List<ThGeometry> GetBelowColumnGeos(this List<ThGeometry> geos)
        {
            return geos.Where(o => IsBelowFloorColumn(o)).ToList();
        }

        public static List<ThGeometry> GetBelowShearwallGeos(this List<ThGeometry> geos)
        {
            return geos.Where(o => IsBelowFloorShearWall(o)).ToList();
        }

        public static bool IsUpperFloorColumn(this ThGeometry geo)
        {
            string category = geo.Properties.GetCategory();
            var fillColor = geo.Properties.GetFillColor();
            return category==ThIfcCategoryManager.ColumnCategory &&
                fillColor == UpperFloorColumnColor;
        }
        public static bool IsBelowFloorColumn(this ThGeometry geo)
        {
            string category = geo.Properties.GetCategory();
            var fillColor = geo.Properties.GetFillColor();
            return category == ThIfcCategoryManager.ColumnCategory &&
                (fillColor == BelowFloorColumnColor1 || 
                fillColor == BelowFloorColumnColor2);
        }
        public static bool IsUpperFloorShearWall(this ThGeometry geo)
        {
            string category = geo.Properties.GetCategory();
            var fillColor = geo.Properties.GetFillColor();
            return category == ThIfcCategoryManager.WallCategory &&
                fillColor == UpperFloorShearWallColor;
        }
        public static bool IsBelowFloorShearWall(this ThGeometry geo)
        {
            string category = geo.Properties.GetCategory();
            var fillColor = geo.Properties.GetFillColor();
            return category == ThIfcCategoryManager.WallCategory &&
                (fillColor == BelowFloorShearWallColor1 || 
                fillColor == BelowFloorShearWallColor2);
        }
    }
}
