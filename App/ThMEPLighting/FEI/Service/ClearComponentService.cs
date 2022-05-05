using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.FEI.Service
{
    public static class ClearComponentService
    {
        /// <summary>
        /// 删除疏散路径
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearLines(this Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //操作图层
                OperationLayer();

                var objs = new DBObjectCollection();
                var lines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME
                    || o.Layer == ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME
                    || o.Layer == ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME
                    || o.Layer == ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                foreach (var line in lines)
                {
                    objs.Add(line);
                }

                var transDic = objs.Cast<Polyline>().ToDictionary(
                    x =>
                    {
                        var transLine = x.Clone() as Polyline;
                        originTransformer.Transform(transLine);
                        return transLine;
                    },
                    y => y
                );
                var transObjs = transDic.Keys.ToList().ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(transObjs);
                var pipes = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Polyline>().ToList();
                foreach (var transLine in pipes)
                {
                    var sLine = transDic[transLine];
                    sLine.UpgradeOpen();
                    sLine.Erase();
                }
            }
        }

        /// <summary>
        /// 清除上一次布置的出入口图块
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearExitBlock(this Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.CreateAILayer(ThMEPLightingCommon.EmgLightLayerName, (int)ColorIndex.BYLAYER);
                //出入口图块
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThMEPLightingCommon.EmgLightLayerName &
                (o.Dxf((int)DxfCode.BlockName) == ThMEPLightingCommon.ExitEBlockName | 
                o.Dxf((int)DxfCode.BlockName) == ThMEPLightingCommon.ExitSBlockName) &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var blocks = new List<BlockReference>();
                var allBlocks = Active.Editor.SelectAll(filterlist);
                if (allBlocks.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allBlocks.Value.GetObjectIds())
                        {
                            blocks.Add(acdb.Element<BlockReference>(obj));
                        }
                    }
                }
                var objs = new DBObjectCollection();
                blocks.Where(o => polyline.Contains(originTransformer.Transform(o.Position)))
                .ForEachDbObject(o => objs.Add(o));
                foreach (BlockReference block in objs)
                {
                    block.UpgradeOpen();
                    block.Erase();
                }
            }
        }

        /// <summary>
        /// 操作图层
        /// </summary>
        public static void OperationLayer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnLockLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnOffLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnPrintLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);

                acadDatabase.Database.UnFrozenLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnLockLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnOffLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnPrintLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);

                acadDatabase.Database.UnFrozenLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnLockLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnOffLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.UnPrintLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);

                acadDatabase.Database.UnFrozenLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnLockLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnOffLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.UnPrintLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
            }
        }
    }
}
