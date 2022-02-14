using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFAS.ViewModel
{
    public class FireAlarmSetting
    {
        //----通用
        public double Scale { get; set; }
        public int SelectFloorRoom { get; set; }//floor:0,room:1
        public int FloorUpDown { get; set; }//down:0,up:1

        public int Beam { get; set; }
        public List<int> LayoutItemList { get; set; }
        public double RoofThickness { get; set; }

        public double BufferDist { get; set; }

        //----烟温感
        public int RoofHight { get; set; }
        public int RoofGrade { get; set; }
        public double FixRef { get; set; }

        //----广播
        public int BroadcastLayout { get; set; }
        public double StepLengthBC { get; set; }

        //----手报
        public double StepLengthMA { get; set; }

        //----可燃气
        public double GasProtectRadius { get; set; }

        //----显示器
        public int DisplayBuilding { get; set; }
        public int DisplayBlk { get; set; }

        //----照明
        public double IlluRadiusNormal { get; set; }
        public double IlluRadiusEmg { get; set; }
        public bool IlluIfLayoutEmg { get; set; }
        public bool IlluIfEmgAsNormal { get; set; }
        public int IlluLightType { get; set; }

        public static FireAlarmSetting Instance = new FireAlarmSetting();

        public FireAlarmSetting()
        {
            Scale = 100;
            SelectFloorRoom = 0;
            FloorUpDown = 0;

            Beam = 1;
            RoofThickness = 100;
            BufferDist = 500;

            RoofHight = 2;
            RoofGrade = 0;
            FixRef = 1.0;

            BroadcastLayout = 1;
            StepLengthBC = 20 * 1000;

            StepLengthMA = 20 * 1000;

            GasProtectRadius = 8000;

            DisplayBuilding = 0;
            DisplayBlk = 0;

            IlluRadiusNormal = 3000;
            IlluRadiusEmg = 3000;
            IlluIfLayoutEmg = false;
            IlluIfEmgAsNormal = false;
            IlluLightType = 0;

            LayoutItemList = new List<int>();

        }
    }
}
