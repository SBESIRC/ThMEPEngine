using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThCADExtension;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.Common
{
    /// <summary>
    /// 用于成图的配置参数
    /// </summary>
    public class ThPlaneConfig
    {
        public PlaneJsonConfig JsonConfig { get; set; }
        /// <summary>
        /// 传入的IFC文件
        /// 对应config.json的path
        /// </summary>
        public string IfcFilePath { get; set; } = "";
        /// <summary>
        /// 输出的Svg文件保存路径
        /// </summary>
        public string SvgSavePath { get; set; } = "";
        /// <summary>
        /// Log文件保存路径
        /// </summary>
        public string LogSavePath { get; set; } = "";
        /// <summary>
        /// 成图类型
        /// 写到config.json        
        /// </summary>
        public string ImageType 
        {
            get
            {
                return DrawingType.GetDrawingType();
            }
        } 
        /// <summary>
        /// 成图类型
        /// </summary>
        public DrawingType DrawingType { get; set; } = DrawingType.Unknown;
        /// <summary>
        /// config.json的完整路径
        /// </summary>
        public string SvgConfigFilePath
        {
            get
            {
                return GetSvgConfgFilePath();
            }
        }
        /// <summary>
        /// elevation-generator.exe的完整路径
        /// </summary>
        public string ExeFilePath
        {
            get
            {
                //var exePath = GetLocalPath(); // exe路径要和当前Dll路径在一个地方     
                var exePath = GetWin64CommonPath();
                return Path.Combine(exePath, "elevation-generator.exe");
            }
        }
        public string Arguments
        {
            get
            {
                return BuildArgument(SvgConfigFilePath, IfcFilePath, SvgSavePath, LogSavePath);
            }
        }
        public string BuildArgument(string svgConfigFilePath,string ifcFilePath,string svgSavePath,string logSavePath)
        {
            return
                   "--config_path " + AddQuotationMarks(ModifyPath(svgConfigFilePath)) +
                   " --input_path " + AddQuotationMarks(ModifyPath(ifcFilePath)) +
                   " --output_path " + AddQuotationMarks(ModifyPath(svgSavePath)) +
                   " --log_path " + AddQuotationMarks(ModifyPath(Path.Combine(logSavePath + "log.txt")));
        }
        private string AddQuotationMarks(string path)
        {
            return "\"" + path + "\"";
        }
        /// <summary>
        /// 生成的Svg文件存放的目录
        /// </summary>
        public string SvgDefaultSavePath
        {
            get
            {
                return GetSvgDefaultSavePath();
            }
        }
        public string LogDefaultSavePath
        {
            get
            {
                return GetLogDefaultSavePath();
            }
        }
        public string IfcFileName
        {
            get
            {
                if(File.Exists(IfcFilePath))
                {
                    return Path.GetFileNameWithoutExtension(IfcFilePath);
                }
                else
                {
                    return "";
                }
            }
        }
        public ThPlaneConfig()
        {
            JsonConfig = new PlaneJsonConfig();
        }
        public void Configure()
        { 
            SetSavePath();

            if(DrawingType == DrawingType.Elevation)
            {
                // 对于立面图，Svg保存路径要给绝对路径
                JsonConfig.SvgConfig.save_path = GetSingleFullSvgSavePath(SvgSavePath, 1, "elevation");
            }
            else if(DrawingType == DrawingType.Section)
            {
                // 对于剖面图，Svg保存路径要给绝对路径
                JsonConfig.SvgConfig.save_path = GetSingleFullSvgSavePath(SvgSavePath, 1, "section");
            }
            else
            {
                JsonConfig.SvgConfig.save_path = SvgSavePath;
            }
            JsonConfig.ObjConfig.path = IfcFilePath;
            JsonConfig.DebugConfig.log_path = LogSavePath;
            JsonConfig.GlobalConfig.image_type = ImageType;
            //考虑写入权限的问题，暂不写入配置
            var svgConfig = ReadJson(SvgConfigFilePath);
            if (svgConfig == null)
            {
                return;
            }
            SetValues(svgConfig,JsonConfig);
            WriteJson(SvgConfigFilePath, svgConfig);
        }

        private string GetSingleFullSvgSavePath(string svgSavePath,int floorNo,string type)
        {
            if(!string.IsNullOrEmpty(IfcFilePath))
            { 
                // 获取Ifc路径
                var ifcFileName = Path.GetFileNameWithoutExtension(IfcFilePath);
                var outputSvgName = ifcFileName + "-" + floorNo + "-" + type+".svg";
                return Path.Combine(svgSavePath, outputSvgName);
            }
            else
            {
                return "";
            }
        }

        private string GetSvgConfgFilePath()
        {
            //var path = GetLocalPath();
            var path = GetWin64CommonPath();
            return Path.Combine(path,"config.json");
        }

        private string GetWin64CommonPath()
        {
            return ThCADCommon.Win64CommonPath();
        }

        private string GetLocalPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private string GetSvgDefaultSavePath()
        {
            return Path.GetTempPath();
            //return Path.Combine(Environment.GetEnvironmentVariable("windir"),"TEMP");
        }

        private string GetLogDefaultSavePath()
        {
            return Path.GetTempPath();
            //return Path.Combine(Environment.GetEnvironmentVariable("windir"),"TEMP");
        }

        private void SetSavePath()
        {
            if (string.IsNullOrEmpty(SvgSavePath))
            {
                SvgSavePath = SvgDefaultSavePath;
            }
            else
            {
                if (!Directory.Exists(SvgSavePath))
                {
                    SvgSavePath = SvgDefaultSavePath;
                }
            }

            if (string.IsNullOrEmpty(LogSavePath))
            {
                LogSavePath = LogDefaultSavePath;
            }
            else
            {
                if (!Directory.Exists(LogSavePath))
                {
                    LogSavePath = LogDefaultSavePath;
                }
            }
        }

        private void SetValues(JObject jObject,PlaneJsonConfig jsonConfig)
        {
            try
            {
                jObject["ObjConfig"]["path"] = ModifyPath(jsonConfig.ObjConfig.path);
                jObject["ObjConfig"]["current_floor"] = jsonConfig.ObjConfig.current_floor;
                jObject["ObjConfig"]["high_floor"] = jsonConfig.ObjConfig.high_floor;
                jObject["SvgConfig"]["save_path"] = ModifyPath(jsonConfig.SvgConfig.save_path);
                jObject["DebugConfig"]["print_time"] = jsonConfig.DebugConfig.print_time;
                jObject["DebugConfig"]["log_path"] = ModifyPath(jsonConfig.DebugConfig.log_path);
                jObject["GlobalConfig"]["image_type"] = jsonConfig.GlobalConfig.image_type;
                jObject["GlobalConfig"]["cut_position"] = jsonConfig.GlobalConfig.cut_position;
                jObject["SvgConfig"]["image_size"] = jsonConfig.SvgConfig.image_size;
                jObject["GlobalConfig"]["eye_dir"]["x"] = jsonConfig.GlobalConfig.eye_dir.x;
                jObject["GlobalConfig"]["eye_dir"]["y"] = jsonConfig.GlobalConfig.eye_dir.y;
                jObject["GlobalConfig"]["eye_dir"]["z"] = jsonConfig.GlobalConfig.eye_dir.z;
                jObject["GlobalConfig"]["up"]["x"] = jsonConfig.GlobalConfig.up.x;
                jObject["GlobalConfig"]["up"]["y"] = jsonConfig.GlobalConfig.up.y;
                jObject["GlobalConfig"]["up"]["z"] = jsonConfig.GlobalConfig.up.z;
            }
            catch
            {
            }
        }

        public string ModifyPath(string path)
        {
            return path.Replace("\\","/");
        }

        private bool CheckValid()
        {
            if (string.IsNullOrEmpty(IfcFilePath) ||
                string.IsNullOrEmpty(ExeFilePath))
            {
                return false;
            }
            if(!File.Exists(IfcFilePath))
            {
                return false;
            }
            if (!File.Exists(ExeFilePath))
            {
                return false;
            }
            return true;
        }        
        /// <summary>
        /// 写入JSON文件
        /// </summary>
        /// <param name="jsonFile"></param>
        /// <param name="jObject"></param>
        private void WriteJson(string jsonFile, JObject jObject)
        {
            using (var streamWriter = new System.IO.StreamWriter(jsonFile))
            {
                streamWriter.Write(jObject.ToString());
            }
        }
        private JObject ReadJson(string jsonFile)
        {
            try
            {
                using (var streamReader = System.IO.File.OpenText(jsonFile))
                {
                    using (JsonTextReader reader = new JsonTextReader(streamReader))
                    {
                        JObject jObject = (JObject)JToken.ReadFrom(reader);
                        return jObject;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }

}
