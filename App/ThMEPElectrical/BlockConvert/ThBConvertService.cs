using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NFox.Cad;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertService
    {
        readonly static string BConvertConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "提资转换配置表.xlsx");

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
        /// 标注样式
        /// </summary>
        public string FrameStyle { get; set; }

        /// <summary>
        /// 标注样式
        /// </summary>
        public bool ConvertManualActuator { get; set; }

        public List<ThBConvertEntityInfos> EntityInfos { get; set; }

        public ThBConvertService(AcadDatabase currentDb, Polyline frame, ConvertMode mode, ConvertCategory category, double scale,
            string frameStyle, bool convertManualActuator)
        {
            CurrentDb = currentDb;
            Frame = frame;
            Mode = mode;
            Category = category;
            Scale = scale;
            FrameStyle = frameStyle;
            ConvertManualActuator = convertManualActuator;
            EntityInfos = new List<ThBConvertEntityInfos>();
        }

        public ThBConvertManager ReadFile(List<string> srcNames, List<string> targetNames)
        {
            // 获取配置表信息
            // 将源块-转换前的块的信息存进rule.Transformation.Item1中，目标块-转换后的块的信息存进rule.Transformation.Item2中
            var manager = ThBConvertManager.CreateManager(BConvertConfigUrl, Mode);
            if (manager.Rules.Count == 0)
            {
                return manager;
            }

            // 获取源块图块名
            manager.Rules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
            {
                var block = o.Transformation.Item1;
                srcNames.Add(block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));
            });

            // 初始化ThBConvertBlockReferenceDataExtension
            var sourceConvertRules = new List<ThBlockConvertBlock>();
            var targetConvertRules = new List<ThBlockConvertBlock>();
            manager.Rules.Select(r => r.Transformation).ForEach(t =>
            {
                sourceConvertRules.Add(t.Item1);
                targetConvertRules.Add(t.Item2);
            });
            ThBConvertBlockReferenceDataExtension.SourceBConvertRules = sourceConvertRules;
            ThBConvertBlockReferenceDataExtension.TargetBConvertRules = targetConvertRules;

            // 获取目标块图块名，以便后续去重
            manager.Rules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
            {
                var targetBlock = o.Transformation.Item2;
                targetNames.Add(targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));

                // 获取内含图块块名
                var str = targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL);
                if (!str.IsNullOrEmpty())
                {
                    targetNames.AddRange(str.Split(','));
                }
            });
            return manager;
        }

        public List<ThBlockReferenceData> TargetBlockExtract(List<string> targetNames)
        {
            // 从图纸中提取目标块
            var targetEngine = new ThBConvertBlockExtractionEngine()
            {
                NameFilter = targetNames.Distinct().ToList(),
            };
            targetEngine.ExtractFromMS(CurrentDb.Database);
            return targetEngine.Results.Count > 0
                ? SelectCrossingPolygon(targetEngine.Results, Frame) : new List<ThBlockReferenceData>();
        }

        public void Convert(ThBConvertManager manager, List<string> srcNames, List<ThBlockReferenceData> targetBlocks, bool setLayer)
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                CurrentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(ThBConvertCommon.LINE_TYPE_HIDDEN, false));
                CurrentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(ThBConvertCommon.LINE_TYPE_CONTINUOUS, false));

                // 从图纸中提取源图块
                var rEngine = new ThBConvertElementExtractionEngine()
                {
                    NameFilter = srcNames.Distinct().ToList(),
                };
                rEngine.Extract(CurrentDb.Database);
                if (rEngine.Results.Count == 0)
                {
                    return;
                }
                var srcBlocks = SelectCrossingPolygon(rEngine.Results, Frame);
                if (srcBlocks.Count == 0)
                {
                    return;
                }

                // 从图纸中提取集水井提资表表身
                var collectingWellEngine = new ThBConvertElementExtractionEngine()
                {
                    NameFilter = new List<string> { ThBConvertCommon.COLLECTING_WELL }
                };
                collectingWellEngine.Extract(CurrentDb.Database);

                // 记录块转换情况，避免重复转换
                var mapping = new Dictionary<ThBlockReferenceData, bool>();
                srcBlocks.ForEach(o => mapping[o] = false);
                var xrg = CurrentDb.Database.GetHostDwgXrefGraph(false);
                // 对所有块，遍历每一条转换规则
                foreach (var rule in manager.Rules.Where(o => (o.Mode & Mode) != 0))
                {
                    var mode = Mode & rule.Mode;
                    var block = rule.Transformation.Item1;
                    var srcName = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                    var visibility = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
                    srcBlocks.Where(o => ThMEPXRefService.OriginalFromXref(o.EffectiveName) == srcName).Where(o =>
                    {
                        // 仅转换指定外参上的图块
                        var name = "";
                        ThXrefDbExtension.XRefNodeName(xrg.RootNode, o.Database, ref name);
                        var r = new Regex(@"([a-zA-Z])");
                        var m = r.Match(name);
                        if (!m.Success)
                        {
                            return false;
                        }
                        switch (Category)
                        {
                            case ConvertCategory.WSS:
                                return m.Value.ToUpper() == "W";
                            case ConvertCategory.HVAC:
                                return m.Value.ToUpper() == "H";
                            default:
                                return true;
                        }
                    }).ForEach(o =>
                    {
                        // 获取转换后的块信息
                        ThBlockConvertBlock transformedBlock = null;
                        switch (mode)
                        {
                            case ConvertMode.STRONGCURRENT:
                                {
                                    if (string.IsNullOrEmpty(ThStringTools.ToChinesePunctuation(visibility)))
                                    {
                                        // 当配置表中可见性为空时，则按图块名转换
                                        transformedBlock = manager.TransformRule(srcName);
                                    }
                                    else if (ThStringTools.CompareWithChinesePunctuation(o.CurrentVisibilityStateValue(), visibility))
                                    {
                                        // 当配置表中可见性有字符时，则按块名和可见性的组合一对一转换
                                        transformedBlock = manager.TransformRule(
                                        srcName,
                                        o.CurrentVisibilityStateValue());
                                    }
                                }
                                break;
                            case ConvertMode.WEAKCURRENT:
                                {
                                    if (string.IsNullOrEmpty(ThStringTools.ToChinesePunctuation(visibility)))
                                    {
                                        transformedBlock = manager.TransformRule(srcName);
                                    }
                                    else if (ThStringTools.CompareWithChinesePunctuation(o.CurrentVisibilityStateValue(), visibility))
                                    {
                                        transformedBlock = manager.TransformRule(srcName, o.CurrentVisibilityStateValue());
                                    }
                                }
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
                            if (FrameStyle == ThBConvertCommon.LABEL_STYLE_BORDERLESS
                                && (targetBlockName == ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION || targetBlockName == ThBConvertCommon.BLOCK_LOAD_DIMENSION))
                            {
                                targetBlockName += "2";
                            }
                            if (targetBlockName.Equals(ThBConvertCommon.MANUAL_ACTUATOR_OF_SMOKE_EXHAUST_VALVE) && !ConvertManualActuator)
                            {
                                return;
                            }

                            CurrentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(targetBlockName), false);
                            CurrentDb.Layers.Import(blockDb.Layers.ElementOrDefault(targetBlockLayerSetting), false);

                            // 动态块的Bug：导入含有Wipeout的动态块，DrawOrder丢失
                            // 修正插入动态块的图层顺序
                            if (targetBlockName.Contains(ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION))
                            {
                                var wipeOut = new ThBConvertWipeOut();
                                wipeOut.FixWipeOutDrawOrder(CurrentDb.Database, targetBlockName);
                            }

                            // 插入新的块引用
                            var scale = new Scale3d(Scale);
                            var engine = CreateConvertEngine(mode);
                            var objId = engine.Insert(targetBlockName, scale, o);
                            if (objId == ObjectId.Null)
                            {
                                return;
                            }
                            var targetBlockData = new ThBlockReferenceData(objId);

                            // 设置新插入的块引用的镜像变化
                            engine.Mirror(targetBlockData, o);

                            // 设置新插入的块引用的角度
                            engine.Rotate(targetBlockData, o, rule.Transformation.Item2);

                            // 设置新插入的块引用位置
                            if (o.EffectiveName.Contains(ThBConvertCommon.BLOCK_AI_SUBMERSIBLE_PUMP))
                            {
                                CurrentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThBConvertCommon.BLOCK_PUMP_LABEL), false);
                                CurrentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThBConvertCommon.BLOCK_PUMP_LABEL_LAYER), false);
                                engine.Displacement(targetBlockData, o, collectingWellEngine.Results, scale);
                                engine.SpecialTreatment(targetBlockData, o);
                            }
                            else
                            {
                                engine.Displacement(targetBlockData, o);
                                engine.SpecialTreatment(targetBlockData, o);
                            }

                            if (targetBlocks.Count > 0)
                            {
                                targetBlocks.Where(t => ThBConvertBlockNameService.BlockNameEquals(ThMEPXRefService.OriginalFromXref(t.EffectiveName), targetBlockData.EffectiveName))
                                    .ForEach(t =>
                                    {
                                        if (t.Position.DistanceTo(targetBlockData.Position) < 10.0)
                                        {
                                            var e = CurrentDb.Element<Entity>(objId, true);
                                            e.Erase();
                                        }
                                    });
                            }

                            // 设置动态块可见性
                            if (!objId.IsErased)
                            {
                                engine.SetVisibilityState(targetBlockData, o);

                                // 将源块引用的属性“刷”到新的块引用
                                engine.MatchProperties(targetBlockData, o);

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
                                    if (!id.IsErased)
                                    {
                                        engine.SetDatabaseProperties(targetBlockData, id, targetBlockLayerSetting);
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
            }
        }


        private string BlockDwgPath()
        {
            return ThCADCommon.ElectricalDwgPath();
        }

        private ThBConvertEngine CreateConvertEngine(ConvertMode mode)
        {
            if ((mode & ConvertMode.STRONGCURRENT) != 0)
            {
                return new ThBConvertEngineStrongCurrent();
            }
            if ((mode & ConvertMode.WEAKCURRENT) != 0)
            {
                return new ThBConvertEngineWeakCurrent();
            }
            throw new NotSupportedException();
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

        private List<ThBlockReferenceData> SelectCrossingPolygon(List<ThRawIfcDistributionElementData> blocks, Polyline frame)
        {
            var objs = blocks.Select(o => o.Geometry).ToCollection();
            var transformer = new ThMEPOriginTransformer(objs);
            transformer.Transform(objs);
            transformer.Transform(frame);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = spatialIndex.SelectCrossingPolygon(frame.Vertices());
            var filters = blocks.Where(o => result.Contains(o.Geometry)).ToList();
            transformer.Reset(objs);
            transformer.Reset(frame);
            return filters.Select(o => o.Data as ThBlockReferenceData).ToList();
        }
    }
}
