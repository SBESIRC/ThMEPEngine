using System.Collections.Generic;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal static class ThTCHBlockMapConfig
    {
        private static Dictionary<string, string> DoorBlkMap { get; set; }
        static ThTCHBlockMapConfig()
        {
            DoorBlkMap = new Dictionary<string, string>();            
            DoorBlkMap.Add("$DorLib2D$00000001", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("$DorLib2D$00000002", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("$DorLib2D$00000003", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("$DorLib2D$00000004", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("$DorLib2D$00000005", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("$DorLib2D$00000006", ThArchPrintBlockManager.ADoor7);
            DoorBlkMap.Add("$DorLib2D$00000009", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("$DorLib2D$00000010", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("$DorLib2D$00000011", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("$DorLib2D$00000012", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("$DorLib2D$00000021", "");

            DoorBlkMap.Add("$DorLib2D$00000114", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("$DorLib2D$00000116", ThArchPrintBlockManager.ADoor7);
            DoorBlkMap.Add("$DorLib2D$00000127", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000128", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000129", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000130", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000131", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000132", "");
            DoorBlkMap.Add("$DorLib2D$00000134", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000135", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("$DorLib2D$00000138", ThArchPrintBlockManager.ADoor5);

            DoorBlkMap.Add("$DorLib2D$00000222", "");
            DoorBlkMap.Add("$DorLib2D$00000223", "");
            DoorBlkMap.Add("$DorLib2D$00000224", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("$DorLib2D$00000225", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("$DorLib2D$00000226", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("$DorLib2D$00000228", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("$DorLib2D$00000231", ThArchPrintBlockManager.ADoor7);
        }

        public static string GetDoorBlkName(string tchDoorBlkName)
        {
            if(string.IsNullOrEmpty(tchDoorBlkName))
            {
                return "";
            }
            foreach(var item in DoorBlkMap)
            {
                if(item.Key.Contains(tchDoorBlkName))
                {
                    return item.Value;
                }
            }
            return "";
        }
    }
}
