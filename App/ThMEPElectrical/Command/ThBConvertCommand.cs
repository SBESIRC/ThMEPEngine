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
                service.Convert(manager, srcNames, targetBlocks, true);
            }
        }
    }
}