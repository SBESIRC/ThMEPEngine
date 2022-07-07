using System;
using System.Collections.Generic;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPElectrical.BlockConvert;

namespace ThMEPElectrical.Command
{
    public class ThBConvertCompareCommand : ThMEPBaseCommand, IDisposable
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
        /// 构造函数
        /// </summary>
        /// <param name="mode"></param>
        public ThBConvertCompareCommand()
        {
            CommandName = "THTZZH";
            ActionName = "提资转换";
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
                var targetBlocks = service.TargetBlockExtract(targetNames);
                service.Convert(manager, srcNames, targetNames, new List<ThBlockReferenceData>(), false);

                var compareService = new ThBConvertCompareService(currentDb.Database, targetBlocks, service.ObjectIds);
                compareService.Compare();
                compareService.Print();
                compareService.Update();

                UpdateLayerSettings(ThBConvertCommon.HIDING_LAYER);
            }
        }

        private void UpdateLayerSettings(string name)
        {
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
            }
        }
    }
}
