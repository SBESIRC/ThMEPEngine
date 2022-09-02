using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.Common;
using ThPlatform3D.ArchitecturePlane;
using ThPlatform3D.ArchitecturePlane.Print;
using ThPlatform3D.StructPlane.Print;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D
{
    public class ThPlatform3DApp : IExtensionApplication
    {
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        /// <summary>
        ///  读取结构SvgFile
        /// </summary>
        [CommandMethod("TIANHUACAD", "THReadStruSvg", CommandFlags.Modal)]
        public void THReadStruSvg()
        {
            var pofo = new PromptOpenFileOptions("\n选择要解析的Svg文件");
            pofo.Filter = "Svg files (*.svg)|*.svg";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 解析
                var svg = new ThStructureSVGReader();
                svg.ReadFromFile(pfnr.StringResult);

                //// 沿着X轴镜像
                //var mt = Matrix3d.Rotation(System.Math.PI, Vector3d.XAxis, Point3d.Origin);
                //geometries.ForEach(o => o.Boundary.TransformBy(mt));

                // Print
                var svgInput = new ThSvgInput()
                {
                    Geos = svg.Geos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                };
                var printPara = new ThPlanePrintParameter()
                {
                    DrawingScale = "1:100",
                };
                Active.Database.ImportStruPlaneTemplate();
                var prinService = new ThStruPlanDrawingPrinter(svgInput, printPara);
                prinService.Print(Active.Database);
            }
        }
        /// <summary>
        ///  读取SvgFile
        /// </summary>
        [CommandMethod("TIANHUACAD", "THReadArchSvg", CommandFlags.Modal)]
        public void THReadArchSvg()
        {
            var pofo = new PromptOpenFileOptions("\n选择要解析的Svg文件");
            pofo.Filter = "Svg files (*.svg)|*.svg";
            var pfnr = Active.Editor.GetFileNameForOpen(pofo);
            if (pfnr.Status == PromptStatus.OK)
            {
                // 解析
                var svg = new ThArchitectureSVGReader();
                svg.ReadFromFile(pfnr.StringResult);

                // Print
                var svgInput = new ThSvgInput()
                {
                    Geos = svg.Geos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                    ComponentInfos = svg.ComponentInfos,
                };
                var printParameter = new ThPlanePrintParameter()
                {
                    DrawingScale = "1:100"
                };
                var drawingType = svg.DocProperties.GetDrawingType();
                ThArchDrawingPrinter printer = null;
                switch (drawingType)
                {
                    case DrawingType.Plan:
                        printer = new ThArchPlanDrawingPrinter(svgInput, printParameter);
                        break;
                    case DrawingType.Elevation:
                        printer = new ThArchElevationDrawingPrinter(svgInput, printParameter);
                        break;
                    case DrawingType.Section:
                        printer = new ThArchSectionDrawingPrinter(svgInput, printParameter);
                        break;
                }
                if (printer != null)
                {
                    // 从模板导入要打印的图层
                    if (!ThImportDatabaseService.ImportArchDwgTemplate(Active.Database))
                    {
                        return;
                    }
                    printer.Print(Active.Database);
                    Active.Document.SendCommand("HatchToBack", "\n");
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THAMUTSC", CommandFlags.Modal)]
        public void THAUTSC()
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

                var printParameter = new ThPlanePrintParameter()
                {
                    DrawingScale = "1:100",
                };

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
