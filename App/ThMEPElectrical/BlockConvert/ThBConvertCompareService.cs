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

        public List<Tuple<ObjectId, string>> ObjectIds { get; set; }

        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public ThBConvertCompareService(Database database, List<ThBlockReferenceData> targetBlocks, List<Tuple<ObjectId, string>> objectIds)
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
                var targetEntites = ObjectIds.Select(o => currentDb.Element<BlockReference>(o.Item1, true)).ToList();

                var sourceEntitiesMap = EntitiesMap(sourceEntites);
                var targetEntitiesMap = EntitiesMap(targetEntites);

                var sourceIndex = new ThCADCoreNTSSpatialIndex(sourceEntitiesMap.Select(o => o.Key).ToCollection());
                var targetIndex = new ThCADCoreNTSSpatialIndex(targetEntitiesMap.Select(o => o.Key).ToCollection());

                sourceEntitiesMap.ForEach(o =>
                {
                    var result = new ThBConvertCompareModel
                    {
                        Database = currentDb.Database,
                        SourceID = o.Value.ObjectId,
                    };
                    CompareModels.Add(result);
                    searchedIds.Add(o.Value.ObjectId);

                    var searchCircle = new Circle(o.Key.Position, Vector3d.ZAxis, AllowTolence).TessellateCircleWithArc(100.0);
                    var filterPoint = targetIndex.SelectCrossingPolygon(searchCircle).OfType<DBPoint>().ToList();
                    if (filterPoint.Count > 0)
                    {
                        var sourceName = o.Value.ObjectId.GetBlockName();
                        var filterEntity = filterPoint.Select(p => targetEntitiesMap[p].ObjectId).Where(id => id.GetBlockName().Equals(sourceName));
                        foreach (var id in filterEntity)
                        {
                            result.TargetID = id;
                            result.Type = ThBConvertCompareType.Unchanged;
                            searchedIds.Add(id);
                            return;
                        }
                    }
                    result.Type = ThBConvertCompareType.Delete;
                });
                targetEntitiesMap.ForEach(o =>
                {
                    if (searchedIds.Contains(o.Value.ObjectId))
                    {
                        return;
                    }

                    var result = new ThBConvertCompareModel
                    {
                        Database = currentDb.Database,
                        TargetID = o.Value.ObjectId,
                        Type = ThBConvertCompareType.Add,
                    };
                    CompareModels.Add(result);
                    searchedIds.Add(o.Value.ObjectId);
                });
            }
        }

        private Dictionary<DBPoint, BlockReference> EntitiesMap(List<BlockReference> entites)
        {
            var result = new Dictionary<DBPoint, BlockReference>();
            entites.ForEach(entity =>
            {
                result.Add(new DBPoint(entity.Position), entity);
            });
            return result;
        }

        public void Print()
        {
            using (var acadDatabase = AcadDatabase.Use(Database))
            {
                if (CompareModels.Count > 0 && acadDatabase.Database.Equals(CompareModels[0].Database))
                {
                    CompareModels.ForEach(model =>
                    {
                        switch (model.Type)
                        {
                            case ThBConvertCompareType.Unchanged:
                                break;
                            case ThBConvertCompareType.Delete:
                                Print(acadDatabase, model.SourceID, 1);
                                break;
                            case ThBConvertCompareType.Add:
                                Print(acadDatabase, model.TargetID, 1);
                                break;
                            case ThBConvertCompareType.Displacement:
                                Print(acadDatabase, model.SourceID, 2);
                                Print(acadDatabase, model.TargetID, 2);
                                break;
                            case ThBConvertCompareType.ParameterChange:
                                Print(acadDatabase, model.SourceID, 3);
                                Print(acadDatabase, model.TargetID, 3);
                                break;
                            case ThBConvertCompareType.RepetitiveID:
                                Print(acadDatabase, model.SourceID, 5);
                                Print(acadDatabase, model.TargetID, 5);
                                break;
                        }
                    });
                }
            }
        }

        private void Print(AcadDatabase acadDatabase, ObjectId objectId, short colorIndex)
        {
            if (!objectId.Equals(ObjectId.Null))
            {
                var block = acadDatabase.Element<BlockReference>(objectId, true);
                var name = objectId.GetBlockName();
                if (name.KeepChinese().Equals(ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION))
                {
                    var objs = new DBObjectCollection();
                    block.Explode(objs);
                    var motor = objs.OfType<BlockReference>().First();
                    var obb = motor.ToOBB();
                    ThBConvertUtils.InsertRevcloud(acadDatabase.Database, obb, colorIndex);
                }
                else if (name.KeepChinese().Equals(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                {
                    var obb = block.Position.CreateSquare(100.0);
                    ThBConvertUtils.InsertRevcloud(acadDatabase.Database, obb, colorIndex);
                }
                else
                {
                    var obb = block.ToOBB();
                    ThBConvertUtils.InsertRevcloud(acadDatabase.Database, obb, colorIndex);
                }
            }
        }

        public void Update()
        {
            using (var currentDb = AcadDatabase.Use(Database))
            {
                var sourceEntites = TargetBlocks.Select(o => currentDb.Element<BlockReference>(o.ObjId, true)).ToList();
                sourceEntites.ForEach(o => o.Erase());

                ObjectIds.ForEach(o =>
                {
                    ThBConvertDbUtils.UpdateLayerSettings(o.Item2);
                    var block = currentDb.Element<Entity>(o.Item1, true);
                    block.Layer = o.Item2;
                });

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
    }
}
