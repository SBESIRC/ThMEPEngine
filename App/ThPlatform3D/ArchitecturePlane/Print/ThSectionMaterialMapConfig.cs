using System;
using System.Collections.Generic;
using ThMEPEngineCore.IO;
using ThPlatform3D.Model.Printer;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThSectionMaterialMapConfig
    {
        private static Dictionary<string, Tuple<HatchPrintConfig,PrintConfig>> Config { get; set; }
        static ThSectionMaterialMapConfig() 
        {
            Config = new Dictionary<string, Tuple<HatchPrintConfig, PrintConfig>>();
            Config.Add(ThTextureMaterialManager.THGasConcrete, CreateThGasConcrete());
            Config.Add(ThTextureMaterialManager.THSteelConcrete, CreateThSteelConcrete());
            Config.Add(ThTextureMaterialManager.THQD_COMMONBRICK, CreateThQD_COMMONBRICK());
            Config.Add(ThTextureMaterialManager.THNoColourFnsh, CreateThNoColourFnsh());
            Config.Add(ThTextureMaterialManager.THSuConcrete, CreateTHSuConcrete());
            Config.Add(ThTextureMaterialManager.THShiCai, CreateThShiCai());
            Config.Add(ThTextureMaterialManager.THBaoWenCeng, CreateThBaoWenCeng());
            Config.Add(ThTextureMaterialManager.THLanGan, CreateThLanGan());
            Config.Add(ThTextureMaterialManager.ThMenChuangKaiqiShikuai, CreateMenChuangKaiqiShikuai());
        }
        public static Tuple<HatchPrintConfig,PrintConfig> GetHatchPrintConfig(string materialName)
        {
            if(Config.ContainsKey(materialName))
            {
                return Config[materialName];
            }
            else
            {
                return null;
            }
        }

        private static Tuple<HatchPrintConfig,PrintConfig> CreateThGasConcrete()
        {
            //加气混凝土
            var hatchConfig = new HatchPrintConfig
            {
                PatternScale = 50.1,
                PatternName = "AR-CONC",
                LayerName = ThArchPrintLayerManager.SDETLHACH,
            };
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig,outlineConfig);
        }

        private static Tuple<HatchPrintConfig,PrintConfig> CreateThSteelConcrete()
        {
            //钢筋混凝土
            var hatchConfig = new HatchPrintConfig
            {
                PatternScale = 1.0,
                PatternName = "SOLID",
                LayerName = ThArchPrintLayerManager.AESTRUHACH,
            };
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig,outlineConfig);
        }

        private static Tuple<HatchPrintConfig,PrintConfig> CreateThQD_COMMONBRICK()
        {
            //加气混凝土
            var hatchConfig = new HatchPrintConfig
            {
                PatternScale = 500.0, //50.1,
                //PatternName = "QD_COMMONBRICK",
                PatternName = "STEEL",
                LayerName = ThArchPrintLayerManager.AEWALLHACH,
            };
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig,outlineConfig);
        }

        private static Tuple<HatchPrintConfig, PrintConfig> CreateThNoColourFnsh()
        {
            HatchPrintConfig hatchConfig = null;
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
        private static Tuple<HatchPrintConfig, PrintConfig> CreateTHSuConcrete()
        {
            HatchPrintConfig hatchConfig = null;
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
        private static Tuple<HatchPrintConfig, PrintConfig> CreateThShiCai()
        {
            //石材
            var hatchConfig = new HatchPrintConfig
            {
                PatternScale = 50.0,
                PatternName = "ANSI33",
                LayerName = ThArchPrintLayerManager.AEFNSH,
            };
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEFNSH,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
        private static Tuple<HatchPrintConfig, PrintConfig> CreateThBaoWenCeng()
        {
            var hatchConfig = new HatchPrintConfig
            {
                PatternScale = 50.0,
                PatternName = "NET",
                LayerName = ThArchPrintLayerManager.AEFNSH,
            };
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEFNSH,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
        private static Tuple<HatchPrintConfig, PrintConfig> CreateThLanGan()
        {
            HatchPrintConfig hatchConfig = null;
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEHDWR,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
        private static Tuple<HatchPrintConfig, PrintConfig> CreateMenChuangKaiqiShikuai()
        {
            HatchPrintConfig hatchConfig = null;
            var outlineConfig = new PrintConfig
            {
                LayerName = ThArchPrintLayerManager.AEWALL,
            };
            return Tuple.Create(hatchConfig, outlineConfig);
        }
    }
}
