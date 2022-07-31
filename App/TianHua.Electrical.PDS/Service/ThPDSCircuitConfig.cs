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
                DefaultDescription = "消防备用照明",
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
                DefaultDescription = "正常照明",
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
                DefaultDescription = "插座",
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
                DefaultDescription = "动力负载",
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
                DefaultDescription = "动力负载",
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
                DefaultDescription = "动力负载",
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
                DefaultDescription = "消防动力负载",
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
                DefaultDescription = "动力负载",
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
                DefaultDescription = "消防动力负载",
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.FireEmergencyLighting,
                TextKey = "WFEL",
                KV = 0.036,
                Phase = ThPDSPhase.None,
                DemandFactor = 1.0,
                PowerFactor =  0.85,
                FireLoad = true,
                DefaultDescription = "应急照明/疏散指示",
            },
            new ThPDSCircuitConfigItem
            {
                CircuitType = ThPDSCircuitType.Control,
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
