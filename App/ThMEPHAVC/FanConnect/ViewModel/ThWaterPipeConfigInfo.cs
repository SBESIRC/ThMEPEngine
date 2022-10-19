using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ThCADExtension;

namespace ThMEPHVAC.FanConnect.ViewModel
{
    public class ThWaterSystemConfigInfo
    {
        public int SystemType { set; get; }//系统选择：0：水系统 1：冷媒系统
        public int HorizontalType { set; get; }//水平同异程 0：异程 1：同程
        public int PipeSystemType { set; get; }//水系统管制 0:两管制 1:四管制
        public bool IsCodeAndHotPipe { set; get; }//空调冷热水管
        public bool IsCWPipe { set; get; }//冷凝水管
        public bool IsCoolPipe { set; get; }//空调冷却水管
        public bool IsGenerValve { set; get; }//是否穿框线处生成阀门
        public string FrictionCoeff { set; get; }//空调水比摩阻
        public bool IsACPipeDim { set; get; } //冷媒管径
        public ObservableCollection<ThACPipeDimConfigFile> ACPipeDimConfigFileList { get; set; }
        public ThACPipeDimConfigFile ACPipeDimConfigFile { get; set; }//冷媒管径配置表
        public double MarkHeigth { set; get; }//中心标高
        public ThWaterSystemConfigInfo()
        {
            SystemType = 0;
            HorizontalType = 0;
            PipeSystemType = 0;
            IsCodeAndHotPipe = true;
            IsCWPipe = true;
            IsCoolPipe = false;
            IsGenerValve = true;
            IsACPipeDim = false;
            FrictionCoeff = "200";
            MarkHeigth = 3.0;

            var romingFile = CopyACPipeConfigFile();
            ACPipeDimConfigFile = new ThACPipeDimConfigFile(romingFile);
            ACPipeDimConfigFileList = new ObservableCollection<ThACPipeDimConfigFile>() { ACPipeDimConfigFile };
        }

        private static string CopyACPipeConfigFile()
        {
            var romingFolder = (string)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("ROAMABLEROOTPREFIX");
            var directoryInfo = new DirectoryInfo(romingFolder);
            romingFolder = directoryInfo.Parent.Parent.Parent.FullName;
            romingFolder = Path.Combine(romingFolder, "LoadCalculationConfig");
            var templateFolder = Path.Combine(ThCADCommon.SupportPath(), "LoadCalculationConfig");

            var romingFile = Path.Combine(romingFolder, ThFanConnectCommon.ACPipeConfigFileName);
            var templateFile = Path.Combine(templateFolder, ThFanConnectCommon.ACPipeConfigFileName);

            if (File.Exists(romingFile) == false && File.Exists(templateFile) == true)
            {
                if (Directory.Exists(romingFolder) == false)
                {
                    Directory.CreateDirectory(romingFolder);
                }
               
                File.Copy(templateFile, romingFile);
            }

            return romingFile;
        }
    }
    public class ThWaterValveConfigInfo
    {
        public List<string> FeedPipeValves { set; get; }//给水管阀门
        public List<string> ReturnPipeValeves { set; get; }//回水管阀门
        public string MapScale { set; get; }//出图比例
        public List<Entity> RoomObb { set; get; }//房间框线
        public ThWaterValveConfigInfo()
        {
            FeedPipeValves = new List<string>();
            ReturnPipeValeves = new List<string>();
            MapScale = "1:100";
            RoomObb = new List<Entity>();
        }
    }

    public class ThWaterPipeConfigInfo
    {
        public ThWaterSystemConfigInfo WaterSystemConfigInfo { set; get; }//系统参数信息
        public ThWaterValveConfigInfo WaterValveConfigInfo { set; get; }//阀门配置信息
        public ThWaterPipeConfigInfo()
        {
            WaterSystemConfigInfo = new ThWaterSystemConfigInfo();
            WaterValveConfigInfo = new ThWaterValveConfigInfo();
        }
    }

    public class ThACPipeDimConfigFile
    {
        public string FullPath { get; set; } = "";
        public string FileName { get; set; } = "";
        public ThACPipeDimConfigFile(string fullPath)
        {
            FullPath = fullPath;
            FileName = Path.GetFileName(fullPath);
        }
    }
}
