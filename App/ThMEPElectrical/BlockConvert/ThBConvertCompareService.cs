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

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertCompareService
    {
        public double AllowTolence = 500.0;

        public Database Database { get; set; }

        public List<ThBConvertEntityInfos> SrcEntityInfos { get; set; }

        public List<ThBConvertEntityInfos> TarEntityInfos { get; set; }

        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public ThBConvertCompareService(Database database, List<ThBConvertEntityInfos> srcEntityInfos, List<ThBConvertEntityInfos> tarEntityInfos)
        {
            Database = database;
            SrcEntityInfos = srcEntityInfos;
            TarEntityInfos = tarEntityInfos;
            CompareModels = new List<ThBConvertCompareModel>();
        }

        public void Compare()
        {
            using (var docLock = Database.GetDocument().LockDocument())
            using (var currentDb = AcadDatabase.Use(Database))
            {
                var searchedIds = new List<ObjectId>();
                var sourceEntites = SrcEntityInfos.Select(o => currentDb.Element<BlockReference>(o.ObjectId, true)).ToList();
                var targetEntites = TarEntityInfos.Select(o => currentDb.Element<BlockReference>(o.ObjectId, true)).ToList();

                var sourceEntitiesMap = EntitiesMap(sourceEntites);
                var targetEntitiesMap = EntitiesMap(targetEntites);

                for (var i = 0; i < sourceEntitiesMap.Count; i++)
                {
                    var sourceIndex = new ThCADCoreNTSSpatialIndex(sourceEntitiesMap[i].Select(o => o.Key).ToCollection());
                    var targetIndex = new ThCADCoreNTSSpatialIndex(targetEntitiesMap[i].Select(o => o.Key).ToCollection());

                    sourceEntitiesMap[i].ForEach(o =>
                    {
                        searchedIds.Add(o.Value.ObjectId);
                        // 电动机及负载标注、负载标注
                        if (i == 0 || i == 1)
                        {
                            var sourceAttributes = o.Value.ObjectId.GetAttributesInBlockReference();
                            var loadId = sourceAttributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID];
                            var targetLoadList = targetEntitiesMap[i].Where(e => GetLoadId(e.Value).Equals(loadId))
                                .OrderBy(e => e.Value.Position.DistanceTo(o.Value.Position)).ToList();
                            if (targetLoadList.Count > 0)
                            {
                                var targetAttributes = targetLoadList[0].Value.ObjectId.GetAttributesInBlockReference();
                                if (o.Key.Position.DistanceTo(targetLoadList[0].Key.Position) < 10.0)
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        SourceId = o.Value.ObjectId,
                                        TargetId = targetLoadList[0].Value.ObjectId,
                                        Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                        EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                        Type = ThBConvertCompareType.Unchanged,
                                    };
                                    CompareModels.Add(result);
                                    searchedIds.Add(targetLoadList[0].Value.ObjectId);

                                    if (ParameterChange(sourceAttributes, targetAttributes))
                                    {
                                        var parameterResult = new ThBConvertCompareModel
                                        {
                                            Database = currentDb.Database,
                                            SourceId = o.Value.ObjectId,
                                            TargetId = targetLoadList[0].Value.ObjectId,
                                            Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                            EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                            Type = ThBConvertCompareType.ParameterChange,
                                        };
                                        CompareModels.Add(parameterResult);
                                    }
                                }
                                else if (string.IsNullOrEmpty(loadId))
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        SourceId = o.Value.ObjectId,
                                        Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                        EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                        Type = ThBConvertCompareType.Delete,
                                    };
                                    CompareModels.Add(result);
                                }
                                else
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        SourceId = o.Value.ObjectId,
                                        TargetId = targetLoadList[0].Value.ObjectId,
                                        Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                        EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                        Type = ThBConvertCompareType.Displacement,
                                    };
                                    CompareModels.Add(result);
                                    searchedIds.Add(targetLoadList[0].Value.ObjectId);

                                    if (ParameterChange(sourceAttributes, targetAttributes))
                                    {
                                        var model = new ThBConvertCompareModel
                                        {
                                            Database = currentDb.Database,
                                            SourceId = o.Value.ObjectId,
                                            TargetId = targetLoadList[0].Value.ObjectId,
                                            Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                            EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                            Type = ThBConvertCompareType.ParameterChange,
                                        };
                                        CompareModels.Add(model);
                                    }
                                }
                            }
                            else
                            {
                                var model = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    SourceId = o.Value.ObjectId,
                                    Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                    EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                    Type = ThBConvertCompareType.Delete,
                                };
                                CompareModels.Add(model);
                            }
                        }
                        else if (i == 2)
                        {
                            var searchCircle = new Circle(o.Key.Position, Vector3d.ZAxis, AllowTolence).TessellateCircleWithArc(100.0);
                            var sourceAttributes = o.Value.ObjectId.GetAttributesInBlockReference();
                            var filterPoint = targetIndex.SelectCrossingPolygon(searchCircle).OfType<DBPoint>().ToList();
                            if (filterPoint.Count > 0)
                            {
                                var filterEntityId = filterPoint.OrderBy(p => p.Position.DistanceTo(o.Value.Position))
                                    .Select(p => targetEntitiesMap[i][p].ObjectId).First();
                                var targetAttributes = filterEntityId.GetAttributesInBlockReference();
                                var model = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    SourceId = o.Value.ObjectId,
                                    TargetId = filterEntityId,
                                    Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                    EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                    Type = ThBConvertCompareType.Unchanged,
                                };
                                CompareModels.Add(model);

                                if (ParameterChange(sourceAttributes, targetAttributes))
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        SourceId = o.Value.ObjectId,
                                        TargetId = filterEntityId,
                                        Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                        EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                        Type = ThBConvertCompareType.ParameterChange,
                                    };
                                    CompareModels.Add(result);
                                }
                            }
                            else
                            {
                                var result = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    SourceId = o.Value.ObjectId,
                                    Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                    EquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId),
                                    Type = ThBConvertCompareType.Delete,
                                };
                                CompareModels.Add(result);
                            }
                        }
                        else
                        {
                            var searchCircle = new Circle(o.Key.Position, Vector3d.ZAxis, AllowTolence).TessellateCircleWithArc(100.0);
                            var srcEquimentType = GetEquimentType(SrcEntityInfos, o.Value.ObjectId);
                            var filterPoint = targetIndex.SelectCrossingPolygon(searchCircle).OfType<DBPoint>().ToList();
                            // 匹配设备类型相同的块
                            var filterEntity = filterPoint.Select(p => targetEntitiesMap[i][p].ObjectId)
                                .Where(id => GetEquimentType(TarEntityInfos, id).Equals(srcEquimentType)).FirstOrDefault();
                            if (filterEntity != ObjectId.Null)
                            {
                                var result = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    SourceId = o.Value.ObjectId,
                                    TargetId = filterEntity,
                                    Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                    EquimentType = srcEquimentType,
                                    Type = ThBConvertCompareType.Unchanged,
                                };
                                CompareModels.Add(result);
                                searchedIds.Add(filterEntity);
                            }
                            else
                            {
                                var result = new ThBConvertCompareModel
                                {
                                    Database = currentDb.Database,
                                    SourceId = o.Value.ObjectId,
                                    Category = GetCategory(SrcEntityInfos, o.Value.ObjectId),
                                    EquimentType = srcEquimentType,
                                    Type = ThBConvertCompareType.Delete,
                                };
                                CompareModels.Add(result);
                            }
                        }
                    });

                    if (i == 0 || i == 1)
                    {
                        var searchRepetitiveIds = new List<ObjectId>();
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
                                var targetIdList = targetLoadList.Select(e => e.Value.ObjectId).Except(searchRepetitiveIds).ToList();
                                if (targetIdList.Count > 0)
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        TargetIdList = targetIdList,
                                        Category = GetCategory(TarEntityInfos, targetLoadList.First().Value.ObjectId),
                                        EquimentType = GetEquimentType(TarEntityInfos, targetLoadList.First().Value.ObjectId),
                                        Type = ThBConvertCompareType.RepetitiveID,
                                    };
                                    CompareModels.Add(result);
                                    searchRepetitiveIds.AddRange(result.TargetIdList);
                                }
                            }
                            targetLoadList.ForEach(pair =>
                            {
                                if (!searchedIds.Contains(pair.Value.ObjectId) && !localSearchIds.Contains(pair.Value.ObjectId))
                                {
                                    var result = new ThBConvertCompareModel
                                    {
                                        Database = currentDb.Database,
                                        TargetId = pair.Value.ObjectId,
                                        Category = GetCategory(TarEntityInfos, pair.Value.ObjectId),
                                        EquimentType = GetEquimentType(TarEntityInfos, pair.Value.ObjectId),
                                        Type = ThBConvertCompareType.Add,
                                    };
                                    CompareModels.Add(result);
                                    localSearchIds.Add(pair.Value.ObjectId);
                                }
                            });
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
                                Category = GetCategory(TarEntityInfos, o.Value.ObjectId),
                                EquimentType = GetEquimentType(TarEntityInfos, o.Value.ObjectId),
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

        private EquimentCategory GetCategory(List<ThBConvertEntityInfos> infos, ObjectId objectId)
        {
            if (objectId == ObjectId.Null)
            {
                return EquimentCategory.暖通;
            }
            return infos.Where(info => info.ObjectId.Equals(objectId)).First().Category;
        }

        private string GetEquimentType(List<ThBConvertEntityInfos> infos, ObjectId objectId)
        {
            if (objectId == ObjectId.Null)
            {
                return "";
            }
            return infos.Where(info => info.ObjectId.Equals(objectId)).First().EquimentType;
        }
    }
}
