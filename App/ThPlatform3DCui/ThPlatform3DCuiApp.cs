using AcHelper;
using System.Reflection;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;

namespace ThPlatform3DCui
{
    public class ThPlatform3DCuiApp : IExtensionApplication
    {
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.

            // Load assemblies from the specified path
            // https://docs.microsoft.com/en-us/dotnet/framework/deployment/best-practices-for-assembly-loading
            ThCuiAssemblyService.SubscribeAssemblyResolve();

            // Redirecting Assembly Loads at Runtime
            // https://blog.slaks.net/2013-12-25/redirecting-assembly-loads-at-runtime/
            RedirectAssemblies();
        }

        private void RedirectAssemblies()
        {
            ThCuiAssemblyService.RedirectAssembly("System.Memory", new Version(4, 0, 1, 2));
            ThCuiAssemblyService.RedirectAssembly("System.Runtime.CompilerServices.Unsafe", new Version(6, 0, 0, 0));
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        [CommandMethod("TIANHUACAD", "TH3DVERSION", CommandFlags.Modal)]
        public void TH3DVERSION()
        {
            var asm = Assembly.GetExecutingAssembly();
            Active.Editor.WriteLine("当前版本号：" + asm.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        }
    }
}
