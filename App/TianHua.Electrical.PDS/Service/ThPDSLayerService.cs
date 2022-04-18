using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSLayerService
    {
        /// <summary>
        /// 标注
        /// </summary>
        /// <returns></returns>
        public static List<string> CircuitMarkLayers()
        {
            return new List<string>
            {
                "E-UNIV-NOTE",
                "E-*-DIMS",
            };
        }

        /// <summary>
        /// 桥架
        /// </summary>
        /// <returns></returns>
        public static List<string> CabletrayLayers()
        {
            return new List<string>
            {
                "E-POWR-CMTB",
                "E-LITE-CMTB",
                "E-UNIV-EL2",
                //"E-CTRL-CMTB",
            };
        }

        /// <summary>
        /// 回路
        /// </summary>
        /// <returns></returns>
        public static List<string> CableLayers()
        {
            return new List<string>
            {
                "E-LITE-WIRE",
                "E-LITE-WIRE2",
                "E-LITE-WIRE-LV",
                "E-POWR-WIRE",
                "E-POWR-WIRE2",
                "E-POWR-WIRE3",
                //"E-CTRL-WIRE",
            };
        }

        /// <summary>
        /// 控制回路
        /// </summary>
        /// <returns></returns>
        public static string ControlCircuitLayer()
        {
            return "E-CTRL-WIRE";
        }

        /// <summary>
        /// 配电箱框线图层
        /// </summary>
        /// <returns></returns>
        public static string DistBoxFrameLayer()
        {
            return "E-REQU-WALL";
        }

        /// <summary>
        /// 表格框线图层
        /// </summary>
        /// <returns></returns>
        public static string TableFrameLayer()
        {
            return "E-UNIV-DIAG";
        }

        /// <summary>
        /// 照明桥架图层
        /// </summary>
        /// <returns></returns>
        public static List<string> LightingCableTrayLayer()
        {
            return new List<string>
            {
                "E-LITE-CMTB",
                "E-UNIV-EL2",
            };
        }

        public static void SelectCircuitType(ThPDSCircuit circuit, ThPDSLoad load, string layer, bool needAssign)
        {
            switch (layer)
            {
                case "E-POWR-WIRE":
                    load.CircuitType = ThPDSCircuitType.PowerEquipment;
                    break;
                case "E-POWR-WIRE2":
                    load.CircuitType = ThPDSCircuitType.Socket;
                    break;
                case "E-POWR-WIRE3":
                    load.CircuitType = ThPDSCircuitType.PowerEquipment;
                    break;
                case "E-LITE-WIRE":
                    load.CircuitType = ThPDSCircuitType.Lighting;
                    break;
                case "E-LITE-WIRE2":
                    load.CircuitType = ThPDSCircuitType.EmergencyLighting;
                    break;
                case "E-LITE-WIRE-LV":
                    load.CircuitType = ThPDSCircuitType.Lighting;
                    break;
                default:
                    load.CircuitType = ThPDSCircuitType.None;
                    break;
            }
            if (needAssign)
            {
                Assign(load);
            }
        }

        public static void Assign(ThPDSLoad load)
        {
            if (load.CircuitType != ThPDSCircuitType.None)
            {
                var config = ThPDSCircuitConfig.BlockConfig
                    .Where(o => o.CircuitType == load.CircuitType).First();
                load.Phase = config.Phase;
                load.DemandFactor = config.DemandFactor;
                load.PowerFactor = config.PowerFactor;
            }
        }

        public static ObjectId CreateAITestCableLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-CABLE", 1);
        }

        public static ObjectId CreateAITestCabletrayLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-CABLETRAY", 2);
        }

        public static ObjectId CreateAITestDistributionLayer(this Database database)
        {
            return database.CreateAILayer("AI-TEST-DISTRIBUTION", 3);
        }
    }
}
