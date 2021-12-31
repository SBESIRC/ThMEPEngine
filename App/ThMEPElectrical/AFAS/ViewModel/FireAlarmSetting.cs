using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFAS.ViewModel
{
    public class FireAlarmSetting
    {
        public double Scale { get; set; }
        public int Beam { get; set; }
        public int LayoutItem { get; set; }
        public List<int> LayoutItemList { get; set; }
        public int RoofHight { get; set; }
        public int RoofGrade { get; set; }
        public double RoofThickness { get; set; }
        public double FixRef { get; set; }
        public int BroadcastLayout { get; set; }
        public double StepLengthBC { get; set; }
        public double StepLengthMA { get; set; }
        public double GasProtectRadius { get; set; }

        public int DisplayBuilding { get; set; }
        public int DisplayBlk { get; set; }

        public static FireAlarmSetting Instance = new FireAlarmSetting();

        public FireAlarmSetting()
        {
            Scale = 100;
            Beam = 1;
            LayoutItem = 0;

            RoofHight = 2;
            RoofGrade = 0;
            RoofThickness = 100;
            FixRef = 1.0;

            BroadcastLayout = 1;
            StepLengthBC = 25 * 1000;

            StepLengthMA = 25 * 1000;

            GasProtectRadius = 8000;

            DisplayBuilding = 0;
            DisplayBlk = 0;

            LayoutItemList = new List<int>();

        }
    }
}
