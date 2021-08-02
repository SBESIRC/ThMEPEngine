using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FireProtectionSystemDiagram.Models;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.FireProtectionSystemDiagram.Bussiness
{
    class InputDataConvert
    {
        public static FloorGroupData SplitFloor(FireControlSystemDiagramViewModel _vm)
        {
            int minFloor = int.MaxValue;
            int maxFloor = int.MinValue;
            var refugeFloors = new List<int>();
            var floorInts = new Dictionary<int, int>();
            var listFloor = _vm.ZoneConfigs.ToList();
            for (int i = 0; i < listFloor.Count; i++)
            {
                var floor = listFloor[i];
                if (!floor.IsEffective() || string.IsNullOrEmpty(floor.StartFloor))
                    break;
                int startFloor = floor.GetIntStartFloor().Value;
                int endFloor = floor.GetIntEndFloor().Value;
                floorInts.Add(startFloor, endFloor);
                minFloor = Math.Min(startFloor, minFloor);
                maxFloor = Math.Max(endFloor, maxFloor);
                bool isRefugeFloor = false;
                if (i != 0)
                {
                    var preFloor = listFloor[i - 1];
                    isRefugeFloor = preFloor.GetIntEndFloor().Value == floor.GetIntStartFloor().Value;
                }
                if (isRefugeFloor)
                    refugeFloors.Add(startFloor);
            }
            var groupData = new FloorGroupData(minFloor,maxFloor);
            if (null != floorInts && floorInts.Count > 0)
                foreach (var keyValue in floorInts)
                    groupData.floorGroups.Add(keyValue.Key, keyValue.Value);
            if (null != refugeFloors && refugeFloors.Count > 0)
                groupData.refugeFloors.AddRange(refugeFloors);
            return groupData;
        }
        public static List<FloorDataModel> FloorDataModels(FloorGroupData groupData) 
        {
            var floors = new List<FloorDataModel>();
            int i = groupData.minFloor;
            while (i <= groupData.maxFloor) 
            {
                var floor = new FloorDataModel(i, groupData.refugeFloors.Any(c => c == i));
                floors.Add(floor);
                i += 1;
            }
            return floors;
        }
    }
}
