using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
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
