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
        public static void PrintNoteLines(List<Polyline> lines, string layer, double scale)
        {
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertConnectPipe(lines, layer, null);
        }

        public static void PrintText(List<DBText> txts, string layer, double scale)
        {
            InsertBlockService.scaleNum = scale;
            InsertBlockService.InsertText(txts, layer, null);
        }
    }
}
