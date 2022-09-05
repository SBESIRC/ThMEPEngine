using System;
using System.IO;

namespace ThCADExtension
{
    public class ThBIMCommon
    {
        public static string Win64CommonPath()
        {
            return Path.Combine(ContentsPath(), "Win64", "Common");
        }

        public static string StructPlanePath()
        {
            return Path.Combine(SupportPath(), "结构平面图.dwg");
        }

        public static string ArchitectureTemplatePath()
        {
            return Path.Combine(SupportPath(), "建筑平立剖图示意.dwg");
        }

        public static string ArchitectureDoorWindowTemplatePath()
        {
            return Path.Combine(SupportPath(), "建筑门窗填充样式文件.dwg");
        }

        private static string SupportPath()
        {
            return Path.Combine(ContentsPath(), "Support");
        }

        private static string ContentsPath()
        {
            return Path.Combine(RootPath(), "Contents");
        }

        private static string RootPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Autodesk\ApplicationPlugins\ThPlatform3D.bundle");
        }
    }
}
