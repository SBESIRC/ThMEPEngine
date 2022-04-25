using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThStructurePlaneConfig
    {
        /// <summary>
        /// 传入的IFC文件
        /// </summary>
        public string IfcFilePath { get; set; }
        /// <summary>
        /// 输出的Svg文件保存路径
        /// </summary>
        public string SvgSavePath { get; set; }
        /// <summary>
        /// Log文件保存路径
        /// </summary>
        public string LogSavePath { get; set; }
        /// <summary>
        /// 当前层名
        /// </summary>
        public string CurrentFloor { get; set; }
        /// <summary>
        /// 上一层名
        /// </summary>
        public string HighFloor { get; set; }
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
                    "--config_path " + ModifyPath(SvgConfigFilePath) +
                    " --input_path " + ModifyPath(IfcFilePath) +
                    " --output_path " + ModifyPath(SvgSavePath) +
                    " --log_path " + ModifyPath(Path.Combine(LogSavePath + "log.txt"));
            }
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
        public ThStructurePlaneConfig()
        {
            IfcFilePath = "";
            SvgSavePath = "";
            LogSavePath = "";
        }
        public bool Configure()
        {
            if (!CheckValid())
            {
                return false;
            }
            SetSavePath();
            return true;
            //考虑写入权限的问题，暂不写入配置
            //var svgConfig = ReadJson(SvgConfigFilePath);
            //if(svgConfig==null)
            //{
            //    return;
            //}
            //SetValues(svgConfig);
            //WriteJson(SvgConfigFilePath, svgConfig);
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

        private void SetValues(JObject jObject)
        {
            try
            {
                if(!string.IsNullOrEmpty(IfcFilePath))
                {
                    jObject["ObjConfig"]["path"] = ModifyPath(IfcFilePath);
                }
                if (!string.IsNullOrEmpty(SvgSavePath))
                {
                    jObject["SvgConfig"]["save_path"] = SvgSavePath;
                }
                if (!string.IsNullOrEmpty(CurrentFloor))
                {
                    jObject["ObjConfig"]["current_floor"] = CurrentFloor;
                }
                if(!string.IsNullOrEmpty(HighFloor))
                {
                    jObject["ObjConfig"]["high_floor"] = HighFloor;
                }
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
