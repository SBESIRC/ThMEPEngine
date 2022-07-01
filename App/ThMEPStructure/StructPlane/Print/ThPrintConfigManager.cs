using System.Collections.Generic;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.ArchitecturePlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal static class ThPrintConfigManager
    {
        public static PrintConfig GetBeamConfig(this Dictionary<string, object> properties)
        {
            var config = ThBeamPrinter.GetBeamConfig();
            var lineType = properties.GetLineType();
            if (string.IsNullOrEmpty(lineType))
            {
                return config;
            }
            else
            {
                // 根据模板来设置
                if (lineType.ToUpper() == "CONTINUOUS")
                {
                    config.LineType = "ByBlock";
                }
                else
                {
                    config.LineType = "ByLayer";
                }
                return config;
            }
        }
        public static PrintConfig GetColumnOutlineConfig(this Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#7f3f3f") // 上层柱
            {
                return ThColumnPrinter.GetUpperColumnConfig();
            }
            else if (fillColor == "#ff0000" || fillColor == "Red") //下层柱
            {
                return ThColumnPrinter.GetBelowColumnConfig();
            }
            else
            {
                return new PrintConfig();
            }
        }
        public static HatchPrintConfig GetColumnHatchConfig(this Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#7f3f3f") // 上层柱
            {
                return ThColumnPrinter.GetUpperColumnHatchConfig();
            }
            else if (fillColor == "#ff0000" || fillColor == "Red") //下层柱
            {
                return ThColumnPrinter.GetBelowColumnHatchConfig();
            }
            else
            {
                return new HatchPrintConfig();
            }
        }
        public static PrintConfig GetShearWallConfig(this Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#ff7f00") // 上层墙
            {
                return ThShearwallPrinter.GetUpperShearWallConfig();
            }
            else if (fillColor == "#ffff00" || fillColor == "Yellow") //下层墙
            {
                return ThShearwallPrinter.GetBelowShearWallConfig();
            }
            else
            {
                return new PrintConfig();
            }
        }
        public static HatchPrintConfig GetShearWallHatchConfig(this Dictionary<string, object> properties)
        {
            var fillColor = properties.GetFillColor();
            if (fillColor == "#ff7f00") // 上层墙
            {
                return ThShearwallPrinter.GetUpperShearWallHatchConfig();
            }
            else if (fillColor == "#ffff00" || fillColor == "Yellow") //下层墙
            {
                return ThShearwallPrinter.GetBelowShearWallHatchConfig();
            }
            else
            {
                return new HatchPrintConfig();
            }
        }
        public static Dictionary<string, HatchPrintConfig> GetSlabHatchConfigs(this List<string> elevations)
        {
            var results = new Dictionary<string, HatchPrintConfig>();
            var configs = ThSlabPrinter.GetSlabHatchConfigs();
            for (int i = 0; i < elevations.Count; i++)
            {
                if (i < configs.Count)
                {
                    results.Add(elevations[i], configs[i]);
                }
                else
                {
                    results.Add(elevations[i], null);
                }
            }
            return results;
        }
        public static PrintConfig GetOpeningConfig(this Dictionary<string, object> properties)
        {
            return ThHolePrinter.GetHoleConfig();
        }
        public static HatchPrintConfig GetOpeningHatchConfig(this Dictionary<string, object> properties)
        {
            return ThHolePrinter.GetHoleHatchConfig();
        }
    }
}
