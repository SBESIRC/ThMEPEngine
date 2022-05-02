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
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.Broadcast.Service.ClearService
{
    public static class ClearComponentService
    {
        /// <summary>
        /// 删除广播图块
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearBroadCast(this Polyline polyline, ThMEPOriginTransformer originTransformer, List<Polyline> otherFrames)
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
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                o.Dxf((int)DxfCode.BlockName) == string.Join(",", new string[] {
                    "E-BFAS410-2",
                    "E-BFAS410-3",
                    "E-BFAS410-4",
                }));
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
                    return polyline.Contains(transBlock.Position) && !otherFrames.Any(x=>x.Contains(transBlock.Position)); 
                }).ForEachDbObject(o => objs.Add(o));
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
        public static void ClearBlindArea(this Polyline polyline, ThMEPOriginTransformer originTransformer)
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
                var transDic = objs.Cast<Polyline>().ToDictionary(
                    x =>
                    {
                        var transArea = x.Clone() as Polyline;
                        originTransformer.Transform(transArea);
                        return transArea;
                    },
                    y => y
                );
                var transObjs = transDic.Keys.ToList().ToCollection();

                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(transObjs);
                var bLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(bufferPoly).Cast<Polyline>().ToList();
                foreach (var transLine in bLines)
                {
                    var line = transDic[transLine];
                    line.UpgradeOpen();
                    line.Erase();
                }
            }
        }

        /// <summary>
        /// 删除喷淋布置线
        /// </summary>
        /// <param name="polyline"></param>
        public static void ClearPipeLines(this Polyline polyline, ThMEPOriginTransformer originTransformer)
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

                var transDic = objs.Cast<Line>().ToDictionary(
                    x =>
                    {
                        var transLine = x.Clone() as Line;
                        originTransformer.Transform(transLine);
                        return transLine;
                    },
                    y => y
                );
                var transObjs = transDic.Keys.ToList().ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(transObjs);
                var pipes = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Line>().ToList();
                foreach (var transLine in pipes)
                {
                    var sLine = transDic[transLine];
                    sLine.UpgradeOpen();
                    sLine.Erase();
                }
            }
        }

        /// <summary>
        /// 获取房间内部的房间线（mpolygon）
        /// </summary>
        /// <param name="polyline"></param>
        public static List<Polyline> GetInnerFrames(this Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.FrameLayer);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.FrameLayer);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.FrameLayer);
                var frameLines = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == ThMEPCommon.FrameLayer)
                    .Select(x => {
                        var clonePoly = x.Clone() as Polyline;
                        originTransformer.Transform(clonePoly);
                        return clonePoly;
                    })
                    .Where(x=> Math.Abs(x.Area - polyline.Area) > 1000)
                    .Where(x=>polyline.Contains(x))
                    .ToList();
                return frameLines;
            }
        }
    }
}
