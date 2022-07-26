﻿using AcHelper;
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
            CommandName = "THSMBT";
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
            var generator = new ThStructurePlaneGenerator(config, printParameter);
            generator.Convert();

            // 查找 storeys.json
            var storeyFile = GetStoreyFileName(fileName);

            // 打开成图参数设置
            ThDrawingParameterConfig.Instance.Storeies = ReadStoreys(storeyFile);
            var parameterUI = new DrawingParameterSetUI();
            AcadApp.ShowModalWindow(parameterUI);
            if(parameterUI.IsGoOn)
            {
                // 更新 printParameter，将生成的Svg打印到图纸上
                printParameter.DrawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
                printParameter.DefaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
                generator.SetDrawingType(ThDrawingParameterConfig.Instance.DrawingType); // 把成图类型传入到Generator
                generator.Generate();
            }
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
            return fileFormatVM.SelectedFileName;
        }
    }
}