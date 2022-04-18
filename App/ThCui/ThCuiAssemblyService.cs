using System;
using System.Reflection;

namespace TianHua.AutoCAD.ThCui
{
    public static class ThCuiAssemblyService
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/assembly/resolve-loads
        public static void RedirectAssembly(string shortName, Version targetVersion)
        {
            Assembly handler(object sender, ResolveEventArgs args)
            {
                // 判断是否是指定的模块
                var assemblyRef = new AssemblyName(args.Name);
                if (assemblyRef.Name != shortName)
                    return null;

                // 指定版本
                assemblyRef.Version = targetVersion;

                // 释放事件handler
                AppDomain.CurrentDomain.AssemblyResolve -= handler;

                // 装载模块
                return Assembly.Load(assemblyRef);
            }

            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }
    }
}
