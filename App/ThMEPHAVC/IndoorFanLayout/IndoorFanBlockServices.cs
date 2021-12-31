using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;

namespace ThMEPHVAC.IndoorFanLayout
{
    class IndoorFanBlockServices
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
        public static void LoadBlockLayerToDocument(Database database)
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

        public static Dictionary<string, string> GetFanBlockAttrDynAttrs(FanLoadBase fanLoad, out Dictionary<string, object> blockDynAttrs)
        {
            var blockAttrs = new Dictionary<string, string>();
            blockDynAttrs = new Dictionary<string, object>();
            blockAttrs.Add("设备编号", fanLoad.FanNumber);
            if (fanLoad is CoilFanLoad coilFanLoad)
            {
                string hotColdStr = coilFanLoad.GetCoolHotString(out string waterTempStr);
                blockAttrs.Add("制冷量/制热量", hotColdStr);
                blockAttrs.Add("冷水温差/热水温差", waterTempStr);
            }
            else if (fanLoad is AirConditionFanLoad airCondition)
            {
                string hotColdStr = airCondition.GetCoolHotString(out string waterTempStr);
                blockAttrs.Add("制冷量/制热量", hotColdStr);
                blockAttrs.Add("冷水温差/热水温差", waterTempStr);
            }
            else
            {
                blockAttrs.Add("制冷量/制热量", string.Format("{0}kW/{1}kW", fanLoad.FanRealCoolLoad, fanLoad.FanRealHotLoad));
            }

            blockAttrs.Add("设备电量", string.Format("{0}W", fanLoad.FanPower));

            blockDynAttrs.Add("设备宽度", fanLoad.FanWidth);
            blockDynAttrs.Add("设备深度", fanLoad.FanLength);
            return blockAttrs;
        }
        public static string GetBlockLayerNameTextAngle(EnumFanType fanType,out string layerName,out double textAngle) 
        {
            string blockName = "";
            layerName = "";
            textAngle = 0.0;
            switch (fanType) 
            {
                case EnumFanType.FanCoilUnitFourControls:
                case EnumFanType.FanCoilUnitTwoControls:
                    blockName = fanType == EnumFanType.FanCoilUnitTwoControls ? CoilFanTwoBlackName : CoilFanFourBlackName;
                    layerName = CoilFanLayerName;
                    break;
                case EnumFanType.VRFConditioninConduit:
                    blockName = VRFFanBlackName;
                    layerName = VRFFanLayerName;
                    textAngle = Math.PI / 2;
                    break;
                case EnumFanType.VRFConditioninFourSides:
                    blockName = VRFFanFourSideBlackName;
                    layerName = VRFFanLayerName;
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    blockName = AirConditionFanBlackName;
                    layerName = AirConditionFanLayerName;
                    textAngle = Math.PI / 2;
                    break;
            }
            return blockName;
        }
    }
}
