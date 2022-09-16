using System.Collections.Generic;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal static class ThTCHBlockMapConfig
    {
        private static Dictionary<string, string> DoorBlkMap { get; set; }
        static ThTCHBlockMapConfig()
        {
            DoorBlkMap = new Dictionary<string, string>();            
            DoorBlkMap.Add("1", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("2", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("3", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("4", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("5", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("6", ThArchPrintBlockManager.ADoor7);
            DoorBlkMap.Add("9", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("10", ThArchPrintBlockManager.ADoor1);
            DoorBlkMap.Add("11", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("12", ThArchPrintBlockManager.ADoor2);
            DoorBlkMap.Add("21", "");

            DoorBlkMap.Add("114", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("116", ThArchPrintBlockManager.ADoor7);
            DoorBlkMap.Add("127", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("128", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("129", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("130", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("131", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("132", "");
            DoorBlkMap.Add("134", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("135", ThArchPrintBlockManager.ADoor4);
            DoorBlkMap.Add("138", ThArchPrintBlockManager.ADoor5);

            DoorBlkMap.Add("222", "");
            DoorBlkMap.Add("223", "");
            DoorBlkMap.Add("224", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("225", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("226", ThArchPrintBlockManager.ADoor3);
            DoorBlkMap.Add("228", ThArchPrintBlockManager.ADoor6);
            DoorBlkMap.Add("231", ThArchPrintBlockManager.ADoor7);
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
