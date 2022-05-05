using System.Collections.Generic;
using System.Text.RegularExpressions;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSCircuitConfig
    {
        public static List<ThPDSCircuitConfigItem> BlockConfig = new List<ThPDSCircuitConfigItem>
        {
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.EmergencyLighting,
                TextKey = "WLE",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 1.0,
                PowerFactor =  0.85,
                FireLoad = true,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.Lighting,
                TextKey = "WL",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 0.8,
                PowerFactor =  0.85,
                FireLoad = false,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.Socket,
                TextKey = "WS",
                KV = 0.22,
                Phase = ThPDSPhase.一相,
                DemandFactor = 0.8,
                PowerFactor =  0.85,
                FireLoad = false,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WE*-*",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "W*-*",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WM*-*",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WPE",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 1.0,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.PowerEquipment,
                TextKey = "WP",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 0.8,
                PowerFactor =  0.8,
                FireLoad = false,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WE",
                KV = 0.38,
                Phase = ThPDSPhase.三相,
                DemandFactor = 1.0,
                PowerFactor =  0.8,
                FireLoad = true,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WFEL",
                KV = 0.036,
                Phase = ThPDSPhase.None,
                DemandFactor = 1.0,
                PowerFactor =  0.85,
                FireLoad = true,
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.EmergencyPowerEquipment,
                TextKey = "WC",
                KV = 0.036,
                Phase = ThPDSPhase.None,
                DemandFactor = 2.0, //实际不存在，为存储方便设为2
                PowerFactor =  2.0, //实际不存在，为存储方便设为2
                FireLoad = false, //实际不存在，为存储方便设为false
            },
        };

        public static ThPDSCircuitConfigItem SelectModel(string circuitNumber)
        {
            var result = new ThPDSCircuitConfigItem();
            foreach (var o in BlockConfig)
            {
                var check = o.TextKey.Replace("*", ".*");
                var r = new Regex(@check);
                var m = r.Match(circuitNumber);
                if (m.Success)
                {
                    result = o;
                    break;
                }
            }
            return result;
        }
    }
}
