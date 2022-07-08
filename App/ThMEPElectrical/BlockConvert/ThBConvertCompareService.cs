using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareService
    {
        public double AllowTolence = 500.0;

        public Database Database { get; set; }

        public List<ThBlockReferenceData> TargetBlocks { get; set; }

        public Dictionary<ObjectId, string> ObjectIds { get; set; }

        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public ThBConvertCompareService(Database database, List<ThBlockReferenceData> targetBlocks, Dictionary<ObjectId, string> objectIds)
        {
            Database = database;
            TargetBlocks = targetBlocks;
            ObjectIds = objectIds;
            CompareModels = new List<ThBConvertCompareModel>();
        }

        public void Compare()
        {
            using (var currentDb = AcadDatabase.Use(Database))
            {
                var searchedIds = new List<ObjectId>();
                var sourceEntites = TargetBlocks.Select(o => currentDb.Element<BlockReference>(o.ObjId, true)).ToList();
                var targetEntites = ObjectIds.Select(o => currentDb.Element<BlockReference>(o.Key, true)).ToList();

                var sourceEntitiesMap = EntitiesMap(sourceEntites);
                var targetEntitiesMap = EntitiesMap(targetEntites);

                for (var i = 0; i < sourceEntitiesMap.Count; i++)
                {
                    var sourceIndex = new ThCADCoreNTSSpatialIndex(sourceEntitiesMap[i].Select(o => o.Key).ToCollection());
                    var targetIndex = new ThCADCoreNTSSpatialIndex(targetEntitiesMap[i].Select(o => o.Key).ToCollection());

                    sourceEntitiesMap[i].ForEach(o =>
                    {
                        var result = new ThBConvertCompareModel
                        {
                            Database = currentDb.Database,
                            SourceId = o.Value.ObjectId,
                        };
                        CompareModels.Add(result);
                        searchedIds.Add(o.Value.ObjectId);

                        var searchCircle = new Circle(o.Key.Position, Vector3d.ZAxis, AllowTolence).TessellateCircleWithArc(100.0);
                        if (i == 0 || i == 1)
                        {
                            var sourceAttributes = o.Value.ObjectId.GetAttributesInBlockReference();
                            var loadId = sourceAttributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID];
                            var targetLoadList = targetEntitiesMap[i].Where(e => GetLoadId(e.Value).Equals(loadId))
                                .OrderBy(e => e.Value.Position.DistanceTo(o.Value.Position)).ToList();
                            if (targetLoadList.Count > 0)
                            {
                                var targetAttributes = targetLoadList[0].Value.ObjectId.GetAttributesInBlockReference();
                                if (searchCircle.Contains(targetLoadList[0].Key.Position))
                                {
                                    if (ParameterChange(sourceAttributes, targetAttributes))
                                    {
                                        result.Type = ThBConvertCompareType.ParameterChange;
                                    }
                                    else
                                    {
                                        result.Type = ThBConvertCompareType.Unchanged;
                                    }

                                    result.TargetId = targetLoadList[0].Value.ObjectId;
                                    searchedIds.Add(targetLoadList[0].Value.ObjectId);
                                }
                                else if (string.IsNullOrEmpty(loadId))
                                {
                                    result.Type = ThBConvertCompareType.Delete;
                                }
                                else
                                {
                                    result.TargetId = targetLoadList[0].Value.ObjectId;
                                    result.Type = ThBConvertCompareType.Displacement;
                                    searchedIds.Add(targetLoadList[0].Value.ObjectId);

                                    if (ParameterChange(sourceAttributes, targetAttributes))
                                    {
                                        var model = new ThBConvertCompareModel
                                        {
                                            Database = currentDb.Database,
                                            SourceId = o.Value.ObjectId,
                                            TargetId = targetLoadList[0].Value.ObjectId,
                                            Type = ThBConvertCompareType.ParameterChange,
                                        };
                                        CompareModels.Add(model);
                                    }
                                }
                            }
                            else
                            {
                                result.Type = ThBConvertCompareType.Delete;
                            }
                        }
                        else if (i == 2)
                        {
                            var sourceAttributes = o.Value.ObjectId.GetAttributesInBlockReference();
                            var filterPoint = targetIndex.SelectCrossingPolygon(searchCircle).OfType<DBPoint>().ToList();
                            if (filterPoint.Count > 0)
                            {
                                var filterEntityId = filterPoint.OrderBy(p => p.Position.DistanceTo(o.Value.Position))
                                    .Select(p => targetEntitiesMap[i][p].ObjectId).First();
                                var targetAttributes = filterEntityId.GetAttributesInBlockReference();
                                if (ParameterChange(sourceAttributes, targetAttributes))
                                {
                                    result.TargetId = filterEntityId;
                                    result.Type = ThBConvertCompareType.ParameterChange;
                                }
                            }
                            else
                            {
                                result.Type = ThBConvertCompareType.Delete;
                            }
                        }
                        else
                        {
                            var sourceName = o.Value.ObjectId.GetBlockName();
                            var filterPoint = targetIndex.SelectCrossingPolygon(searchCircle).OfType<DBPoint>().ToList();
                            if (filterPoint.Count > 0)
                            {
                                var filterEntity = filterPoint.Select(p => targetEntitiesMap[i][p].ObjectId).Where(id => id.GetBlockName().Equals(sourceName));
                                foreach (var id in filterEntity)
                                {
                                    result.TargetId = id;
                                    result.Type = ThBConvertCompareType.Unchanged;
                                    searchedIds.Add(id);
                                    return;
                                }
                            }
                            result.Type = ThBConvertCompareType.Delete;
                        }
                    });

                    if (i == 0 || i == 1)
                    {
                        var localSearchIds = new List<ObjectId>();
                        targetEntitiesMap[i].ForEach(o =>
                        {
                            if (localSearchIds.Contains(o.Value.ObjectId))
                            {
                                return;
                            }

                            var sourceAttributes = o.Value.ObjectId.GetAttributesInBlockReference();
                            var loadId = sourceAttributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID];
                            var targetLoadList = targetEntitiesMap[i].Where(e => GetLoadId(e.Value).Equals(loadId));
                            if (targetLoadList.Count() > 1)
                            {
                                var result = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    TargetIdList = targetLoadList.Select(e => e.Value.ObjectId).ToList(),
                                    Type = ThBConvertCompareType.RepetitiveID,
                                };
                                CompareModels.Add(result);
                                localSearchIds.AddRange(result.TargetIdList);
                            }
                        });
                    }
                    else
                    {
                        targetEntitiesMap[i].ForEach(o =>
                        {
                            if (searchedIds.Contains(o.Value.ObjectId))
                            {
                                return;
                            }

                            var result = new ThBConvertCompareModel
                            {
                                Database = currentDb.Database,
                                TargetId = o.Value.ObjectId,
                                Type = ThBConvertCompareType.Add,
                            };
                            CompareModels.Add(result);
                            searchedIds.Add(o.Value.ObjectId);
                        });
                    }
                }
            }
        }

        private string GetLoadId(BlockReference block)
        {
            return block.ObjectId.GetAttributesInBlockReference()[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID];
        }

        private bool ParameterChange(SortedDictionary<string, string> sourceAttributes, SortedDictionary<string, string> targetAttributes)
        {
            var parameterChange = false;
            sourceAttributes.ForEach(e =>
            {
                if (e.Key.Equals(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID))
                {
                    return;
                }

                if (!targetAttributes.ContainsKey(e.Key) || !targetAttributes[e.Key].Equals(e.Value))
                {
                    parameterChange = true;
                }
            });
            return parameterChange;
        }

        private List<Dictionary<DBPoint, BlockReference>> EntitiesMap(List<BlockReference> entites)
        {
            var result = new List<Dictionary<DBPoint, BlockReference>>
            {
                new Dictionary<DBPoint, BlockReference>(),
                new Dictionary<DBPoint, BlockReference>(),
                new Dictionary<DBPoint, BlockReference>(),
                new Dictionary<DBPoint, BlockReference>(),
            };
            entites.ForEach(entity =>
            {
                var name = entity.GetBlockName().KeepChinese();
                if (name.Equals(ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION))
                {
                    result[0].Add(new DBPoint(entity.Position), entity);
                }
                else if (name.Equals(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                {
                    result[1].Add(new DBPoint(entity.Position), entity);
                }
                else if (name.Equals(ThBConvertCommon.BLOCK_PUMP_LABEL))
                {
                    result[2].Add(new DBPoint(entity.Position), entity);
                }
                else
                {
                    result[3].Add(new DBPoint(entity.Position), entity);
                }
            });
            return result;
        }

        public void Update()
        {
            using (var currentDb = AcadDatabase.Use(Database))
            {
                if (CompareModels.Count > 0 && currentDb.Database.Equals(CompareModels[0].Database))
                {
                    CompareModels.ForEach(model =>
                    {
                        switch (model.Type)
                        {
                            case ThBConvertCompareType.Unchanged:
                                break;
                            case ThBConvertCompareType.Delete:
                                currentDb.Element<BlockReference>(model.SourceId, true).Erase();

                                Print(currentDb, model.SourceId, 1, ThBConvertCommon.LINE_TYPE_HIDDEN);
                                break;
                            case ThBConvertCompareType.Add:
                                ThBConvertDbUtils.UpdateLayerSettings(ObjectIds[model.TargetId]);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = ObjectIds[model.TargetId];

                                Print(currentDb, model.TargetId, 1, ThBConvertCommon.LINE_TYPE_CONTINUOUS);
                                break;
                            case ThBConvertCompareType.Displacement:
                                currentDb.Element<BlockReference>(model.SourceId, true).Erase();
                                ThBConvertDbUtils.UpdateLayerSettings(ObjectIds[model.TargetId]);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = ObjectIds[model.TargetId];

                                Print(currentDb, model.SourceId, 2, ThBConvertCommon.LINE_TYPE_HIDDEN);
                                Print(currentDb, model.TargetId, 2, ThBConvertCommon.LINE_TYPE_CONTINUOUS);
                                break;
                            case ThBConvertCompareType.ParameterChange:
                                currentDb.Element<BlockReference>(model.SourceId, true).Erase();
                                ThBConvertDbUtils.UpdateLayerSettings(ObjectIds[model.TargetId]);
                                currentDb.Element<BlockReference>(model.TargetId, true).Layer = ObjectIds[model.TargetId];

                                Print(currentDb, model.SourceId, 3, ThBConvertCommon.LINE_TYPE_HIDDEN);
                                Print(currentDb, model.TargetId, 3, ThBConvertCommon.LINE_TYPE_CONTINUOUS);
                                break;
                            case ThBConvertCompareType.RepetitiveID:
                                model.TargetIdList.ForEach(o =>
                                {
                                    ThBConvertDbUtils.UpdateLayerSettings(ObjectIds[o]);
                                    currentDb.Element<BlockReference>(o, true).Layer = ObjectIds[o];

                                    Print(currentDb, o, 5, ThBConvertCommon.LINE_TYPE_CONTINUOUS);
                                });
                                break;
                        }
                    });
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
        }

        private void Print(AcadDatabase acadDatabase, ObjectId objectId, short colorIndex, string lineType)
        {
            var obb = ThBConvertObbService.BlockObb(acadDatabase, objectId);
            ThBConvertUtils.InsertRevcloud(acadDatabase.Database, obb, colorIndex, lineType);
        }
    }
}
