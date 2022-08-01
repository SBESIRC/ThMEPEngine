using System.IO;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.TSSD
{
    public class TSSDWallColumnConfigWriter
    {
        private TSSDEdition edition;
        private string section = "墙一-墙柱设置";
        private string concreteGradeKey = "砼强度等级";
        private string wallAntiSeismicGradeKey = "墙抗震等级";
        private string wallLocationKey = "墙所在部位";
        private string protectThickKey = "保护层(mm)";
        private string drawingScaleKey = "绘图比例";
        private string wallColumnElevationKey = "墙柱标高";
        private string pointReinforceLineWeightKey = "点筋线宽";
        private string stirrupLineWeightKey = "箍筋线宽";

        public TSSDWallColumnConfigWriter(TSSDEdition edition = TSSDEdition.TSSD2022)
        {
            this.edition = edition;
        }
        public void WriteToIni(ThWallColumnReinforceConfig config)
        {
            //参数来源于 ThWallColumnReinforceConfig
            var tssdPath = GetTSSDPath();
            if (!IsDirectoryExists(tssdPath))
            {
                return; 
            }
            var tssdIniPath = GetTssdPrgInfFilePath(tssdPath);
            if (!IsFileExists(tssdIniPath))
            {
                return;
            }
            // 写入Ini
            Write(config, tssdIniPath);
        }

        private void Write(ThWallColumnReinforceConfig config,string filePath)
        {
            // 砼强度等级
            var concreteGradeIndex = GetConcreteGradeIndex(config.ConcreteStrengthGrade);
            ThIniTool.WriteIni(this.section, concreteGradeKey,concreteGradeIndex, filePath);

            // 墙抗震等级
            var wallAntiSeismicGradeIndex = GetAntiSeismicGradeIndex(config.AntiSeismicGrade);
            ThIniTool.WriteIni(this.section, wallAntiSeismicGradeKey, wallAntiSeismicGradeIndex, filePath);

            // 墙所在部位
            var wallLocationIndex = GetWallLocationIndex(config.WallLocation);
            ThIniTool.WriteIni(this.section, wallLocationKey, wallLocationIndex, filePath);

            // 保护层厚度
            ThIniTool.WriteIni(this.section, protectThickKey, config.C.ToString(), filePath);

            // 绘图比例
            var drawingScale = GetDrawingScaleValue(config.DrawScale);
            ThIniTool.WriteIni(this.section, drawingScaleKey, drawingScale, filePath);

            // 墙柱标高
            ThIniTool.WriteIni(this.section, wallColumnElevationKey, config.Elevation,filePath);

            // 点筋线宽
            ThIniTool.WriteIni(this.section, pointReinforceLineWeightKey, config.PointReinforceLineWeight.ToString(), filePath);

            // 箍筋线宽
            ThIniTool.WriteIni(this.section, stirrupLineWeightKey, config.StirrupLineWeight.ToString(), filePath);
        }

        private string GetAntiSeismicGradeIndex(string antiSeismicGrade)
        {
            //0-特1级，1-一级，2-二级，3-三级，4-四级，5-非抗震
            switch (antiSeismicGrade)
            {
                case "特1级":
                    return "0";
                case "一级":
                    return "1";
                case "二级":
                    return "2";
                case "三级":
                    return "3";
                case "四级":
                    return "4";
                case "非抗震":
                    return "5";
                default:
                    return "";
            }
        }

        private string GetWallLocationIndex(string wallLocation)
        {
            //0-底部加强区，1-一般部位
            switch (wallLocation)
            {
                case "底部加强区":
                    return "0";
                case "其它部位":
                    return "1";               
                default:
                    return "";
            }
        }

        private string GetConcreteGradeIndex(string concreteGrade)
        {
            switch(concreteGrade)
            {
                case "C15":
                    return "0";
                case "C20":
                    return "1";
                case "C25":
                    return "2";
                case "C30":
                    return "3";
                case "C35":
                    return "4";
                case "C40":
                    return "5";
                case "C45":
                    return "6";
                case "C50":
                    return "7";
                case "C55":
                    return "8";
                case "C60":
                    return "9";
                case "C65":
                    return "10";
                case "C70":
                    return "11";
                case "C75":
                    return "12";
                case "C80":
                    return "13";
                default:
                    return "";
            }
        }

        private string GetDrawingScaleValue(string drawScale)
        {
            //"1:1", "1:10", "1:20", "1:25", "1:30", "1:50"
            switch (drawScale)
            {
                case "1:1":
                    return "1";
                case "1:10":
                    return "10";
                case "1:20":
                    return "20";
                case "1:25":
                    return "25";
                case "1:30":
                    return "30";
                case "1:50":
                    return "50";
                default:
                    return "";
            }
        }

        private bool IsDirectoryExists(string path)
        {
            var di =new DirectoryInfo(path);
            return di.Exists;
        }

        private bool IsFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        private string GetTssdPrgInfFilePath(string tssdPath)
        {
            if(this.edition == TSSDEdition.TSSD2022)
            {
                return GetTssd2022PrgInfFilePath(tssdPath);
            }
            else
            {
                return "";
            }
        }
        private string GetTSSDPath()
        {
            if (this.edition == TSSDEdition.TSSD2022)
            {
                return GetTSSD2022Path();
            }
            else
            {
                return "";
            }
        }

        private string GetTSSD2022Path()
        {
            var path = @"SOFTWARE\TszCAD\TSDP2022";
            var paramName = "Path";
            return ThRegistryTool.GetLocalMachineRegistryValue(path, paramName);
        }
        private string GetTssd2022PrgInfFilePath(string tssdPath)
        {
            return Path.Combine(tssdPath, "Prg\\tssdpro.ini");
        }
    }
    public enum TSSDEdition
    {
        TSSD2021,
        TSSD2022,
    }
}
