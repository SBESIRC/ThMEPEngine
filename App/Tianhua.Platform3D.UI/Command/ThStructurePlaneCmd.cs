﻿using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Threading;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO.SVG;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThPlatform3D.Common;
using ThPlatform3D.StructPlane;
using Tianhua.Platform3D.UI.StructurePlane;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;

namespace Tianhua.Platform3D.UI.Command
{
    public class ThStructurePlaneCmd : IAcadCommand, IDisposable
    {
        private bool _flag;
        public ThStructurePlaneCmd()
        {
            _flag = false;

        }
        public ThStructurePlaneCmd(bool flag)
        {
            _flag = flag;
        }
        public void Dispose()
        {
            //
        }
        public void Execute()
        {
            Active.Document.Window.Focus();
            var fileName =SelectFile(_flag);
            if (string.IsNullOrEmpty(fileName) || fileName == "error")
            {
                return;
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
            if (!generator.IsSuccessedBuildSvgFiles)
            {
                return;
            }

            // 查找 storeys.json
            var storeyFile = GetStoreyFileName(fileName);
            // 把楼层文件的解析的成果
            ThDrawingParameterConfig.Instance.Storeies = ReadStoreys(storeyFile);
            // 序列化以当前名结尾的Storey.json

            // 打开成图参数设置
            if(_flag)
            {
                // for demo
                ThStopWatchService.Start();
                // 更新 printParameter，将生成的Svg打印到图纸上
                generator.SetDrawingType(ThStructurePlaneCommon.WallColumnDrawingName); // 把成图类型传入到Generator
                printParameter.DrawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
                printParameter.DefaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
                printParameter.FloorSpacing = ThDrawingParameterConfig.Instance.FloorSpacing;
                printParameter.ShowSlabHatchAndMark = ThDrawingParameterConfig.Instance.ShowSlabHatchAndMark;
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
            else
            {
                var parameterUI = new DrawingParameterSetUI();
                AcadApp.ShowModalWindow(parameterUI);
                if (parameterUI.IsGoOn)
                {
                    ThStopWatchService.Start();
                    // 更新 printParameter，将生成的Svg打印到图纸上
                    generator.SetDrawingType(ThDrawingParameterConfig.Instance.DrawingType); // 把成图类型传入到Generator
                    printParameter.DrawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
                    printParameter.DefaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
                    printParameter.FloorSpacing = ThDrawingParameterConfig.Instance.FloorSpacing;
                    printParameter.ShowSlabHatchAndMark = ThDrawingParameterConfig.Instance.ShowSlabHatchAndMark;
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

        private string SelectFile(bool flag=false)
        {
           return Program.Run(flag);
        }        
    }

    static class Program
    {
        static Mutex CadMutex = null;
        static Mutex ViewerMutex = null;
        static Mutex FileMutex = null;
        static Mutex FlagMutex = null;
        static Mutex ViewerMutex2 = null;

        public static string Run(bool cutFlag)
        {
            try
            {
                string cutType = "";
                if(cutFlag)
                {
                    cutType = "test";
                }
                else
                {
                    cutType = "structrue";
                }
                var flag = Mutex.TryOpenExisting("viewerMutex", out ViewerMutex);
                if (!flag) return "";
                var flag2 = Mutex.TryOpenExisting("fileMutex", out FileMutex);
                if (!flag2) return "";
                var flag3 = Mutex.TryOpenExisting("flagMutex", out FlagMutex);
                if (!flag3) return "";
                

                InitMutex();
                
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("getFileType", 1024 * 1024, MemoryMappedFileAccess.ReadWrite))
                {
                    using (var stream = mmf.CreateViewStream())
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, cutType);
                    }
                    ViewerMutex2.ReleaseMutex();
                    FlagMutex.WaitOne();
                }
                //FileMutex.WaitOne();
                FileMutex.WaitOne();
                string getName = "";
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("getFileName"))
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        getName = (string)formatter.Deserialize(stream);
                    }
                }
                CadMutex.ReleaseMutex();
                ViewerMutex.WaitOne();
                return getName;
            }
            catch(Exception ex)
            {
                return "error";
            }
            finally
            {
                CadMutex?.Dispose();
                ViewerMutex?.Dispose();
                FileMutex?.Dispose();
                FlagMutex?.Dispose();
                ViewerMutex2?.Dispose();
            }
            return "";
        }

        static void InitMutex()
        {
            var cadMutexName = "cadMutex";
            var viewerMutexName2 = "viewerMutex2";

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
            try
            {
                ViewerMutex2 = new Mutex(true, viewerMutexName2, out _);
            }
            catch
            {
                ViewerMutex2 = Mutex.OpenExisting(viewerMutexName2, System.Security.AccessControl.MutexRights.FullControl);
                ViewerMutex2.Dispose();
                ViewerMutex2 = new Mutex(true, viewerMutexName2, out _);
            }
        }
    }
}
