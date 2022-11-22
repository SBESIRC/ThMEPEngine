using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.PressureDrainageSystem.Model;
using static DotNetARX.Preferences;
using static NPOI.HSSF.Util.HSSFColor;

namespace ThMEPWSS.PumpSectionalView.Utils
{
    public class Pump_Arr
    {
        public Pump_Arr()
        {
            No = "";
            Flow_Info = 1.0;
            Head = 1.0;
            Power = 1.0;
            Num = 1;
            Note = "";
            NoteSelect = "";
            Hole = 1.0;
            Type = "";
        }
        public string No;//编号
        public double Flow_Info;//信息流量q
        public double Head;//扬程h
        public double Power;//功率p
        public int Num;//台数n
        public string Note;//自定义备注
        public string NoteSelect;//下拉备注
        public double Hole;//放气孔高度
        public string Type;//泵类型
    }
}
