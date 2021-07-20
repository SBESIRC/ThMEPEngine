using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FireProtectionSystemDiagram.Models
{
    class FloorDataModel
    {
        public int floorNum { get; }
        public double floorLevel { get; set; }
        public bool isRefugeFloor { get; set; }
        public FloorDataModel(int floorNum,bool isRefugeFloor) 
        {
            this.floorNum = floorNum;
            this.floorLevel = 0;
            this.isRefugeFloor = isRefugeFloor;
        }
    }
    class FloorGroupData 
    {
        public List<int> refugeFloors { get; }
        public Dictionary<int, int> floorGroups { get; }
        public int minFloor { get; }
        public int maxFloor { get; }
        public FloorGroupData(int minFloor,int maxFloor) 
        {
            this.refugeFloors = new List<int>();
            this.floorGroups = new Dictionary<int, int>();
            this.minFloor = minFloor;
            this.maxFloor = maxFloor;
        }
    }
}
