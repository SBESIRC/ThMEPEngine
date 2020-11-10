using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionUICommon
    {
        public const string DOCUMENT_USER_DATA_UI = "THMODELPICKDLG";
        public const string DOCUMENT_USER_DATA_UI_VISIBLE = "THMODELPICKDLGVISIBLE";


	public const string MODEL_EXPORTCATALOG = "FanDesignData.json";        
	public const string NOTE_CENTER_EXTRACTION = "中庭周围场所设有排烟系统时，中庭采用机械排烟系统的，中庭排烟量应按周围场所防烟分区中最大排烟量的2倍数值计算，且不应小于107000m³/h。";
        public const string NOTE_CENTER_EXTRACTION_NOSMOKE = "当中庭周围场所不需设置排烟系统，仅在回廊设置排烟系统时，回廊的排烟量不应小于13000m³/h。中庭的排烟量不应小于40000m³/h。";
    }
}
