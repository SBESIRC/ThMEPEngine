using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADExtension;
using AcHelper.Commands;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
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
            return Path.Combine(ThCADCommon.SupportPath(), ThBConvertCommon.BLOCK_MAP_RULES_FILE);
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
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                ThBConvertManager manager = new ThBConvertManager()
                {
                    Rules = ThMEPElectricalService.Instance.ConvertParameter.Rules,
                };
                if (manager.Rules.Count == 0)
                {
                    return;
                }

                // 在当前图纸中框选一个区域，获取块引用
                var extents = new Extents3d();
                var prompts = new List<string>();
                var objs = new ObjectIdCollection();
                using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, prompts))
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
                    var filterlist = OpFilter.Bulid(o =>
                        o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(BlockReference)).DxfName);
                    var entSelected = Active.Editor.SelectCrossingWindow(winCorners[0], winCorners[1], filterlist);
                    if (entSelected.Status == PromptStatus.OK)
                    {
                        // 我们需要用这个选择框在外参中的相同范围内寻找块引用，
                        // 考虑到外参图纸的WCS和当前图纸的WCS是完全一致的（暂时不考虑图纸WCS不一致的情况）
                        // 同时考虑到PointCollector收集的点是UCS下的点，所以需要将这些点从UCS转换到WCS下
                        extents.AddPoint(winCorners[0].TransformBy(Active.Editor.UCS2WCS()));
                        extents.AddPoint(winCorners[1].TransformBy(Active.Editor.UCS2WCS()));
                        entSelected.Value.GetObjectIds().ForEach(o => objs.Add(o));
                    }
                }
                if (objs.Count == 0)
                {
                    return;
                }

                // 过滤所选的对象
                //  1. 对象类型是块引用
                //  2. 块引用是外部引用(Xref)
                //  3. 块引用是“Overlay”
                // 对于每一个Xref块引用，获取其Xref数据库
                // 一个Xref块可以有多个块引用
                var xrefs = new List<string>();
                foreach (ObjectId obj in objs)
                {
                    var blkRef = currentDb.Element<BlockReference>(obj);
                    var blkDef = blkRef.GetEffectiveBlockTableRecord();
                    if (blkDef.IsFromExternalReference /* && blkDef.IsFromOverlayReference */)
                    {
                        // 暂时不考虑unresolved的情况
                        using (var xrefDatatbase = blkDef.GetXrefDatabase(false))
                        {
                            // 暂时不考虑各专业的外参图纸
                            xrefs.Add(xrefDatatbase.Filename);
                            //// 暂时只关心特定的外参图纸（暖通提资）
                            //if (Path.GetFileName(xrefDatatbase.Filename).StartsWith("H"))
                            //{
                            //    xrefs.Add(xrefDatatbase.Filename);
                            //}
                        }
                    }
                }

                // 遍历每一个XRef的Database，在其中寻找在选择框线内特定的块引用
                // 通过查找映射表，获取映射后的块信息
                // 根据获取后的块信息，在当前图纸中创建新的块引用
                // 并将源块引用中的属性”刷“到新的块引用
                foreach (var xref in xrefs)
                {
                    // 在协同环境下，外参可能是不可写的
                    // 在外参不可读的情况下，XrefFileLock.LockFile()会抛出异常
                    // 具体的情况可以参阅这篇文章：
                    //  https://www.keanw.com/2015/01/modifying-the-contents-of-an-autocad-xref-using-net.html
                    // 考虑到我们这里的情况，并不需要对外参进行写操作，我们可以直接以“只读”的方式打开外参
                    // 从而绕过XrefFileLock.LockFile()的异常
                    using (AcadDatabase xrefDb = AcadDatabase.Open(xref, DwgOpenMode.ReadOnly, false))
                    {
                        foreach (var rule in manager.Rules.Where(o => o.Mode == Mode))
                        {
                            var block = rule.Transformation.Item1;
                            var visibility = block.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY);
                            foreach (ObjectId blkRef in xrefDb.Database.GetBlockReferences(block, extents))
                            {
                                try
                                {
                                    // 根据块引用的“块名”，匹配转换后的块定义的信息
                                    var blockReference = blkRef.Database.GetBlockReference(blkRef);

                                    // 获取转换后的块信息
                                    ThBlockConvertBlock transformedBlock = null;
                                    if (Mode == ConvertMode.STRONGCURRENT)
                                    {
                                        if (string.IsNullOrEmpty(visibility))
                                        {
                                            // 当配置表中可见性为空时，则按图块名转换
                                            transformedBlock = manager.TransformRule(blockReference.EffectiveName);
                                        }
                                        else if (blockReference.CurrentVisibilityStateValue() == visibility)
                                        {
                                            // 当配置表中可见性有字符时，则按块名和可见性的组合一对一转换
                                            transformedBlock = manager.TransformRule(
                                                blockReference.EffectiveName,
                                                blockReference.CurrentVisibilityStateValue());
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                    else if (Mode == ConvertMode.WEAKCURRENT)
                                    {
                                        if (blockReference.CurrentVisibilityStateValue() == visibility)
                                        {
                                            transformedBlock = manager.TransformRule(
                                                blockReference.EffectiveName,
                                                blockReference.CurrentVisibilityStateValue());
                                        }
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                    if (transformedBlock == null)
                                    {
                                        continue;
                                    }

                                    // 转换后块的名字
                                    // 在当前图纸中查找是否存在新的块定义
                                    // 若不存在，则插入新的块定义；
                                    // 若存在，则保持现有的块定义
                                    string name = null;
                                    if (Mode == ConvertMode.STRONGCURRENT)
                                    {
                                        name = (string)transformedBlock.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME];
                                    }
                                    else if (Mode == ConvertMode.WEAKCURRENT)
                                    {
                                        name = (string)transformedBlock.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME];
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                    var result = currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);

                                    // 插入新的块引用
                                    var scale = ThMEPElectricalService.Instance.ConvertParameter.Scale;
                                    var objId = engine.Insert(name, scale, blockReference);

                                    // 将新插入的块引用调整到源块引用所在的位置
                                    engine.TransformBy(objId, blockReference);

                                    // 将源块引用的属性“刷”到新的块引用
                                    engine.MatchProperties(objId, blockReference);

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
                                    refIds.Cast<ObjectId>().ForEach(o =>
                                    {
                                        engine.SetDatbaseProperties(objId, blockReference);
                                    });
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
