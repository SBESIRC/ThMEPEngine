using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    public enum ThEvacuationDoorType
    {
        单扇,
        双扇,
    }

    public class ThEvacuationDoor
    {
        public int Count_Door_Q { get; set; }
        public double Width_Door_Q { get; set; }
        public double Height_Door_Q { get; set; }
        public double Crack_Door_Q { get; set; }
        public string FloorType_Q { get; set; }
        public ThEvacuationDoorType Type { get; set; }
    }
}
