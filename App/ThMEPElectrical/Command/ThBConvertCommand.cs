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
        public ConvertMode Mode { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mode"></param>
        public ThBConvertCommand(ConvertMode mode)
        {
            Mode = mode;
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
            switch (mode)
            {
                case ConvertMode.STRONGCURRENT:
                    return new ThBConvertEngineStrongCurrent();
                case ConvertMode.WEAKCURRENT:
                    return new ThBConvertEngineWeakCurrent();
                default:
                    throw new NotSupportedException();
            }
        }

        public void Execute()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (ThBConvertEngine engine = CreateConvertEngine(Mode))
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
                    manager.Rules.Where(o => o.Mode == Mode).ForEach(o =>
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

                    foreach (var rule in manager.Rules.Where(o => o.Mode == Mode))
                    {
                        var block = rule.Transformation.Item1;
                        var srcName = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                        var visibility = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
                        srcBlocks.Select(o => o.Data as ThBlockReferenceData)
                            .Where(o => ThMEPXRefService.OriginalFromXref(o.EffectiveName) == srcName)
                            .ForEach(o =>
                            {
                                try
                                {
                                    // 获取转换后的块信息
                                    ThBlockConvertBlock transformedBlock = null;
                                    if (Mode == ConvertMode.STRONGCURRENT)
                                    {
                                        if (string.IsNullOrEmpty(visibility))
                                        {
                                            // 当配置表中可见性为空时，则按图块名转换
                                            transformedBlock = manager.TransformRule(srcName);
                                        }
                                        else if (o.CurrentVisibilityStateValue() == visibility)
                                        {
                                            // 当配置表中可见性有字符时，则按块名和可见性的组合一对一转换
                                            transformedBlock = manager.TransformRule(
                                                srcName,
                                                o.CurrentVisibilityStateValue());
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                    else if (Mode == ConvertMode.WEAKCURRENT)
                                    {
                                        if (o.CurrentVisibilityStateValue() == visibility)
                                        {
                                            transformedBlock = manager.TransformRule(
                                                srcName,
                                                o.CurrentVisibilityStateValue());
                                        }
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                    if (transformedBlock == null)
                                    {
                                        return;
                                    }

                                    // 转换后块的名字
                                    // 在当前图纸中查找是否存在新的块定义
                                    // 若不存在，则插入新的块定义；
                                    // 若存在，则保持现有的块定义
                                    string targetBlockName = null;
                                    if (Mode == ConvertMode.STRONGCURRENT)
                                    {
                                        targetBlockName = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                                    }
                                    else if (Mode == ConvertMode.WEAKCURRENT)
                                    {
                                        targetBlockName = transformedBlock.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME);
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }

                                    // 导入目标图块
                                    var result = currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(targetBlockName), false);

                                    // 插入新的块引用
                                    var scale = new Scale3d(100.0);
                                    var objId = engine.Insert(targetBlockName, scale, o);

                                    // 将新插入的块引用调整到源块引用所在的位置
                                    engine.TransformBy(objId, o);

                                    // 微调
                                    engine.Adjust(objId, o);

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
                                        engine.SetDatbaseProperties(b, o);
                                    });
                                }
                                catch
                                {
                                    return;
                                }
                            });
                    }
                }
            }
        }

        private List<ThRawIfcDistributionElementData> SelectCrossingPolygon(List<ThRawIfcDistributionElementData> blocks, Polyline frame)
        {
            var objs = blocks.Select(o => o.Geometry).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var result = spatialIndex.SelectCrossingPolygon(frame.Vertices());
            return blocks.Where(o => result.Contains(o.Geometry)).ToList();
        }
    }
}
