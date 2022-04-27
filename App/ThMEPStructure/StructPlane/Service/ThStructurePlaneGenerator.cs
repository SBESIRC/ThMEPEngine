using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AcHelper;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThStructurePlaneGenerator
    {
        private ThStructurePlaneConfig Config { get; set; }
        public ThStructurePlaneGenerator(ThStructurePlaneConfig config)
        {
            Config = config;
        }
        public void Generate()
        {
            // 先配置
            if(!Config.Configure())
            {
                return;
            }

            // 清除
            Clear();

            // 成图
            Plot();

            // 获取Svg
            var svgFiles = GetGeneratedSvgFiles();
            svgFiles = Sort(svgFiles);

            // 打印
            Print(svgFiles);

            // 删除
            Erase(svgFiles);
        }
        private void Plot()
        {
            using (var proc = new Process())
            {
                object output = null;                           
                proc.StartInfo.FileName = Config.ExeFilePath;                
                // 是否使用操作系统Shell启动
                proc.StartInfo.UseShellExecute = false;
                // 不显示程序窗口
                proc.StartInfo.CreateNoWindow = true;
                // 由调用程序获取输出信息
                proc.StartInfo.RedirectStandardOutput = true;
                // 接受来自调用程序的输入信息
                proc.StartInfo.RedirectStandardInput = true;
                // 重定向标准错误输出
                proc.StartInfo.RedirectStandardError = true;

                // elevation-generator.exe--config_path config_path --input_path input_path --output_path output_path --log_path log_path
                proc.StartInfo.Arguments = Config.Arguments;
                    
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    output = proc.StandardOutput.ReadToEnd();
                }
            }
        }
        
        private void Print(List<string> svgFiles)
        {            
            var printer = new ThMultiSvgPrintService(Active.Database,svgFiles);
            printer.Print();
        }

        private void Clear()
        {
            var svgFiles = GetGeneratedSvgFiles();
            Erase(svgFiles);
        }

        private List<string> Sort(List<string> svgFiles)
        {
            //svgFiles已经经过合理性检查
            //C: \Users\XXXX\AppData\Local\Temp\0407-1-Floor_1-Floor_2.svg
            var ifcFileName = Config.IfcFileName.ToUpper();
            return svgFiles.OrderBy(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var restStr = fileName.Substring(ifcFileName.Length + 1);
                var strs = restStr.Split('-');
                return int.Parse(strs[0].Trim());
            }).ToList();
        }

        private void Erase(List<string> svgFiles)
        {
            svgFiles.ForEach(svgFile =>
            {
                var fi = new FileInfo(svgFile);
                if(fi.Exists)
                {
                    fi.Delete();
                }
            });
        }

        private List<string> GetGeneratedSvgFiles()
        {
            var results = new List<string>();
            //文件名，不含后缀
            var ifcFileName = Config.IfcFileName.ToUpper();
            var di = new DirectoryInfo(Config.SvgSavePath);
            foreach(var fileInfo in di.GetFiles())
            {
                if(fileInfo.Extension.ToUpper()==".SVG" &&
                    fileInfo.Name.ToUpper().StartsWith(ifcFileName))
                {
                    results.Add(fileInfo.FullName);
                }
            }
            return results
                .Where(o=>IsValidSvgFile(o))
                .ToList();
        }
        private bool IsValidSvgFile(string svgFilePath)
        {
            //ifcFileName-> 0407-1
            var ifcFileName = Config.IfcFileName.ToUpper();
            // 0407-1-Floor_1-Floor_2
            var fileName = Path.GetFileNameWithoutExtension(svgFilePath); 
            if(!fileName.StartsWith(ifcFileName))
            {
                return false;
            }
            // 1-Floor_1-Floor_2
            var restStr = fileName.Substring(ifcFileName.Length+1);
            var strs = restStr.Split('-');
            if(strs.Length<2)
            {
                return false;
            }
            for(int i=0;i< strs.Length;i++)
            {
                if(i==0)
                {
                    if(!IsInteger(strs[i]))
                    {
                        return false;
                    }
                }
                else
                {
                    if(i==strs.Length-1 && string.IsNullOrEmpty(strs[i]))
                    {
                        continue;
                    }
                    if(!strs[i].ToUpper().StartsWith("FLOOR"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private bool IsInteger(string content)
        {
            string pattern = @"^\s*\d+\s*$";
            return System.Text.RegularExpressions.Regex.IsMatch(content, pattern);
        }
    }
}
