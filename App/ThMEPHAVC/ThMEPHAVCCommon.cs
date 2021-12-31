using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC
{
    public static class ThMEPHAVCCommon
    {
        public static double MmToMeter(this int length)
        {
            return (length*1.0).MmToMeter();
        }

        public static double MmToMeter(this double length)
        {
            return length / 1000.0;
        }

        public static double GetArea(double length,double width)
        {
            return length * width;
        }

        public static double GetArea(double radius)
        {
            return Math.PI* radius * radius;
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
        /// <summary>
        /// 创建和打开图层的设置
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layer"></param>
        public static void CreateLayer(this Database database,string layer)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                acadDb.Database.AddLayer(layer);
                acadDb.Database.UnLockLayer(layer);
                acadDb.Database.UnOffLayer(layer);
                acadDb.Database.UnHidden(layer);
                acadDb.Database.UnFrozenLayer(layer);
            }
        }

        public static List<ThIfcRoom> FindRooms(this Point3d wcsPt,List<ThIfcRoom> rooms)
        {
            // 查找包括此点的房间
            return rooms
                .Where(o=>o.Boundary!=null)
                .Where(o => o.Boundary.IsContains(wcsPt))
                .ToList();
        }
    }
}
