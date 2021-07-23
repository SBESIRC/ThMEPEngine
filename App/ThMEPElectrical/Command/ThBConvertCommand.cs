using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical.Command
{
    public class ThBConvertCommand : IAcadCommand, IDisposable
    {
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
        /// 构造函数
        /// </summary>
        /// <param name="mode"></param>
        public ThBConvertCommand()
        {
            //
        }

        public void Dispose()
        {
            //
        }

        private string BlockDwgPath()
        {
            return ThCADCommon.BlockConvertDwgPath();
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

        public void Execute()
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
                    var manager = ThBConvertManager.CreateManager(blockDb.Database, Mode);
                    if (manager.Rules.Count == 0)
                    {
                        return;
                    }

                    // 获取目标图块名
                    var srcNames = new List<String>();
                    manager.Rules.Where(o => (o.Mode & Mode) != 0).ForEach(o =>
                    {
                        var block = o.Transformation.Item1;
                        srcNames.Add(block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME));
                    });

                    // 从图纸中提取目标图块
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

                    var prefix = Category == ConvertCategory.WSS ? "W" : "H";
                    foreach (var rule in manager.Rules.Where(o => (o.Mode & Mode) != 0))
                    {
                        ConvertMode mode = Mode & rule.Mode;
                        var block = rule.Transformation.Item1;
                        var srcName = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                        var visibility = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
                        srcBlocks.Select(o => o.Data as ThBlockReferenceData)
                            .Where(o => ThMEPXRefService.OriginalFromXref(o.EffectiveName) == srcName)
                            .ForEach(o =>
                            {
                                // 获取转换后的块信息
                                ThBlockConvertBlock transformedBlock = null;
                                switch(mode)
                                {
                                    case ConvertMode.STRONGCURRENT:
                                        {
                                            if (string.IsNullOrEmpty(visibility) && string.IsNullOrEmpty(ThStringTools.ToChinesePunctuation(visibility)))
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
                                            if (ThStringTools.CompareWithChinesePunctuation(o.CurrentVisibilityStateValue(), visibility))
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
                                    // 导入目标图块
                                    var targetBlockName = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                                    var targetBlockLayer = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER);
                                    currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(targetBlockName), false);
                                    currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(targetBlockLayer), false);

                                    // 动态块的Bug：导入含有Wipeout的动态块，DrawOrder丢失
                                    // 修正插入动态块的图层顺序
                                    if (targetBlockName == "电动机及负载标注")
                                    {
                                        var wipeOut = new ThBConvertWipeOut();
                                        wipeOut.FixWipeOutDrawOrder(currentDb.Database, targetBlockName);
                                    }  

                                    // 插入新的块引用
                                    var scale = new Scale3d(Scale);
                                    var engine = CreateConvertEngine(mode);
                                    var objId = engine.Insert(targetBlockName, scale, o);

                                    // 设置新插入的块引用的镜像变化
                                    engine.Mirror(objId, o);

                                    // 设置新插入的块引用的角度
                                    engine.Rotate(objId, o);

                                    // 设置新插入的块引用位置
                                    engine.Displacement(objId, o);

                                    // 设置动态块可见性
                                    engine.SetVisibilityState(objId, o);

                                    // 将源块引用的属性“刷”到新的块引用
                                    engine.MatchProperties(objId, o);

                                    // 考虑到目标块可能有多个，在制作模板块时将他们再封装在一个块中
                                    // 如果是多个目标块的情况，这里将块炸开，以便获得多个块
                                    var refIds = new ObjectIdCollection();
                                    if (rule.Explodable())
                                    {
                                        var blkref = currentDb.Element<BlockReference>(objId, true);

                                        // 
                                        void handler(object s, ObjectEventArgs e)
                                        {
                                            if (e.DBObject is BlockReference reference)
                                            {
                                                refIds.Add(e.DBObject.ObjectId);
                                            }
                                        }
                                        currentDb.Database.ObjectAppended += handler;
                                        blkref.ExplodeToOwnerSpace();
                                        currentDb.Database.ObjectAppended -= handler;

                                        blkref.Erase();
                                    }
                                    else
                                    {
                                        refIds.Add(objId);
                                    }

                                    // 设置块引用的数据库属性
                                    refIds.Cast<ObjectId>().ForEach(b =>
                                    {
                                        engine.SetDatbaseProperties(b, o, targetBlockLayer);
                                    });
                                }
                            });
                    }
                }
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
            filters.ForEach(o => transformer.Reset(o.Geometry));
            return filters;
        }
    }
}
