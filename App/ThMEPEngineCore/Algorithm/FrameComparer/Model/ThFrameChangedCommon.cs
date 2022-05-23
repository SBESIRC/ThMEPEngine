using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.FrameComparer.Model
{
    public class ThFrameChangedCommon
    {
        public enum CompareFrameType
        {
            DOOR, WINDOW, ROOM, FIRECOMPONENT
        }

        public static string ChangeType_Change = "框线改变";
        public static string ChangeType_Append = "新增框线";
        public static string ChangeType_Delete = "删除框线";
        public static string ChangeType_ChangeText = "功能改变";
        public static string TempLayer = "AI-临时";

        public static Dictionary<CompareFrameType, string> FrameTypeName = new Dictionary<CompareFrameType, string>() {
                                                                        {CompareFrameType.DOOR ,"门" },
                                                                        {CompareFrameType.WINDOW ,"窗" },
                                                                        {CompareFrameType.ROOM ,"房间框线" },
                                                                        {CompareFrameType.FIRECOMPONENT ,"防火分区" },
                                                                        };
    }
}
