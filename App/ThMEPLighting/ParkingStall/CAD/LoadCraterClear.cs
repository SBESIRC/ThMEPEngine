using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;

namespace ThMEPLighting.ParkingStall.CAD
{
    class LoadCraterClear
    {
        static List<string> layerNames = new List<string>()
        {
            ParkingStallCommon.PARK_LIGHT_CONNECT_LAYER
        };
        public static void LoadBlockLayerToDocument(Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    foreach (var item in layerNames)
                    {
                        if (string.IsNullOrEmpty(item))
                            continue;
                        var layer = blockDb.Layers.ElementOrDefault(item);
                        if (null == layer)
                            continue;
                        currentDb.Layers.Import(layer, true);
                    }
                }
            }
        }
        public static void ClaerHistoryBlocks(Database database, string blockName, Polyline outPolyline, List<Polyline> innerPolylines, ThMEPOriginTransformer originTransformer) 
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                var delEntitys = new List<BlockReference>();
                var targetEntitys = currentDb.ModelSpace
                   .OfType<BlockReference>().Where(o => o.GetEffectiveName() == blockName);
                if (targetEntitys == null || targetEntitys.Count() < 1)
                    return;
                targetEntitys.ForEach(x =>
                {
                    var transEntity = x.Clone() as BlockReference;
                    var point = x.Position;
                    if (null != originTransformer)
                        point = originTransformer.Transform(point);
                    bool isDel = outPolyline.Contains(point);
                    if (isDel && innerPolylines != null && innerPolylines.Count > 0) 
                    {
                        foreach (var pl in innerPolylines) 
                        {
                            if (!isDel)
                                continue;
                            isDel = !outPolyline.Contains(point);
                        }
                    }
                    if (isDel)
                        delEntitys.Add(x);
                });
                if (delEntitys.Count < 1)
                    return;
                var objs = new DBObjectCollection();
                delEntitys.ForEachDbObject(c => objs.Add(c));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }
        public static void ClearHistoryLines(Database database, string layerName, Polyline outPolyline, List<Polyline> innerPolylines, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                var delCurves = new List<Curve>();
                var targetLines = currentDb.ModelSpace
                   .OfType<Curve>().Where(o => o.Layer == layerName);
                if (targetLines == null || targetLines.Count() < 1)
                    return;
                targetLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    if (null != originTransformer)
                        originTransformer.Transform(transCurve);
                    var sp = transCurve.StartPoint;
                    var ep = transCurve.EndPoint;
                    if(outPolyline.Contains(sp) && outPolyline.Contains(ep))
                        delCurves.Add(x);
                });
                if (delCurves.Count < 1)
                    return;
                var objs = new DBObjectCollection();
                delCurves.ForEachDbObject(c => objs.Add(c));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }
        public static void ChangeBlockDrawOrders(List<ObjectId> blockIds)
        {
            if (null == blockIds || blockIds.Count < 1)
                return;
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                BlockTableRecord block = null;
                DrawOrderTable drawOrder = null;
                foreach (var id in blockIds) 
                {
                    if (id == null || !id.IsValid || id.IsErased)
                        continue;
                    var ent = acdb.ModelSpace.Element(id);
                    block = acdb.Blocks.Element(ent.BlockId);
                    drawOrder = acdb.Element<DrawOrderTable>(block.DrawOrderTableId);
                    break;
                }
                if (null == block || drawOrder == null)
                    return;
                blockIds = blockIds.Distinct().ToList();
                var ids = new ObjectIdCollection();
                blockIds.ForEach(c => ids.Add(c));
                drawOrder.UpgradeOpen();
                drawOrder.MoveToTop(ids);
                drawOrder.DowngradeOpen();
            }
        }
    }
}
