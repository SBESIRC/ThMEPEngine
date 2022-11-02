using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO.SVG;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThPlatform3D.Common;
using ThPlatform3D.StructPlane;
using ThPlatform3D.StructPlane.Service;
using Tianhua.Platform3D.UI.StructurePlane;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Tianhua.Platform3D.UI.Command
{
    public class ThStructurePlaneCmd : IAcadCommand, IDisposable
    {
        public ThStructurePlaneCmd()
        {
            //
        }
        public void Dispose()
        {
            //
        }

        public void Execute()
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
                ThStopWatchService.Start();
                var ydbToIfcService = new ThYdbToIfcConvertService();
                fileName = ydbToIfcService.Convert(fileName);
                ThStopWatchService.Stop();
                ThStopWatchService.Print("YdbToIfc解析时间：");
            }

            // 转Svg ，*.Storey.txt
            ThStopWatchService.Start();
            var printParameter = new ThPlanePrintParameter()
            {
                DrawingScale = "1:100",
            };
            var config = CreatePlaneConfig(fileName);
            var generator = new ThStructurePlaneGenerator(config, printParameter);
            generator.Convert();
            ThStopWatchService.Stop();
            ThStopWatchService.Print("IfcToSvg解析时间：");

            // 查找 storeys.json
            var storeyFile = GetStoreyFileName(fileName);
            // 把楼层文件的解析的成果
            ThDrawingParameterConfig.Instance.Storeies = ReadStoreys(storeyFile);
            // 序列化以当前名结尾的Storey.json


            // 打开成图参数设置
            var parameterUI = new DrawingParameterSetUI();
            AcadApp.ShowModalWindow(parameterUI);
            if(parameterUI.IsGoOn)
            {
                ThStopWatchService.Start();
                // 更新 printParameter，将生成的Svg打印到图纸上
                generator.SetDrawingType(ThDrawingParameterConfig.Instance.DrawingType); // 把成图类型传入到Generator
                printParameter.DrawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
                printParameter.DefaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
                printParameter.FloorSpacing = ThDrawingParameterConfig.Instance.FloorSpacing;
                if (ThDrawingParameterConfig.Instance.IsAllStorey)
                {
                    generator.SetStdFlrNo("");
                }
                else
                {
                    generator.SetStdFlrNo(ThDrawingParameterConfig.Instance.StdFlrNo);
                }                
                generator.Generate();
                ThStopWatchService.Stop();
                ThStopWatchService.Print("成图打印时间：");
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
            if(File.Exists(ifcFileName))
            {
                var fi = new FileInfo(ifcFileName);
                var storeyFileName = Path.GetFileNameWithoutExtension(ifcFileName) + ".storeys.txt";
                return Path.Combine(fi.DirectoryName, storeyFileName);
            }
            else
            {
                return "";
            }
        }

        private List<ThIfcStoreyInfo> ReadStoreys(string fileName)
        {
            if(File.Exists(fileName))
            {
                return ThParseStoreyService.ParseFromTxt(fileName);
            }
            else
            {
                return new List<ThIfcStoreyInfo>();
            }
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

    class Program
    {
        static Mutex CadMutex = null;
        static Mutex ViewerMutex = null;
        public static void Run()
        {
            try
            {
                var flag = Mutex.TryOpenExisting("viewerMutex", out ViewerMutex);
                if (!flag) return;
                InitMutex();
                CadMutex.ReleaseMutex();
                ViewerMutex.WaitOne();
            }
            catch
            {
                return;
            }
            finally
            {
                CadMutex?.Dispose();
                ViewerMutex?.Dispose();
            }
        }

        static void InitMutex()
        {
            var cadMutexName = "cutdata";
            try
            {
                CadMutex = new Mutex(true, cadMutexName, out bool cadMutexCreated);
            }
            catch
            {
                CadMutex = Mutex.OpenExisting(cadMutexName, System.Security.AccessControl.MutexRights.FullControl);
                CadMutex.Dispose();
                CadMutex = new Mutex(true, cadMutexName, out _);
            }
        }
    }
}
