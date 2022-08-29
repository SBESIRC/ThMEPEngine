using System;
using System.Collections.Generic;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical.Command
{
    public class ThBConvertCommand : ThMEPBaseCommand, IDisposable
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
        /// 标注样式
        /// </summary>
        public string FrameStyle { get; set; }

        /// <summary>
        /// 标注样式
        /// </summary>
        public bool ConvertManualActuator { get; set; }

        /// <summary>
        /// 操作命令
        /// </summary>
        public BConvertCommand Command { get; set; }

        /// <summary>
        /// 比对结果
        /// </summary>
        public List<ThBConvertCompareModel> CompareModels { get; set; }

        public List<ThBConvertEntityInfos> TarEntityInfos { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mode"></param>
        public ThBConvertCommand()
        {
            CommandName = "THTZZH";
            ActionName = "提资转换";
            CompareModels = new List<ThBConvertCompareModel>();
            TarEntityInfos = new List<ThBConvertEntityInfos>();
        }

        public void Dispose()
        {
            //
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

                var service = new ThBConvertService(currentDb, frame, Mode, Category, Scale, FrameStyle, ConvertManualActuator);
                var srcNames = new List<string>();
                var targetNames = new List<string>();
                var manager = service.ReadFile(srcNames, targetNames);

                // 清空"AI-提资比对"图层
                ClearAll();
                var targetBlocks = service.TargetBlockExtract(targetNames);
                if (Command == BConvertCommand.BlockConvert)
                {
                    service.Convert(manager, srcNames, targetBlocks, true);
                }
                else
                {
                    service.Convert(manager, srcNames, new List<ThBlockReferenceData>(), false);
                    TarEntityInfos = service.EntityInfos;
                    var compareService = new ThBConvertCompareService(currentDb.Database, GetEntityInfos(targetBlocks, manager), TarEntityInfos);
                    compareService.Compare();
                    CompareModels = compareService.CompareModels;

                    HiddenLayer(ThBConvertCommon.HIDING_LAYER);
                }
            }
        }

        private void ClearAll()
        {
            using (var docLock = Active.Document.LockDocument())
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThBConvertCommon.HIDING_LAYER,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var selectionResult = Active.Editor.SelectAll(filter);
                if (selectionResult.Status != PromptStatus.OK)
                {
                    return;
                }
                foreach (var objId in selectionResult.Value.GetObjectIds())
                {
                    currentDb.Element<BlockReference>(objId, true).Erase();
                }
            }
        }

        private List<ThBConvertEntityInfos> GetEntityInfos(List<ThBlockReferenceData> targetBlocks, ThBConvertManager manager)
        {
            var results = new List<ThBConvertEntityInfos>();
            targetBlocks.ForEach(t =>
            {
                var result = new ThBConvertEntityInfos();
                result.ObjectId = t.ObjId;
                if (t.EffectiveName.Equals("E-BDB054"))
                {
                    result.Category = EquimentCategory.给排水;
                    result.EquimentType = ThBConvertCommon.BLOCK_SUBMERSIBLE_PUMP;
                }
                else if(t.EffectiveName.Equals("E-BFAS23-3"))
                {
                    result.Category = EquimentCategory.给排水;
                    result.EquimentType = ThBConvertCommon.BLOCK_LEVEL_CONTROLLER;
                }
                else
                {
                    foreach (var rule in manager.Rules)
                    {
                        if (rule.Transformation.Item2.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME).Equals(t.EffectiveName))
                        {
                            result.Category = rule.Transformation.Item2.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY).Convert();
                            result.EquimentType = rule.Transformation.Item2.StringValue(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EQUIMENT);
                            break;
                        }
                    }
                }
                results.Add(result);
            });

            return results;
        }

        private void HiddenLayer(string name)
        {
            using (var docLock = Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ltr = acadDatabase.Layers.ElementOrDefault(name, true);
                if (ltr == null)
                {
                    return;
                }

                // 如果当前图层等于插入图层，暂不处理
                if (acadDatabase.Database.Clayer.Equals(ltr.ObjectId))
                {
                    return;
                }

                // 设置图层状态
                ltr.IsFrozen = true;

                // 设置非打印图层
                ltr.IsPlottable = false;
            }
        }
    }
}