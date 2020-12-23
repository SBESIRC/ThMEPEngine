using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPLighting.Garage.Service
{
    public class ThQueryLightBlockService
    {
        private ThQueryLightBlockService()
        {
        }
        public static ThQueryLightBlockService Create()
        {
            var instance = new ThQueryLightBlockService();
            instance.create();
            return instance;
        }
        private void create()
        {
            using (var acadDatabase=AcadDatabase.Active())
            {
                var tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(BlockReference)).DxfName),
                    new TypedValue((int)DxfCode.RegAppFlags,RXClass.GetClass(typeof(BlockReference)).DxfName),
                };
                Active.Editor.SelectAll();
            }
        }
    }
}
