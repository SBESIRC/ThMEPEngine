using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Print
{
    public static class PrintMarks
    {
        public static void PrintNoteLines(List<Polyline> lines)
        {
            InsertBlockService.InsertConnectPipe(lines, ThWSSCommon.DrivepipeNoteLayerName, null);
        }

        public static void PrintText(List<DBText> txts)
        {
            InsertBlockService.InsertText(txts, ThWSSCommon.DrivepipeNoteLayerName, null);
        }
    }
}
