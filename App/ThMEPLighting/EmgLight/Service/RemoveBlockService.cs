using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using DotNetARX;
using NFox.Cad;
using ThCADCore.NTS;

namespace ThMEPLighting.EmgLight.Service
{
    public static class RemoveBlockService
    {
        public static void ClearEmergencyLight(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPLightingCommon.EmgLightLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPLightingCommon.EmgLightLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPLightingCommon.EmgLightLayerName);

                //获取应急照明
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThMEPLightingCommon.EmgLightLayerName &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var emgLight = new List<BlockReference>();
                var allEmgLight = Active.Editor.SelectAll(filterlist);
                if (allEmgLight.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allEmgLight.Value.GetObjectIds())
                        {
                            emgLight.Add(acdb.Element<BlockReference>(obj));
                        }
                    }
                }
                var objs = new DBObjectCollection();
                emgLight.Where(o => polyline.Contains(o.Position)).ForEachDbObject(o => objs.Add(o));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }


    }
}
