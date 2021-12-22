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
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    public static class CreateClearEmgLamp
    {
        static List<string> lampBlockNames = new List<string>()
            {
                ThMEPLightingCommon.PILOTLAMP_WALL_TWOWAY_SINGLESIDE,
                ThMEPLightingCommon.PILOTLAMP_WALL_ONEWAY_SINGLESIDE,
                ThMEPLightingCommon.PILOTLAMP_HOST_TWOWAY_SINGLESIDE,
                ThMEPLightingCommon.PILOTLAMP_HOST_TWOWAY_DOUBLESIDE,
                ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_SINGLESIDE,
                ThMEPLightingCommon.PILOTLAMP_HOST_ONEWAY_DOUBLESIDE
            };
        public static ObjectId CreatePilotLamp(this Database database, Point3d pt, Vector3d layoutDir,string blockName,bool isHosting, Dictionary<string, string> attNameValues,double scaleNum) 
        {
            double rotateAngle = Vector3d.XAxis.GetAngleTo(layoutDir);
            //控制旋转角度
            if (layoutDir.DotProduct(Vector3d.YAxis) < 0)
            {
                rotateAngle = -rotateAngle;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                
                var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPLightingCommon.EmgLightLayerName,
                    blockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle,
                    attNameValues);
                if (null != id && isHosting && id.IsValid)
                {
                    rotateAngle = rotateAngle % Math.PI;
                    if (rotateAngle > Math.PI / 2) 
                    {
                        Point3d point1 = pt;
                        Point3d point2 = pt + layoutDir.MultiplyBy(100);
                        id = id.Mirror(point1, point2, true);
                    }
                }
                return id;
            }
        }

        public static void ClearPolylineInnerBlock(this Polyline polyline, ThMEPOriginTransformer originTransformer,string layName, string blockName) 
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layName);
                acadDatabase.Database.UnLockLayer(layName);
                acadDatabase.Database.UnOffLayer(layName);

                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.BlockName) == blockName &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var braodcasts = new List<BlockReference>();
                var allBraodcasts = Active.Editor.SelectAll(filterlist);
                if (allBraodcasts.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allBraodcasts.Value.GetObjectIds())
                        {
                            braodcasts.Add(acdb.Element<BlockReference>(obj));
                        }
                    }
                }
                var objs = new DBObjectCollection();
                braodcasts.Where(o =>
                {
                    var transBlock = o.Clone() as BlockReference;
                    originTransformer.Transform(transBlock);
                    return polyline.Contains(transBlock.Position);
                }).ForEachDbObject(o => objs.Add(o));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }

        public static void ClearPolylineInnerBlocks(this Polyline polyline, ThMEPOriginTransformer originTransformer, List<string> blockNames)
        {
            foreach (var item in blockNames)
            {
                if (string.IsNullOrEmpty(item))
                    continue;
                ClearPolylineInnerBlock(polyline, originTransformer, ThMEPLightingCommon.EmgLightLayerName, item);
            }
        }
        public static void ClearExtractRevCloud(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer,int colorId)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<Polyline>()
                      .Where(o => o.Layer == LayerName);

                List<Polyline> lineList = line.Select(x => x.WashClone()).Cast<Polyline>().ToList();
                var delPlines = new List<Polyline>();
                foreach (Polyline pl in line)
                {
                    if (pl.ColorIndex != colorId)
                        continue;
                    var plTrans = pl.WashClone() as Polyline;
                    transformer.Transform(plTrans);
                    if (bufferFrame.Contains(plTrans))
                        delPlines.Add(pl);
                }
                delPlines.ForEachDbObject(c => objs.Add(c));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }
        public static void ClearEFIExitPilotLamp(this Polyline polyline, ThMEPOriginTransformer originTransformer) 
        {
            
            ClearPolylineInnerBlocks(polyline, originTransformer, lampBlockNames);
        }

        public static void LoadBlockToDocument(this Database database) 
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in lampBlockNames) 
                {
                    if (item == null)
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, false);
                }
            }
        }
    
    }
}
