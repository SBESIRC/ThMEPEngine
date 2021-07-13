using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public static class CleanDebugDrawings
    {
        public static void ClearDebugDrawing(Polyline transFrame = null, ThMEPOriginTransformer transformer = null)
        {
            Regex rx = new Regex(@"l[0-9]+");
            List<string> layerList = new List<string>();

            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {

                    if (rx.IsMatch(layer.Name))
                    {
                        layerList.Add(layer.Name);
                    }
                }

                ClearDrawing(layerList, transFrame, transformer);

            }
        }

        private static void ClearDrawing(List<string> layerList, Polyline transFrame = null, ThMEPOriginTransformer transformer = null)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                LayerTable lt = (LayerTable)acadDatabase.Database.LayerTableId.GetObject(OpenMode.ForRead);
                foreach (var layerName in layerList)
                {
                    if (lt.Has(layerName))
                    {
                        acadDatabase.Database.UnFrozenLayer(layerName);
                        acadDatabase.Database.UnLockLayer(layerName);
                        acadDatabase.Database.UnOffLayer(layerName);
                    }
                }

                var items = acadDatabase.ModelSpace
                    .OfType<Entity>()
                    .Where(o => layerList.Contains(o.Layer) == true).ToList();

                var itemDict = new Dictionary<Entity, Entity>();
                if (transFrame != null && transformer != null)
                {
                    foreach (Curve item in items)
                    {
                        var itemTrans = item.Clone() as Entity;
                        transformer.Transform(itemTrans);
                        
                        itemDict.Add(item, itemTrans);
                    }
                    ////transfer 以后经常不共面
                    //itemDict = itemDict.Where(o => transFrame.Contains(o.Value) || transFrame.Intersects(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    //itemDict = items.Where (x=> transFrame.Contains(x) || transFrame.Intersects(x)).ToDictionary(x => x, x => x);
                    itemDict = items.ToDictionary(x => x, x => x);
                }

                var itemsDel = itemDict.Select(x => x.Key);

                foreach (var item in itemsDel)
                {
                    item.UpgradeOpen();
                    item.Erase();
                }

                foreach (var layerName in layerList)
                {
                    acadDatabase.Database.DeleteLayer(layerName);
                }
            }
        }

        public static void ClearFinalDrawing(Polyline transFrame = null, ThMEPOriginTransformer transformer = null)
        {
            var sLayerName = ThDrainageSDCommon.LDrainageGivenSD;
            ClearDrawing(new List<string>() { sLayerName }, transFrame, transformer);
        }
    }
}




