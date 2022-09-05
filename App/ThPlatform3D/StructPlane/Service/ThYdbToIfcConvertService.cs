using System.IO;
using System.Reflection;
using System.Diagnostics;
using ThCADExtension;

namespace ThPlatform3D.StructPlane.Service
{
    public class ThYdbToIfcConvertService
    {
        /// <summary>
        /// elevation-generator.exe的完整路径
        /// </summary>
        public string ExeFilePath
        {
            get
            {
                var exePath = GetWin64CommonPath();
                return Path.Combine(exePath, "ydb2ifc.exe");
            }
        }

        private string GetWin64CommonPath()
        {
            return ThBIMCommon.Win64CommonPath();
        }

        public string Convert(string ydbFile)
        {
            var ifcFileName = "";
            if(!string.IsNullOrEmpty(ydbFile) && IsExist(ydbFile) && IsExist(ExeFilePath))
            {
                // 获取Ifc路径
                var ydbPath = Path.GetDirectoryName(ydbFile);
                var ydbFileName = Path.GetFileNameWithoutExtension(ydbFile);
                var outputIfcName = ydbFileName + ".ifc";
                ifcFileName = Path.Combine(ydbPath, outputIfcName);
                ToIfc(ydbFile, ifcFileName);
                if (!IsExist(ifcFileName))
                {
                    ifcFileName = "";
                }
            }
            return ifcFileName;
        }

        private bool IsExist(string fileName)
        {
            return File.Exists(fileName);
        }

        private void ToIfc(string ydbFile,string outIfcName)
        {
            using (var proc = new Process())
            {
                object output = null;
                proc.StartInfo.FileName = ExeFilePath;
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

                proc.StartInfo.Arguments = ydbFile + " "+ outIfcName;

                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    output = proc.StandardOutput.ReadToEnd();
                }
            }
        }
    }
}
