using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.Common
{
    internal class ThPlaneConfig
    {
        public PlaneJsonConfig JsonConfig { get; set; }
        /// <summary>
        /// 图纸比例
        /// </summary>
        public string DrawingScale { get; set; } = "";
        /// <summary>
        /// 楼层间距
        /// </summary>
        public double FloorSpacing { get; set; } = 100000;

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
                // exe路径要和当前Dll路径在一个地方     
                var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(currentDllPath, "elevation-generator.exe");
            }
        }
        public string Arguments
        {
            get
            {
                return 
                    "--config_path " + AddQuotationMarks(ModifyPath(SvgConfigFilePath)) +
                    " --input_path " + AddQuotationMarks(ModifyPath(IfcFilePath)) +
                    " --output_path " + AddQuotationMarks(ModifyPath(SvgSavePath)) +
                    " --log_path " + AddQuotationMarks(ModifyPath(Path.Combine(LogSavePath + "log.txt")));
            }
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
                var fi = new FileInfo(IfcFilePath);
                if(fi.Exists)
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

            JsonConfig.SvgConfig.save_path = SvgSavePath;
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

        private string GetSvgConfgFilePath()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(path,"config.json");
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
                var dirInfo = new DirectoryInfo(SvgSavePath);
                if (!dirInfo.Exists)
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
                var dirInfo = new DirectoryInfo(LogSavePath);
                if (!dirInfo.Exists)
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
            var fileInfo1 = new FileInfo(IfcFilePath);
            if(!fileInfo1.Exists)
            {
                return false;
            }
            var fileinfo2 = new FileInfo(ExeFilePath);
            if (!fileinfo2.Exists)
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
