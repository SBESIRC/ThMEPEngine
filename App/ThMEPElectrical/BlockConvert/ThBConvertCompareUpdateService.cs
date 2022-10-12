using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareUpdateService
    {
        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public List<ThBConvertEntityInfos> TarEntityInfos { get; set; }

        public ThBConvertCompareUpdateService(List<ThBConvertCompareModel> compareModels, List<ThBConvertEntityInfos> tarEntityInfos)
        {
            CompareModels = compareModels;
            TarEntityInfos = tarEntityInfos;
        }

        public void Update(double scale)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            {
                if (CompareModels.Count > 0 && currentDb.Database.Equals(CompareModels[0].Database))
                {
                    var noteLayer = ThBConvertUtils.CreateAILayer(currentDb.Database, ThBConvertCommon.NOTE_LAYER);
                    var printParameterList = new List<ThRevcloudParameter>();
                    CompareModels.ForEach(model =>
                    {
                        var layer = GetLayer(TarEntityInfos, model.TargetId);
                        switch (model.Type)
                        {
                            case ThBConvertCompareType.Unchanged:
                                break;
                            case ThBConvertCompareType.Delete:
                                printParameterList.Add(GetParameter(currentDb, model.SourceId, 1, ThBConvertCommon.LINE_TYPE_HIDDEN, scale));
                                currentDb.Element<BlockReference>(model.SourceId, true).Erase();
                                break;
                            case ThBConvertCompareType.Add:
                                ThBConvertDbUtils.UpdateLayerSettings(layer);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = layer;
                                printParameterList.Add(GetParameter(currentDb, model.TargetId, 1, ThBConvertCommon.LINE_TYPE_CONTINUOUS, scale));
                                break;
                            case ThBConvertCompareType.Displacement:
                                ThBConvertDbUtils.UpdateLayerSettings(layer);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = layer;
                                printParameterList.Add(GetParameter(currentDb, model.SourceId, 2, ThBConvertCommon.LINE_TYPE_HIDDEN, scale));
                                printParameterList.Add(GetParameter(currentDb, model.TargetId, 2, ThBConvertCommon.LINE_TYPE_CONTINUOUS, scale));
                                currentDb.Element<BlockReference>(model.SourceId, true).Erase();
                                break;
                            case ThBConvertCompareType.ParameterChange:
                                var srcBlock = currentDb.Element<BlockReference>(model.SourceId, true);
                                RemainLabel(currentDb, srcBlock, noteLayer);
                                ThBConvertDbUtils.UpdateLayerSettings(layer);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = layer;
                                printParameterList.Add(PrintLabelParameter(currentDb, model.SourceId, 3, ThBConvertCommon.LINE_TYPE_HIDDEN, scale));
                                printParameterList.Add(PrintLabelParameter(currentDb, model.TargetId, 3, ThBConvertCommon.LINE_TYPE_CONTINUOUS, scale));
                                break;
                            case ThBConvertCompareType.RepetitiveID:
                                model.TargetIdList.ForEach(o =>
                                {
                                    //var oLayer = GetLayer(TarEntityInfos, o);
                                    //ThBConvertDbUtils.UpdateLayerSettings(oLayer);
                                    //currentDb.Element<BlockReference>(o, true).Layer = oLayer;
                                    printParameterList.Add(PrintLabelParameter(currentDb, o, 5, ThBConvertCommon.LINE_TYPE_CONTINUOUS, scale));
                                });
                                break;
                        }
                    });
                    ThInsertRevcloud.Set(printParameterList);
                    CommandHandlerBase.ExecuteFromCommandLine(false, "THCOMPAREREVCLOUD");
                }

                var ltr = currentDb.Layers.ElementOrDefault(ThBConvertCommon.HIDING_LAYER, true);
                if (ltr == null)
                {
                    return;
                }

                var idCollection = new ObjectIdCollection
                {
                    ltr.Id,
                };
                currentDb.Database.Purge(idCollection);
                if (idCollection.Count > 0)
                {
                    ltr.Erase();
                }
            }
            Active.Editor.Regen();
        }

        private ThRevcloudParameter GetParameter(AcadDatabase acadDatabase, ObjectId objectId, short colorIndex, string lineType, double scale)
        {
            var obb = ThBConvertObbService.BlockObb(acadDatabase, objectId, scale);
            return new ThRevcloudParameter(acadDatabase.Database, obb, colorIndex, lineType, scale);
        }

        private ThRevcloudParameter PrintLabelParameter(AcadDatabase acadDatabase, ObjectId objectId, short colorIndex, string lineType, double scale)
        {
            var obb = ThBConvertObbService.BlockLabelObb(acadDatabase, objectId, scale);
            return new ThRevcloudParameter(acadDatabase.Database, obb, colorIndex, lineType, scale);
        }

        private void RemainLabel(AcadDatabase currentDb, BlockReference block, ObjectId layerId)
        {
            if (layerId != null)
            {
                var objs = new DBObjectCollection();
                block.Burst(objs);
                objs.OfType<Entity>().ForEach(e =>
                {
                    if (e.Layer.Equals(ThBConvertCommon.BLOCK_PUMP_LABEL_LAYER))
                    {
                        currentDb.ModelSpace.Add(e);
                        e.LayerId = layerId;
                    }
                });
            }
            block.Erase();
        }

        private string GetLayer(List<ThBConvertEntityInfos> infos, ObjectId objectId)
        {
            if (objectId == ObjectId.Null)
            {
                return "";
            }
            return infos.Where(info => info.ObjectId.Equals(objectId)).First().Layer;
        }
    }
}
