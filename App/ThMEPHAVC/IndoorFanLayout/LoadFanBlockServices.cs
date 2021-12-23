using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using ThCADExtension;

namespace ThMEPHVAC.IndoorFanLayout
{
    static class LoadFanBlockServices
    {
        public const string FanVentBlackName = "AI-风口";
        public const string CoilFanTwoBlackName = "AI-FCU(两管制)";
        public const string CoilFanFourBlackName = "AI-FCU(四管制)";
        public const string VRFFanBlackName = "AI-中静压VRF室内机(风管机)";
        public const string VRFFanFourSideBlackName = "AI-VRF室内机(四面出风型)";
        public const string AirConditionFanBlackName = "AI-吊顶式空调箱";

        public const string FanVentLayerName = "H-DAPP-GRIL";
        public const string CoilFanLayerName = "H-EQUP-FC";
        public const string VRFFanLayerName = "H-EQUP-VRV";
        public const string AirConditionFanLayerName = "H-EQUP-AHU";
        public const string FanBoxLayerName = "H-DUCT-ACON";
        public const string FanBoxMidLayerName = "H-DUCT-ACON-MID";

        static List<string> blockNames = new List<string>()
        {
            FanVentBlackName,
            CoilFanTwoBlackName,
            CoilFanFourBlackName,
            VRFFanBlackName,
            VRFFanFourSideBlackName,
            AirConditionFanBlackName
        };
        static List<string> layerNames = new List<string>()
        {
            FanVentLayerName,
            CoilFanLayerName,
            VRFFanLayerName,
            AirConditionFanLayerName,
            FanBoxLayerName,
            FanBoxMidLayerName,
        };
        public static void LoadBlockLayerToDocument(this Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, true);
                }
                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);
                }
            }
        }
    }
}
