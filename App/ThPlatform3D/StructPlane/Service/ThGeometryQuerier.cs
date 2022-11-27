using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.StructPlane.Service
{
    internal static class ThGeometryQuerier
    {
        private const string UpperFloorColumnColor = "#7f3f3f";
        private const string BelowFloorColumnColor1 = "#ff0000";
        private const string BelowFloorColumnColor2 = "Red";

        private const string UpperFloorShearWallColor = "#ff7f00";
        private const string BelowFloorShearWallColor1 = "#ffff00";
        private const string BelowFloorShearWallColor2 = "Yellow";
        private const string CantiSlabSign = "CANTISLAB";

        public static List<string> GetSlabElevations(this List<ThGeometry> geos)
        {
            var groups = GetSlabGeos(geos)
                .Select(g => g.Properties.GetElevation())
                .Where(g => !string.IsNullOrEmpty(g))
                .GroupBy(o => o);
            return groups.OrderByDescending(o => o.Count()).Select(o => o.Key).ToList();
        }
        public static List<ThGeometry> GetAllWallGeos(this List<ThGeometry> geos)
        {
            // 获取IfcWall几何物体
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.WallCategory)
                .ToList();
        }
        public static List<ThGeometry> GetStandardWallGeos(this List<ThGeometry> geos)
        {
            // 获取IfcWall几何物体
            return geos.GetAllWallGeos()
                .Where(g => g.Properties.GetDescription().IsStandardWall())
                .ToList();
        }

        public static List<ThGeometry> GetPCWallGeos(this List<ThGeometry> geos)
        {
            return geos.GetAllWallGeos()
                .Where(g => g.Properties.GetDescription().IsPCWall())
                .ToList();
        }

        public static List<ThGeometry> GetBelowPCWallGeos(this List<ThGeometry> geos)
        {
            return geos.GetPCWallGeos()
                .Where(g => g.IsBelowFloorShearWall())
                .ToList();
        }
        public static List<ThGeometry> GetUpperPCWallGeos(this List<ThGeometry> geos)
        {
            return geos.GetPCWallGeos()
                .Where(g => g.IsUpperFloorShearWall())
                .ToList();
        }

        public static List<ThGeometry> GetPassHeightGeos(this List<ThGeometry> geos)
        {
            // 获取type=IfcWall,description="S_CONS_通高墙"
            return geos.GetAllWallGeos()
                .Where(g => g.Properties.GetDescription().IsPassHeightWall())
                .ToList();
        }

        public static List<ThGeometry> GetWindowHeightGeos(this List<ThGeometry> geos)
        {
            // 获取type=IfcWall,description="S_CONS_窗台墙"
            return geos.GetAllWallGeos()
                .Where(g => g.Properties.GetDescription().IsWindowWall())
                .ToList();
        }

        public static List<ThGeometry> GetSlabGeos(this List<ThGeometry> geos)
        {
            // 获取IfcSlab几何物体
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.SlabCategory && !(g.Boundary is DBText))
                .ToList();
        }
        public static List<ThGeometry> GetCantiSlabGeos(this List<ThGeometry> geos)
        {
            // 获取IfcSlab几何物体
            return geos
                .Where(g => IsCantiSlab(g) && !(g.Boundary is DBText))
                .ToList();
        }

        public static List<ThGeometry> GetSlabMarks(this List<ThGeometry> geos)
        {
            // 获取IfcSlab标注
            return geos
                .Where(g => g.Properties.GetCategory() == ThIfcCategoryManager.SlabCategory && g.Boundary is DBText)
                .ToList();
        }

        public static List<ThGeometry> GetTenThickSlabMarks(this List<ThGeometry> geos)
        {
            // 获取10mm厚度的IfcSlab标注
            return GetSlabMarks(geos)
                .Where(g => g.Boundary is DBText dbText && IsTenThickSlab(dbText.TextString))
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

        public static List<ThGeometry> GetBelowStandardColumnGeos(this List<ThGeometry> geos)
        {
            return geos.Where(o => IsBelowFloorColumn(o) && o.Properties.GetDescription().IsStandardColumn()).ToList();
        }

        public static List<ThGeometry> GetBelowStandardShearwallGeos(this List<ThGeometry> geos)
        {
            return geos.Where(o => IsBelowFloorShearWall(o) && o.Properties.GetDescription().IsStandardWall()).ToList();
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
        public static bool IsCantiSlab(this ThGeometry geo)
        {
            string category = geo.Properties.GetCategory();
            string name = geo.Properties.GetName().ToUpper();
            return category == ThIfcCategoryManager.SlabCategory && name.StartsWith(CantiSlabSign);
        }
        public static bool IsTenThickSlab(this string content)
        {
            var values = content.GetDoubles();
            if (values.Count == 1)
            {
                return Math.Abs(values[0] - 10.0) <= 1e-4;
            }
            return false;
        }

        public static bool IsStandardColumn(this string description)
        {
            return string.IsNullOrEmpty(description);
        }

        public static bool IsConstructColumn(this string description)
        {
            return description.ToUpper().Contains("S_CONS_构造柱");
        }

        public static bool IsStandardWall(this string description)
        {
            return string.IsNullOrEmpty(description);
        }

        public static bool IsPCWall(this string description)
        {
            return description.ToUpper().Contains("PCWALL");
        }

        public static bool IsPassHeightWall(this string description)
        {
            // 全混凝土外墙（通高）
            return description.ToUpper().Contains("S_CONS_通高墙");
        }

        public static bool IsWindowWall(this string description)
        {
            // 全混凝土外墙（窗台）
            return description.Contains("S_CONS_窗台墙");
        }
    }
}
