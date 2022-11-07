using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.BlockConvert.Model;

namespace ThMEPElectrical.BlockConvert
{
    public class ThTCHConvertService
    {
        private readonly static string TCHConvertConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "天正提资转换配置表.xlsx");

        /// <summary>
        /// 当前database
        /// </summary>
        public AcadDatabase CurrentDb { get; set; }

        /// <summary>
        /// 框选范围
        /// </summary>
        public Polyline Frame { get; set; }

        /// <summary>
        /// 模式
        /// </summary>
        public ConvertMode Mode { get; set; }

        /// <summary>
        /// 专业
        /// </summary>
        public ConvertCategory Category { get; set; }

        /// <summary>
        /// 图纸比例
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// 转换结果
        /// </summary>
        public List<ThBConvertEntityInfos> EntityInfos { get; set; }

        public ThTCHConvertService(AcadDatabase currentDb, Polyline frame, ConvertMode mode, ConvertCategory category, double scale)
        {
            CurrentDb = currentDb;
            Frame = frame;
            Mode = mode;
            Category = category;
            Scale = scale;
            EntityInfos = new List<ThBConvertEntityInfos>();
        }

        public ThBConvertManager ReadFile(List<string> tchSrcNames, List<string> tchTargetNames)
        {
            // 获取配置表信息
            // 将源块-转换前的块的信息存进rule.Transformation.Item1中，目标块-转换后的块的信息存进rule.Transformation.Item2中
            var manager = ThBConvertManager.CreateTCHManager(TCHConvertConfigUrl, Mode);
            if (manager.TCHRules.Count == 0)
            {
                return manager;
            }

            // 获取源块图块名
            manager.TCHRules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
            {
                var tchElement = o.Transformation.Item1;
                tchSrcNames.Add(tchElement.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_TCH_MODEL));
            });

            // 初始化ThBConvertBlockReferenceDataExtension
            var targetConvertRules = new List<ThBlockConvertBlock>();
            manager.TCHRules.Select(r => r.Transformation).ForEach(t =>
            {
                targetConvertRules.Add(t.Item2);
            });
            ThBConvertBlockReferenceDataExtension.TargetBConvertRules.AddRange(targetConvertRules);

            // 获取目标块图块名，以便后续去重
            manager.TCHRules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
            {
                var targetBlock = o.Transformation.Item2;
                if ((Category.Equals(ConvertCategory.WSS)
                    && !targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY).Equals(ConvertCategory.WSS.GetDescription()))
                    || (Category.Equals(ConvertCategory.HVAC)
                    && !targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY).Equals(ConvertCategory.HVAC.GetDescription())))
                {
                    return;
                }
                tchTargetNames.Add(targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));

                // 获取内含图块块名
                var str = targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL);
                if (!str.IsNullOrEmpty())
                {
                    tchTargetNames.AddRange(str.Split(','));
                }
            });
            return manager;
        }

        public List<ThBlockReferenceData> TargetBlockExtract(List<string> targetNames)
        {
            // 从图纸中提取目标块
            var nameFilter = targetNames.Distinct().ToList();
            nameFilter.Add(ThBConvertCommon.BLOCK_PUMP_LABEL);
            nameFilter.Add(ThBConvertCommon.BLOCK_LOAD_DIMENSION + "2");
            nameFilter.Add(ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION + "2");
            var targetEngine = new ThBConvertBlockExtractionEngine()
            {
                NameFilter = nameFilter,
            };

            targetEngine.ExtractFromMS(CurrentDb.Database);
            return targetEngine.Results.Count > 0 ? ThBConvertSpatialIndexService.SelectCrossingPolygon(targetEngine.Results, Frame) : new List<ThBlockReferenceData>();
        }

        public void Convert(ThBConvertManager manager, List<string> tchSrcNames, List<ThBlockReferenceData> targetBlocks, bool setLayer)
        {
            using (var docLock = Active.Document.LockDocument())
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                CurrentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(ThBConvertCommon.LINE_TYPE_HIDDEN, false));
                CurrentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(ThBConvertCommon.LINE_TYPE_CONTINUOUS, false));

                // 从图纸中提取源图块
                var rEngine = new ThTCHBConvertElementExtractionEngine()
                {
                    NameFilter = tchSrcNames.Distinct().ToList(),
                    Category = Category,
                };
                rEngine.Extract(CurrentDb.Database);
                if (rEngine.Results.Count == 0)
                {
                    return;
                }
                var tchElements = ThBConvertSpatialIndexService.TCHSelectCrossingPolygon(rEngine.Results, Frame);
                if (tchElements.Count == 0)
                {
                    return;
                }

                // 记录块转换情况，避免重复转换
                var mapping = new Dictionary<ThTCHElementData, bool>();
                tchElements.ForEach(o => mapping[o] = false);
                var xrg = CurrentDb.Database.GetHostDwgXrefGraph(false);
                // 对所有块，遍历每一条转换规则
                foreach (var rule in manager.TCHRules.Where(o => (o.Mode & Mode) != 0))
                {
                    var mode = Mode & rule.Mode;
                    var block = rule.Transformation.Item1;
                    var tchSrcName = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_TCH_MODEL);
                    tchElements.Where(o => ThMEPXRefService.OriginalFromXref(o.Name) == tchSrcName).ForEach(o =>
                    {
                        // 获取转换后的块信息
                        ThBlockConvertBlock transformedBlock = null;
                        switch (mode)
                        {
                            case ConvertMode.STRONGCURRENT:
                                // 按天正图元型号转换
                                transformedBlock = manager.TCHTransformRule(tchSrcName);
                                break;
                            case ConvertMode.WEAKCURRENT:
                                // 按天正图元型号转换
                                transformedBlock = manager.TCHTransformRule(tchSrcName);
                                break;
                            default:
                                throw new NotSupportedException();
                        }

                        // 开始块转换
                        if (transformedBlock != null)
                        {
                            // 检测块是否已转换
                            if (mapping[o])
                            {
                                return;
                            }

                            // 标记已经转换的块
                            mapping[o] = true;

                            // 获取需要导入的块名、图层，导入目标图块
                            var targetBlockName = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                            var targetBlockLayer = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER);
                            string targetBlockLayerSetting;
                            if (setLayer)
                            {
                                targetBlockLayerSetting = targetBlockLayer;
                            }
                            else
                            {
                                targetBlockLayerSetting = ThBConvertCommon.HIDING_LAYER;
                            }

                            CurrentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(targetBlockName), false);
                            CurrentDb.Layers.Import(blockDb.Layers.ElementOrDefault(targetBlockLayerSetting), false);

                            // 插入新的块引用
                            var scale = new Scale3d(Scale);
                            var engine = new ThTCHBConvertEngine();
                            var objId = engine.Insert(targetBlockName, scale, o);
                            if (objId == ObjectId.Null)
                            {
                                return;
                            }
                            var targetBlockData = new ThBlockReferenceData(objId);

                            // 设置新插入的块引用的镜像变化
                            //engine.Mirror(targetBlockData, o);

                            // 设置新插入的块引用的角度
                            engine.Rotate(targetBlockData, o, rule.Transformation.Item2);

                            // 设置新插入的块引用位置
                            engine.Displacement(targetBlockData, o);

                            if (targetBlocks.Count > 0)
                            {
                                targetBlocks.Where(t => ThBConvertBlockNameService.BlockNameEquals(ThMEPXRefService.OriginalFromXref(t.EffectiveName), targetBlockData.EffectiveName))
                                    .ForEach(t =>
                                    {
                                        if (t.Position.DistanceTo(targetBlockData.Position) < 10.0)
                                        {
                                            if (targetBlockData.EffectiveName.Contains(ThBConvertCommon.BLOCK_LOAD_DIMENSION) &&
                                                !t.EffectiveName.Equals(targetBlockData.EffectiveName))
                                            {
                                                var e = CurrentDb.Element<Entity>(t.ObjId, true);
                                                if (!e.IsErased)
                                                {
                                                    e.Erase();
                                                }
                                            }
                                            else
                                            {
                                                var e = CurrentDb.Element<Entity>(objId, true);
                                                e.Erase();
                                            }
                                        }
                                    });
                            }

                            // 设置动态块可见性
                            if (!objId.IsErased)
                            {
                                // 考虑到目标块可能有多个，在制作模板块时将他们再封装在一个块中
                                // 如果是多个目标块的情况，这里将块炸开，以便获得多个块
                                var refIds = new ObjectIdCollection();
                                if (rule.Explodable())
                                {
                                    ExplodeWithErase(objId, refIds);

                                    // 如果是“单台潜水泵”，继续炸一次
                                    var objIds = new ObjectIdCollection();
                                    foreach (ObjectId item in refIds)
                                    {
                                        var name = item.GetBlockName();
                                        if (!name.IsNull() && name.Contains(ThBConvertCommon.SINGLE_SUBMERSIBLE_PUMP))
                                        {
                                            ExplodeWithErase(item, objIds);
                                        }
                                        else
                                        {
                                            objIds.Add(item);
                                        }
                                    }

                                    // 获取最终结果
                                    refIds = objIds;

                                    foreach (ObjectId id in refIds)
                                    {
                                        var explodeBlockData = new ThBlockReferenceData(id);
                                        targetBlocks.Where(t => ThMEPXRefService.OriginalFromXref(t.EffectiveName) == explodeBlockData.EffectiveName)
                                            .ForEach(t =>
                                            {
                                                if (t.Position.DistanceTo(explodeBlockData.Position) < 10.0)
                                                {
                                                    var e = CurrentDb.Element<Entity>(id, true);
                                                    e.Erase();
                                                }
                                            });
                                    }
                                }
                                else
                                {
                                    refIds.Add(objId);
                                }

                                // 设置块引用的数据库属性
                                refIds.OfType<ObjectId>().ForEach(id =>
                                {
                                    if (id.IsErased)
                                    {
                                        return;
                                    }

                                    engine.SetDatabaseProperties(targetBlockData, id, targetBlockLayerSetting);
                                    if (id.GetBlockName().Equals(ThBConvertCommon.BLOCK_NAME_LEVEL_CONTROLLER))
                                    {
                                        EntityInfos.Add(new ThBConvertEntityInfos(
                                        id,
                                        transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY).Convert(),
                                        ThBConvertCommon.BLOCK_LEVEL_CONTROLLER,
                                        targetBlockLayer));
                                    }
                                    else
                                    {
                                        EntityInfos.Add(new ThBConvertEntityInfos(
                                        id,
                                        transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY).Convert(),
                                        transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EQUIMENT),
                                        targetBlockLayer));
                                    }
                                });
                            }
                        }
                    });
                }

                // 设备避让
                var avoidService = new ThBConvertDamperAvoidService
                {
                    Scale = Scale,
                    EntityInfos = EntityInfos,
                };
                avoidService.Avoid();
            }
        }

        private string BlockDwgPath()
        {
            return ThCADCommon.ElectricalDwgPath();
        }

        private void ExplodeWithErase(ObjectId objId, ObjectIdCollection results)
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var blkref = currentDb.Element<BlockReference>(objId, true);

                // Explode
                var objs = new DBObjectCollection();
                blkref.ExplodeWithVisible(objs);
                foreach (Entity item in objs)
                {
                    results.Add(currentDb.ModelSpace.Add(item));
                }

                // Erase
                blkref.Erase();
            }
        }
    }
}
