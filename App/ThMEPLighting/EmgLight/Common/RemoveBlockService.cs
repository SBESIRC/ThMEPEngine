using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;


namespace ThMEPLighting.EmgLight.Service
{
    public static class RemoveBlockService
    {
        public static void ClearDrawing()
        {
            Regex rx = new Regex(@"l[0-9]+");
           
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    
                    if (rx.IsMatch(layer.Name))
                    {
                        ClearDrawing(layer.Name);
                    }
                }
            }
        }

        private static void ClearDrawing(string layerName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                LayerTable lt = (LayerTable)acadDatabase.Database.LayerTableId.GetObject(OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    acadDatabase.Database.UnFrozenLayer(layerName);
                    acadDatabase.Database.UnLockLayer(layerName);
                    acadDatabase.Database.UnOffLayer(layerName);

                    var items = acadDatabase.ModelSpace
                        .OfType<Entity>()
                        .Where(o => o.Layer == layerName);

                    foreach (var line in items)
                    {
                        line.UpgradeOpen();
                        line.Erase();
                    }

                    acadDatabase.Database.DeleteLayer(layerName);
                }
            }
        }

        public static void ClearEmergencyLight( Dictionary<BlockReference, BlockReference> emgLight)
        {
            var objs = new DBObjectCollection();
            emgLight.Select(x => x.Key).ForEachDbObject(o => objs.Add(o));
            foreach (BlockReference  spray in objs)
            {
                spray.UpgradeOpen();
                spray.Erase();
            }
        }

        public static void ClearEmgConnect()
        {
            var sLayerName = ThMEPLightingCommon.EmgLightConnectLayerName;
            ClearDrawing(sLayerName);
        }

    }



}
