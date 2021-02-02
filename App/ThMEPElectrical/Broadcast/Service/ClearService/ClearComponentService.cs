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
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast.Service.ClearService
{
    public static class ClearComponentService
    {
        /// <summary>
        /// 删除广播图块
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearBroadCast(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BroadcastLayerName);

                //获取广播
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThMEPCommon.BroadcastLayerName &
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
                braodcasts.Where(o => polyline.Contains(o.Position)).ForEachDbObject(o => objs.Add(o));
                foreach (Entity spray in objs)
                {
                    spray.UpgradeOpen();
                    spray.Erase();
                }
            }
        }


        /// <summary>
        /// 删除盲区信息
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearBlindArea(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BlindAreaLayer);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BlindAreaLayer);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BlindAreaLayer);

                var bufferPoly = polyline.Buffer(-1)[0] as Polyline;
                var objs = new DBObjectCollection();
                var blindLines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThMEPCommon.BlindAreaLayer);
                blindLines.ForEachDbObject(x => objs.Add(x));

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var bLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Polyline>().ToList();
                foreach (var line in bLines)
                {
                    line.UpgradeOpen();
                    line.Erase();
                }
            }
        }

        /// <summary>
        /// 删除喷淋布置线
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearPipeLines(this Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.ConnectPipeLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.ConnectPipeLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.ConnectPipeLayerName);
                var objs = new DBObjectCollection();
                var pipeLines = acadDatabase.ModelSpace
                    .OfType<Line>()
                    .Where(o => o.Layer == ThMEPCommon.ConnectPipeLayerName);
                foreach (var line in pipeLines)
                {
                    objs.Add(line);
                }

                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pipes = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Line>().ToList();

                foreach (var sLine in pipes)
                {
                    sLine.UpgradeOpen();
                    sLine.Erase();
                }
            }
        }
    }
}
