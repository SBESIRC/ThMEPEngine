using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.DSFEL
{
    public static class DSFELConfigCommon
    {
        public static List<string> LayoutRoomText = new List<string>() {
            "走道",
            "走廊",
            "连廊",
            "过道",
            "外廊",
            "前室",   //前室、合用前室、消防电梯前室、防烟前室
            "楼梯间",
            "避难",   //避难层、避难间、避难走道
            "非机动车库",
            "电梯厅",
        };

        public static List<string> EvacuationExitArea = new List<string>() {
            "楼梯间",
            "避难",  //避难层、避难间、避难走道
            "前室",  //合用前室
        };
    }
}
