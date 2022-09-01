using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ThPlatform3DCui
{
    public static class ThCuiAssemblyService
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/assembly/resolve-loads
        public static void RedirectAssembly(string shortName, Version targetVersion)
        {
            Assembly handler(object sender, ResolveEventArgs args)
            {
                // 检查是否已经装载
                var assembly = GetLoadedAssembly(args.Name);
                if (assembly != null)
                {
                    return assembly;
                }

                // 判断是否是指定的模块
                var assemblyRef = new AssemblyName(args.Name);
                if (assemblyRef.Name != shortName)
                {
                    return null;
                }

                // 指定版本
                assemblyRef.Version = targetVersion;

                // 释放事件handler
                AppDomain.CurrentDomain.AssemblyResolve -= handler;

                // 装载模块
                return Assembly.Load(assemblyRef);
            }

            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }

        // https://www.codeproject.com/articles/597398/loading-assemblies-using-assemb
        // https://weblog.west-wind.com/posts/2016/Dec/12/Loading-NET-Assemblies-out-of-Seperate-Folders
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // 检查是否已经装载
            var assembly = GetLoadedAssembly(args.Name);
            if (assembly != null)
            {
                return assembly;
            }

            try
            {
                // 从指定路径中装载
                string filename = args.Name.Split(',')[0] + ".dll".ToLower();
                return Assembly.LoadFrom(Path.Combine(Win64CommonPath(), filename));
            }
            catch
            {
                return null;
            }
        }

        private static Assembly GetLoadedAssembly(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == name);
        }

        public static void SubscribeAssemblyResolve()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static string RootPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Autodesk\ApplicationPlugins\ThPlatform3D.bundle");
        }

        private static string ContentsPath()
        {
            return Path.Combine(RootPath(), "Contents");
        }

        public static string Win64CommonPath()
        {
            return Path.Combine(ContentsPath(), "Win64", "Common");
        }
    }
}