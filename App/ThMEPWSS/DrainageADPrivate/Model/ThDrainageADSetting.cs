using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    public class ThDrainageADSetting
    {
        public double qL { get; set; }
        public double m { get; set; }
        public double Kh { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; }

        public static ThDrainageADSetting Instance = new ThDrainageADSetting();

        public ThDrainageADSetting()
        {
            qL = 230;
            m = 3.5;
            Kh = 1.5;
            BlockNameDict = new Dictionary<string, List<string>>();

        }
    }
}
