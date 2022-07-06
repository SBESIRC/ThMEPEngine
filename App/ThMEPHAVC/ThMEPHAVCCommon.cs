using System;
using AcHelper;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC
{
    public static class ThMEPHAVCCommon
    {
        public static double MmToMeter(this int length)
        {
            return (length * 1.0).MmToMeter();
        }

        public static double MmToMeter(this double length)
        {
            return length / 1000.0;
        }

        public static double GetArea(double length, double width)
        {
            return length * width;
        }

        public static double GetArea(double radius)
        {
            return Math.PI * radius * radius;
        }

        public static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

        public static List<ThIfcRoom> FindRooms(this Point3d wcsPt, List<ThIfcRoom> rooms)
        {
            // 查找包括此点的房间
            return rooms
                .Where(o => o.Boundary != null)
                .Where(o => o.Boundary.EntityContains(wcsPt))
                .ToList();
        }

        //正压送风平面
        public const string SMOKE_PROOF_BLOCK_NAME = "AI-防烟计算";
        public const string SMOKE_PROOF_LAYER_NAME = "AI-房间功能";

        //xdata
        public const string RegAppName_SmokeProof = "THCAD_SMOKE_PROOF";
    }
}
