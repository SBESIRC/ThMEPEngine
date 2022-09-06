using System;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.Common;
using ThPlatform3D.ArchitecturePlane;

namespace Tianhua.Platform3D.UI.Command
{
    public class ThArchitecturePlaneCmd : IAcadCommand, IDisposable
    {
        public ThArchitecturePlaneCmd()
        {
            //
        }
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            var pofo = new PromptOpenFileOptions("\n选择要成图的Ifc文件");
            pofo.Filter = "Ifc files (*.ifc)|*.ifc|Ifc files (*.get)|*.get";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                var options = new PromptKeywordOptions("\n选择出图方式");
                options.Keywords.Add("平面图", "P", "平面图(P)");
                options.Keywords.Add("立面图", "E", "立面图(E)");
                options.Keywords.Add("剖面图", "S", "剖面图(S)");
                options.Keywords.Default = "平面图";
                options.AllowArbitraryInput = true;
                var result1 = Active.Editor.GetKeywords(options);
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }

                var printParameter = new ThPlanePrintParameter()
                {
                    DrawingScale = "1:100",
                };
                var result2 = Active.Editor.GetPoint("\n输入插入的基点");
                if (result2.Status == PromptStatus.OK)
                {
                    printParameter.BasePoint = result2.Value;
                }

                var drawingType = DrawingType.Unknown;
                var eye_dir = new Direction();
                var up = new Direction();
                int? cut_position = null;
                int? ralative_cut_position = null;
                switch (result1.StringResult)
                {
                    case "平面图":
                        drawingType = DrawingType.Plan;
                        eye_dir = new Direction(0, 0, -1);
                        up = new Direction(0, 1, 0);
                        ralative_cut_position = 1200;
                        break;
                    case "立面图":
                        drawingType = DrawingType.Elevation;
                        eye_dir = new Direction(0, -1, 0);
                        up = new Direction(0, 0, 1);
                        break;
                    case "剖面图":
                        drawingType = DrawingType.Section;
                        eye_dir = new Direction(-1, 0, 0);
                        up = new Direction(0, 0, 1);
                        ralative_cut_position = 500;
                        break;
                    default:
                        break;
                }
                var config = new ThPlaneConfig()
                {
                    IfcFilePath = pfnr.StringResult,
                    SvgSavePath = "",
                    DrawingType = drawingType,
                };
                config.JsonConfig.SvgConfig.image_size = null;
                config.JsonConfig.GlobalConfig.eye_dir = eye_dir;
                config.JsonConfig.GlobalConfig.up = up;
                config.JsonConfig.GlobalConfig.cut_position = cut_position;
                config.JsonConfig.GlobalConfig.relative_cut_position = ralative_cut_position;

                if (drawingType == DrawingType.Section)
                {
                    var pio = new PromptIntegerOptions("\n请输入裁剪位置");
                    pio.AllowArbitraryInput = true;
                    pio.AllowNegative = true;
                    pio.AllowNone = false;
                    pio.AllowZero = true;
                    var pdr = Active.Editor.GetInteger(pio);
                    if (pdr.Status == PromptStatus.OK)
                    {
                        config.JsonConfig.GlobalConfig.cut_position = pdr.Value;
                    }
                    else
                    {
                        return;
                    }
                }
                var generator = new ThArchitectureGenerator(config, printParameter);
                generator.Generate();
            }
        }
    }
}
