using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;


namespace ThMEPLighting.EmgLight.Service
{
    public static class RemoveBlockService
    {
        public static Dictionary<BlockReference, Point3d> ExtractClearEmergencyLight(ThMEPOriginTransformer transformer)
        {
            var emgLight = new Dictionary<BlockReference, Point3d>();
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

                var allEmgLight = Active.Editor.SelectAll(filterlist);
                if (allEmgLight.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allEmgLight.Value.GetObjectIds())
                        {
                            var block = acdb.Element<BlockReference>(obj);
                            var blockTrans = new Point3d(block.Position.X, block.Position.Y, block.Position.Z);
                            transformer.Transform(ref blockTrans);
                            emgLight.Add(block, blockTrans);
                        }
                    }
                }
            }

            return emgLight;
        }

        public static void ClearEmergencyLight(this Polyline polyline, Dictionary<BlockReference, Point3d> emgLight)
        {

            var objs = new DBObjectCollection();
            emgLight.Where(o => polyline.Contains(o.Value)).Select(x => x.Key).ForEachDbObject(o => objs.Add(o));
            foreach (Entity spray in objs)
            {
                spray.UpgradeOpen();
                spray.Erase();
            }
        }
    }



}
