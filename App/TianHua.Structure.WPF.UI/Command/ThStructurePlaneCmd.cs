using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.IO;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.Common;
using ThMEPStructure.StructPlane;
using ThMEPStructure.StructPlane.Service;
using TianHua.Structure.WPF.UI.StructurePlane;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Structure.WPF.UI.Command
{
    public class ThStructurePlaneCmd : ThMEPBaseCommand, IDisposable
    {
        public ThStructurePlaneCmd()
        {
            ActionName = "结构成图";
            CommandName = "THSMUTSC";
        }
        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            // 选择文件名
            var fileName = SelectFile();
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            // 选择成图类型
            var drawingType = SelectDrawingType();
            if(string.IsNullOrEmpty(drawingType))
            {
                return;
            }

            // ydb to ifc
            if (Path.GetExtension(fileName).ToUpper() == ".YDB")
            {
                var ydbToIfcService = new ThYdbToIfcConvertService();
                fileName = ydbToIfcService.Convert(fileName);
            }

            // 转Svg ，*.Storey.txt
            var printParameter = new ThPlanePrintParameter()
            {
                DrawingScale = "1:100",
            };
            var config = CreatePlaneConfig(fileName);
            var generator = new ThStructurePlaneGenerator(config, printParameter, drawingType);
            generator.Convert();

            // 查找 storeys.json
            var storeyFile = GetStoreyFileName(fileName);

            // 打开成图参数设置
            ThDrawingParameterConfig.Instance.Storeies = ReadStoreys(storeyFile);
            var parameterUI = new DrawingParameterSetUI();
            AcadApp.ShowModalWindow(parameterUI);

            // 更新 printParameter，将生成的Svg打印到图纸上
            printParameter.DrawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
            printParameter.DefaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
            generator.Generate();
        }

        private ThPlaneConfig CreatePlaneConfig(string ifcFilePath)
        {
            var config = new ThPlaneConfig()
            {
                IfcFilePath = ifcFilePath,
                DrawingType = DrawingType.Structure,
            };
            config.JsonConfig.GlobalConfig.eye_dir = new Direction(0, 0, -1);
            config.JsonConfig.GlobalConfig.up = new Direction(0, 1, 0);
            return config;
        }

        private string GetStoreyFileName(string ifcFileName)
        {
            var storeyFileName = Path.GetFileNameWithoutExtension(ifcFileName) + "storeys.json";
            return File.Exists(storeyFileName) ? storeyFileName : "";
        }

        private List<string> ReadStoreys(string fileName)
        {
            // TODO
            return new List<string>();
        }

        private string SelectFile()
        {
            // 选择文件格式
            var fileFormatVM = new FileFormatSelectVM();
            var ui = new FileFormatSelectorUI(fileFormatVM);
            ui.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            AcadApp.ShowModalWindow(ui);

            // 选择文件
            return fileFormatVM.BrowseFile();
        }

        private string SelectDrawingType()
        {
            var options = new PromptKeywordOptions("\n选择出图方式");
            options.Keywords.Add("结构平面图", "P", "结构平面图(P)");
            options.Keywords.Add("墙柱施工图", "D", "墙柱施工图(D)");
            options.Keywords.Default = "结构平面图";
            options.AllowArbitraryInput = true;
            var pr = Active.Editor.GetKeywords(options);
            if (pr.Status == PromptStatus.OK)
            {
                return pr.StringResult;
            }
            else
            {
                return "";
            }
        }
    }
}
