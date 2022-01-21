using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.BlockConvert;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.Command
{
    public class ThBConvertCommand : ThMEPBaseCommand, IDisposable
    {
        readonly static string BConvertConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "提资转换配置表.xlsx");

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
        /// 构造函数
        /// </summary>
        /// <param name="mode"></param>
        public ThBConvertCommand()
        {
            CommandName = "THTZZH";
            ActionName = "提资转换";
        }

        public void Dispose()
        {
            //
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

        public override void SubExecute()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    var manager = ThBConvertManager.CreateManager(BConvertConfigUrl, Mode);
                    if (manager.Rules.Count == 0)
                    {
                        return;
                    }

                    // 获取源块图块名
                    var srcNames = new List<String>();
                    manager.Rules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
                    {
                        var block = o.Transformation.Item1;
                        srcNames.Add(block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));
                    });

                    // 从图纸中提取源图块
                    var rEngine = new ThBConvertElementExtractionEngine()
                    {
                        NameFilter = srcNames,
                    };
                    rEngine.Extract(currentDb.Database);
                    if (rEngine.Results.Count == 0)
                    {
                        return;
                    }
                    var srcBlocks = SelectCrossingPolygon(rEngine.Results, frame);
                    if (srcBlocks.Count == 0)
                    {
                        return;
                    }

                    // 从图纸中提取集水井提资表表身
                    var collectingWellEngine = new ThBConvertElementExtractionEngine()
                    {
                        NameFilter = new List<string> { ThBConvertCommon.COLLECTING_WELL }
                    };
                    collectingWellEngine.Extract(currentDb.Database);

                    // 获取目标块图块名
                    var targetNames = new List<String>();
                    manager.Rules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
                    {
                        var targetBlock = o.Transformation.Item2;
                        targetNames.Add(targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));

                        var str = targetBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL);
                        if (!str.IsNullOrEmpty())
                        {
                            targetNames.AddRange(str.Split(','));
                        }
                    });
                    targetNames = targetNames.Distinct().ToList();

                    // 从图纸中提取目标块
                    var targetEngine = new ThBConvertBlockExtractionEngine()
                    {
                        NameFilter = targetNames,
                    };
                    targetEngine.ExtractFromMS(currentDb.Database);
                    var targetBlocks = SelectCrossingPolygon(targetEngine.Results, frame);

                    var mapping = new Dictionary<ThBlockReferenceData, bool>();
                    srcBlocks.Select(o => o.Data as ThBlockReferenceData).ForEach(o => mapping[o] = false);
                    XrefGraph xrg = currentDb.Database.GetHostDwgXrefGraph(false);
                    foreach (var rule in manager.Rules.Where(o => (o.Mode & Mode) != 0))
                    {
                        ConvertMode mode = Mode & rule.Mode;
                        var block = rule.Transformation.Item1;
                        var srcName = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                        var visibility = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
                        srcBlocks.Select(o => o.Data as ThBlockReferenceData)
                            .Where(o => ThMEPXRefService.OriginalFromXref(o.EffectiveName) == srcName)
                            .Where(o =>
                            {
                                string name = "";
                                ThXrefDbExtension.XRefNodeName(xrg.RootNode, o.Database, ref name);
                                Regex r = new Regex(@"([a-zA-Z])");
                                Match m = r.Match(name);
                                if (!m.Success)
                                {
                                    return false;
                                }
                                switch (Category)
                                {
                                    case ConvertCategory.WSS:
                                        return m.Groups[1].Value.ToUpper() == "W";
                                    case ConvertCategory.HVAC:
                                        return m.Groups[1].Value.ToUpper() == "H";
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
                                                transformedBlock = manager.TransformRule(
                                                    srcName,
                                                    o.CurrentVisibilityStateValue());
                                            }
                                        }
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }

                                // 转换
                                if (transformedBlock != null)
                                {
                                    // 避免重复转换
                                    if (mapping[o] == true)
                                    {
                                        return;
                                    }

                                    // 标记已经转换的块
                                    mapping[o] = true;

                                    // 导入目标图块
                                    var targetBlockName = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                                    var targetBlockLayer = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER);
                                    if (FrameStyle == "标注无边框" && (targetBlockName == "电动机及负载标注" || targetBlockName == "负载标注"))
                                    {
                                        targetBlockName += "2";
                                    }
                                    currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(targetBlockName), false);
                                    currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(targetBlockLayer), false);

                                    // 动态块的Bug：导入含有Wipeout的动态块，DrawOrder丢失
                                    // 修正插入动态块的图层顺序
                                    if (targetBlockName.Contains("电动机及负载标注"))
                                    {
                                        var wipeOut = new ThBConvertWipeOut();
                                        wipeOut.FixWipeOutDrawOrder(currentDb.Database, targetBlockName);
                                    }

                                    // 插入新的块引用
                                    var scale = new Scale3d(Scale);
                                    var engine = CreateConvertEngine(mode);
                                    var objId = engine.Insert(targetBlockName, scale, o);
                                    if (objId == ObjectId.Null)
                                    {
                                        return;
                                    }

                                    // 设置新插入的块引用的镜像变化
                                    engine.Mirror(objId, o);

                                    // 设置新插入的块引用的角度
                                    engine.Rotate(objId, o);

                                    // 设置新插入的块引用位置
                                    if (o.EffectiveName.Contains("潜水泵-AI"))
                                    {
                                        currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("水泵标注"), false);
                                        currentDb.Layers.Import(blockDb.Layers.ElementOrDefault("E-UNIV-NOTE"), false);
                                        engine.Displacement(objId, o, collectingWellEngine.Results, scale);
                                    }
                                    else
                                    {
                                        engine.Displacement(objId, o, rule.InsertMode());
                                    }

                                    var targetBlockData = new ThBlockReferenceData(objId);
                                    targetBlocks.Select(t => t.Data as ThBlockReferenceData)
                                                .Where(t => ThMEPXRefService.OriginalFromXref(t.EffectiveName) == targetBlockData.EffectiveName)
                                                .ForEach(t =>
                                                {
                                                    if (t.Position.DistanceTo(targetBlockData.Position) < 10.0)
                                                    {
                                                        var e = currentDb.Element<Entity>(objId, true);
                                                        e.Erase();
                                                    }
                                                });
                                    // 对电动机及负载标注、负载标注单独处理
                                    if (!objId.IsErased && targetBlockName.Contains("负载标注"))
                                    {
                                        var name = KeepChinese(targetBlockName);
                                        targetBlocks.Select(t => t.Data as ThBlockReferenceData)
                                                .Where(t => ThMEPXRefService.OriginalFromXref(t.EffectiveName).Contains(name))
                                                .ForEach(t =>
                                                {
                                                    if (t.Position.DistanceTo(targetBlockData.Position) < 10.0)
                                                    {
                                                        var e = currentDb.Element<Entity>(t.ObjId, true);
                                                        e.Erase();
                                                    }
                                                });
                                    }

                                    // 设置动态块可见性
                                    if (!objId.IsErased)
                                    {
                                        engine.SetVisibilityState(objId, o);

                                        // 将源块引用的属性“刷”到新的块引用
                                        engine.MatchProperties(objId, o);

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
                                                if (item.GetBlockName().Contains("单台潜水泵"))
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
                                                targetBlocks.Select(t => t.Data as ThBlockReferenceData)
                                                            .Where(t => ThMEPXRefService.OriginalFromXref(t.EffectiveName) == explodeBlockData.EffectiveName)
                                                            .ForEach(t =>
                                                            {
                                                                if (t.Position.DistanceTo(explodeBlockData.Position) < 10.0)
                                                                {
                                                                    var e = currentDb.Element<Entity>(id, true);
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
                                        refIds.Cast<ObjectId>().ForEach(b =>
                                        {
                                            if (!b.IsErased)
                                            {
                                                engine.SetDatabaseProperties(b, o, targetBlockLayer);
                                            }
                                        });
                                    }
                                }
                            });
                    }
                }
            }
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

        private List<ThRawIfcDistributionElementData> SelectCrossingPolygon(List<ThRawIfcDistributionElementData> blocks, Polyline frame)
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
            return filters;
        }

        private string KeepChinese(string str)
        {
            //声明存储结果的字符串
            var chineseString = "";

            //将传入参数中的中文字符添加到结果字符串中
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] >= 0x4E00 && str[i] <= 0x9FA5) //汉字
                {
                    chineseString += str[i];
                }
            }

            //返回保留中文的处理结果
            return chineseString;
        }
    }
}
