using Autodesk.AutoCAD.Runtime;
using System;
using System.Reflection;

namespace ThMEPArchitecture
{
    public class ThMEPArchitectureApp : IExtensionApplication
    {
        public void Initialize()
        {
            //throw new System.NotImplementedException();
            RedirectAssembly("System.Buffers", new Version(4, 0, 3, 0));
        }
        // from TianHua.AutoCAD.ThCui.ThCuiAssemblyService
        public void RedirectAssembly(string shortName, Version targetVersion)
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
        public void Terminate()
        {
            //throw new System.NotImplementedException();
        }
    }
}
