using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDMessageServie
    {
        public static void WriteMessage(string msg)
        {
            Active.Editor .WriteMessage(msg);
        }
    }

    public class ThDrainageSDMessageCommon
    {
        public static string startPtNoInFrame = "\n起点不在选择框内";
        public static string noRoomToilet = "\n框选范围没有房间或洁具";
        public static string noPipe = "\n框选范围没管线";
        public static string noStart = "\n管线找不到起点";
    }

}
