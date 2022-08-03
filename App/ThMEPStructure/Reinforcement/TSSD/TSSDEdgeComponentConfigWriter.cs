using System;
using System.IO;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.TSSD
{
    public class TSSDEdgeComponentConfigWriter :IDisposable
    {
        private TSSDEdition edition;
        private string section = "墙一-边缘构件绘制";
        private string calculateSoftwareKey = "计算软件";
        private string prefixKey = "前缀";
        private string wallColumnLayerKey = "墙柱图层";
        private string sortWayKey = "排序方式";
        private string leaderTypeKey = "引线形式";
        private string markPosKey = "标注位置";
        private string mergeSizeKey = "归并尺寸";
        private string mergeStirrupRatioKey = "归并配筋率(%)";
        private string mergeReinforceRatioKey = "归并配箍率(%)";
        private string mergeConsiderWallKey = "归并考虑墙体";
        private string constructPrefixKey = "构造前缀";
        private string constructPrefixStartNumberKey = "构件前缀的起始编号";
        private string constraintPrefixKey = "约束前缀";
        private string constraintPrefixStartNumberKey = "约束前缀的起始编号";

        public TSSDEdgeComponentConfigWriter(TSSDEdition edition = TSSDEdition.TSSD2022)
        {
            this.edition = edition;
        }

        public void Dispose()
        {
            //
        }

        public void WriteToIni(TSSDEdgeComponentConfig config)
        {
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

        private void Write(TSSDEdgeComponentConfig config,string filePath)
        {
            // 计算软件
            var calculateSoftwareIndex = GetCalculateSoftwareIndex(config.CalculationSoftware);
            ThIniTool.WriteIni(this.section, calculateSoftwareKey, calculateSoftwareIndex, filePath);

            // 前缀     
            var prefixIndex = GetPrefixIndex(config.Prefix);
            ThIniTool.WriteIni(this.section, prefixKey, prefixIndex, filePath);

            // 墙柱图层
            ThIniTool.WriteIni(this.section, wallColumnLayerKey, config.WallColumnLayer, filePath);

            // 排序方式
            var sortwayIndex = GetSortWayIndex(config.SortWay);
            ThIniTool.WriteIni(this.section, sortWayKey, sortwayIndex, filePath);

            // 引线形式
            var leaderTypeIndex = GetLeaderTypeIndex(config.LeaderType);
            ThIniTool.WriteIni(this.section, leaderTypeKey, leaderTypeIndex, filePath);

            // 标注位置
            var markPosIndex = GetMarkPosIndex(config.MarkPosition);
            ThIniTool.WriteIni(this.section, markPosKey, markPosIndex, filePath);

            // 归并尺寸
            ThIniTool.WriteIni(this.section, mergeSizeKey, config.MergeSize, filePath);

            // 归并配筋率(%)
            ThIniTool.WriteIni(this.section, mergeStirrupRatioKey, config.MergeStirrupRatio, filePath);

            // 归并配箍率(%)
            ThIniTool.WriteIni(this.section, mergeReinforceRatioKey, config.MergeReinforceRatio, filePath);

            // 归并考虑墙体
            var mergeConsiderWallIndex = config.MergeConsiderWall ? "1" : "0";
            ThIniTool.WriteIni(this.section, mergeConsiderWallKey, mergeConsiderWallIndex, filePath);

            // 构造前缀
            ThIniTool.WriteIni(this.section, constructPrefixKey, config.ConstructPrefix, filePath);
            // 构件前缀的起始编号
            ThIniTool.WriteIni(this.section, constructPrefixStartNumberKey, config.ConstructPrefixStartNumber, filePath);

            // 约束前缀
            ThIniTool.WriteIni(this.section, constraintPrefixKey, config.ConstraintPrefix, filePath);
            // 约束前缀的起始编号
            ThIniTool.WriteIni(this.section, constraintPrefixStartNumberKey, config.ConstraintPrefixStartNumber, filePath);
        }

        private string GetCalculationSoftwareIndex(string calculationSoftware)
        {
            switch(calculationSoftware)
            {
                case "PKPM(＜3.1)":
                    return "0";
                case "PKPM(≥3.1，＜4.3）":
                    return "1";
                case "PKPM(≥4.3）":
                    return "2";
                case "YJK":
                    return "3";
                case "智能识别计算软件":
                    return "4";
                default:
                    return "";
            }
        }

        private string GetCalculationSoftwareMark()
        {
            return " //0-PKPM(＜3.1)，1-PKPM(≥3.1，＜4.3），2-PKPM(≥4.3），3-YJK，4-智能识别计算软件";
        }

        private string GetPrefixIndex(string prefix)
        {
            switch(prefix)
            {
                case "统一":
                    return "0";
                case "仅构造约束":
                    return "1";
                case "构造约束、形状":
                    return "2";
                default:
                    return "";
            }
        }

        private string GetPrefixMark()
        {
            return "//0-统一，1-仅构造约束，2-构造约束、形状";
        }

        private string GetSortWayIndex(string sortway)
        {
            switch(sortway)
            {
                case "从左到右，从下到上":
                    return "0";
                case "从左到右，从上到下":
                    return "1";
                case "从右到左，从下到上":
                    return "3";
                case "从右到左，从上到下":
                    return "4";
                case "从下到上，从左到右":
                    return "5";
                case "从下到上，从右到左":
                    return "6";
                case "从上到下，从左到右":
                    return "7";
                case "从上到下，从右到左":
                    return "8";
                default:
                    return "";
            }
        }

        private string GetSortWayMark()
        {
            // 排序方式
            return " //0-从左到右，从下到上，1-从左到右，从上到下，3-从右到左，从下到上，4-从右到左，从上到下，5-从下到上，从左到右，6-从下到上，从右到左，7-从上到下，从左到右，8-从上到下，从右到左";
        }

        private string GetLeaderTypeIndex(string leaderType)
        {
            switch(leaderType)
            {
                case "无引出线":
                    return "0";
                case "斜线引出":
                    return "1";
                case "折线引出":
                    return "2";
                default:
                    return "";
            }
        }

        private string GetLeaderTypeMark()
        {
            return " //0-无引出线，1-斜线引出，2-折线引出"; // 引线形式
        }

        private string GetMarkPosIndex(string markPos)
        {
            switch (markPos)
            {
                case "左上":
                    return "0";
                case "左下":
                    return "1";
                case "右上":
                    return "2";
                case "右下":
                    return "3";
                default:
                    return "";
            }

        }

        private string GetMarkPositionMark()
        {
            return " //0-左上，1-左下，2-右上，3右下"; //标注位置
        }

        private string GetCalculateSoftwareIndex(string calculationsoftware)
        {
            //0-PKPM(＜3.1)，1-PKPM(≥3.1，＜4.3），2-PKPM(≥4.3），3-YJK，4-智能识别计算软件
            switch (calculationsoftware)
            {
                case "PKPM(＜3.1)":
                    return "0";
                case "PKPM(≥3.1，＜4.3）":
                    return "1";
                case "PKPM(≥4.3）":
                    return "2";
                case "YJK":
                    return "3";
                case "智能识别计算软件":
                    return "4";
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
}
