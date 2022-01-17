using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarmFixLayout
{
    public class ThFaFixCommon
    {
        public static List<string> DisplayPublicBuildingOrder = new List<string> { "消防电梯前室", "前室", "走道", "门厅", "中庭" };

        //public static  List<string> FireLinkageNames = new List<string> { "消防水泵房", "电气机房", "网络通信机房", "计算机机房", "通风机房",
        //                                                        "空调机房", "防排烟机房", "控制室", "电梯机房" };

        public static List<string> FireLinkageNames = new List<string> { "消防水泵房", "发电机房", "配电室", "计算机网络机房", "通风机房", "空调机房",
                                                            "防排烟机房", "灭火控制系统控制室", "消防控制室", "调度中心", "消防电梯机房" };
    }
}
