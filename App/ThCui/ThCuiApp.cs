using System;
using AcHelper;
using System.IO;
using ThMEPIdentity;
using ThCADExtension;
using System.Reflection;
using System.Configuration;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.AutoCAD.ThCui
{
    public class ThCuiApp : IExtensionApplication
    {
        ThPartialCui partialCui = new ThPartialCui();

        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.

            //  在装载模块时主动装载局部CUIX文件
            LoadPartialCui(true);

            ////注册命令
            //RegisterCommands();

            ////定制Preferences
            //OverridePreferences(true);

            ////复写打印机配置文件到Roaming目录
            //OverwritePlotConfigurations();

            // CUI界面更新
            AcadApp.Idle += Application_OnIdle_Ribbon;
            //AcadApp.Idle += Application_OnIdle_Menubar;
            //AcadApp.Idle += Application_OnIdle_Toolbar;

            ////注册RibbonPaletteSet事件
            //if (RibbonServices.RibbonPaletteSet == null)
            //{
            //    // Ribbon未创建
            //    RibbonServices.RibbonPaletteSetCreated += RibbonPaletteSetCreated;
            //}
            //else
            //{
            //    // Ribbon已创建
            //    RibbonServices.RibbonPaletteSet.StateChanged += RibbonPaletteSet_StateChanged;
            //}

            //注册DocumentCollection事件
            AcadApp.DocumentManager.DocumentLockModeChanged += DocCollEvent_DocumentLockModeChanged_Handler;
            ////注册SystemVariableChanged 事件
            //AcadApp.SystemVariableChanged += SystemVariableChangedHandler;

            // Load assemblies from the specified path
            // https://docs.microsoft.com/en-us/dotnet/framework/deployment/best-practices-for-assembly-loading
            ThCuiAssemblyService.SubscribeAssemblyResolve();

            // Redirecting Assembly Loads at Runtime
            // https://blog.slaks.net/2013-12-25/redirecting-assembly-loads-at-runtime/
            RedirectAssemblies();
        }

        private void RedirectAssemblies()
        {
            ThCuiAssemblyService.RedirectAssembly("System.Buffers", new Version(4, 0, 3, 0));
            ThCuiAssemblyService.RedirectAssembly("ICSharpCode.SharpZipLib", new Version(1, 3, 2, 10));
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.

            //  在卸载模块时主动卸载局部CUIX文件
            LoadPartialCui(false);

            ////恢复Preferences
            //OverridePreferences(false);

            ////反注册RibbonPaletteSet事件
            //if (RibbonServices.RibbonPaletteSet == null)
            //{
            //    // Ribbon未创建状态
            //    // 反注册Ribbon创建事件
            //    RibbonServices.RibbonPaletteSetCreated -= RibbonPaletteSetCreated;
            //}
            //else
            //{
            //    // Ribbon已创建
            //    // 反注册Ribbon状态改变事件
            //    RibbonServices.RibbonPaletteSet.StateChanged -= RibbonPaletteSet_StateChanged;
            //}

            //反注册DocumentCollection事件
            AcadApp.DocumentManager.DocumentLockModeChanged -= DocCollEvent_DocumentLockModeChanged_Handler;
            ////反注册SystemVariableChanged 事件
            //AcadApp.SystemVariableChanged -= SystemVariableChangedHandler;
        }

#if DEBUG
        [CommandMethod("TIANHUACAD", "THDUMPCUI", CommandFlags.Modal)]
        public void ThDumpCui()
        {
            CreatePartialCui();
        }
#endif

        [CommandMethod("TIANHUACAD", "THMEPVERSION", CommandFlags.Modal)]
        public void ThMEPVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            Active.Editor.WriteLine("当前版本号：" + asm.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        }

        [CommandMethod("TIANHUACAD", "THMEPPROFILE", CommandFlags.Modal)]
        public void ThMEPProfile()
        {
            // 指定专业
            PromptKeywordOptions keywordOptions = new PromptKeywordOptions("\n请指定专业：")
            {
                AllowNone = true
            };
            keywordOptions.Keywords.Add("ARCHITECTURE", "ARCHITECTURE", "建筑(A)");
            keywordOptions.Keywords.Add("STRUCTURE", "STRUCTURE", "结构(S)");
            keywordOptions.Keywords.Add("HAVC", "HAVC", "暖通(H)");
            keywordOptions.Keywords.Add("ELECTRICAL", "ELECTRICAL", "电气(E)");
            keywordOptions.Keywords.Add("WSS", "WSS", "给排水(W)");
            keywordOptions.Keywords.Default = "ARCHITECTURE";
            PromptResult result = Active.Editor.GetKeywords(keywordOptions);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            switch (result.StringResult)
            {
                case "ARCHITECTURE":
                    {
                        ThRibbonUtils.ConfigPanelsWithProfile("A");
                    }
                    break;
                case "HAVC":
                    {
                        ThRibbonUtils.ConfigPanelsWithProfile("H");
                    }
                    break;
                case "ELECTRICAL":
                    {
                        ThRibbonUtils.ConfigPanelsWithProfile("E");
                    }
                    break;
                case "WSS":
                    {
                        ThRibbonUtils.ConfigPanelsWithProfile("W");
                    }
                    break;
                case "STRUCTURE":
                    {
                        ThRibbonUtils.ConfigPanelsWithProfile("S");
                    }
                    break;
                default:
                    return;
            }
        }

        [CommandMethod("TIANHUACAD", "THMEPUSERCONFIG", CommandFlags.Modal)]
        public void ThMEPUserConfig()
        {
            Active.Editor.WriteLine(string.Format("配置文件路径：{0}", UserConfigPath()));
        }

        private string UserConfigPath()
        {
            // https://stackoverflow.com/questions/982354/where-are-the-properties-settings-default-stored
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
        }

        private void LoadPartialCui(bool bLoad = true)
        {
            if (bLoad)
            {
                partialCui.Load(Path.Combine(ThCADCommon.ResourcesPath(), ThCADCommon.CuixFile), ThCADCommon.CuixMenuGroup);
            }
            else
            {
                partialCui.UnLoad(Path.Combine(ThCADCommon.ResourcesPath(), ThCADCommon.CuixFile), ThCADCommon.CuixMenuGroup);
            }
        }

        private void CreatePartialCui()
        {
            partialCui.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ThCADCommon.CuixFile), ThCADCommon.CuixMenuGroup);
        }

        private void Application_OnIdle_Cmd_Veto(object sender, EventArgs e)
        {
            AcadApp.Idle -= Application_OnIdle_Cmd_Veto;

            // 显示登陆提示
            Active.Editor.WriteLine("未登录天华三维协同或此功能未开放，请登录三维协同平台或联系管理员。");
        }

        private void Application_OnIdle_Ribbon(object sender, EventArgs e)
        {
            // 若是新版本，则主动运行RIBBON命令显示RIBBON
            var version = GetCurrentVersion();
            if (version > ReadVersion())
            {
                SaveVersion(version);
                Active.Document.SendStringToExecute("_.RIBBON ", true, false, false);
            }

            // 使用AutoCAD Windows runtime API来配置自定义Tab中的Panels
            // 需要保证Ribbon自定义Tab是存在的，并且自定义Tab中的Panels也是存在的。
            if (ThRibbonUtils.Tab != null && ThRibbonUtils.Tab.Panels.Count != 0)
            {
                // 配置完成后就不需要Idle事件
                AcadApp.Idle -= Application_OnIdle_Ribbon;

                // 更新Ribbon
                UpdateRibbonUserInterface();
            }
        }

        private void SaveVersion(Version version)
        {
            Properties.Settings.Default.Version = version.ToString();
            Properties.Settings.Default.Save();
        }

        private Version ReadVersion()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.Version))
            {
                return new Version();
            }
            else
            {
                return new Version(Properties.Settings.Default.Version);
            }
        }

        private Version GetCurrentVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            return new Version(asm.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        }

        //private void Application_OnIdle_Menubar(object sender, EventArgs e)
        //{
        //    if (ThMenuBarUtils.PopupMenu != null
        //    {
        //        // 配置完成后就不需要Idle事件
        //        AcadApp.Idle -= Application_OnIdle_Menubar;

        //        // 更新Menubar
        //        UpdateMenubarUserInterface();
        //    }
        //}

        //private void Application_OnIdle_Toolbar(object sender, EventArgs e)
        //{
        //    if (ThToolbarUtils.Toolbars != null)
        //    {
        //        // 配置完成后就不需要Idle事件
        //        AcadApp.Idle -= Application_OnIdle_Toolbar;

        //        // 更新Toolbar
        //        UpdateToolbarUserInterface();
        //    }
        //}

        /// <summary>
        /// 更新当前的Ribbon界面
        /// </summary>
        private void UpdateRibbonUserInterface()
        {
            // 根据当前的登录信息配置Panels
            if (ThAcsSystemService.Instance.IsLogged)
            {
                // 根据登录用户的专业自动配置Panels
                var major = ThAcsSystemService.Instance.MajorCode;
                switch (major)
                {
                    case "A":
                        ThRibbonUtils.ConfigPanelsWithProfile("A");
                        break;
                    case "S":
                        ThRibbonUtils.ConfigPanelsWithProfile("S");
                        break;
                    case "H":
                        ThRibbonUtils.ConfigPanelsWithProfile("H");
                        break;
                    case "E":
                        ThRibbonUtils.ConfigPanelsWithProfile("E");
                        break;
                    case "W":
                        ThRibbonUtils.ConfigPanelsWithProfile("W");
                        break;
                    default:
                        ThRibbonUtils.ConfigPanelsWithProfile("W");
                        break;
                }
            }
            else
            {
                // 未登录，使用默认专业配置Panels
                ThRibbonUtils.ConfigPanelsWithProfile("W");
            }
        }

        //private void UpdateToolbarUserInterface()
        //{
        //    // 根据当前的登录信息配置Toolbars
        //    ThToolbarUtils.ConfigToolbarsWithCurrentUser();
        //}

        //private void UpdateMenubarUserInterface()
        //{
        //    // 根据当前的Profile配置Menubar
        //    ThMenuBarUtils.ConfigMenubarWithCurrentProfile();

        //    // 根据当前的登录信息配置Menubar
        //    ThMenuBarUtils.ConfigMenubarWithCurrentUser();
        //}

        //private void Application_OnIdle_RibbonPaletteSet(object sender, EventArgs e)
        //{
        //    if (RibbonServices.RibbonPaletteSet != null)
        //    {
        //        AcadApp.Idle -= Application_OnIdle_RibbonPaletteSet;
        //        RibbonServices.RibbonPaletteSet.StateChanged -= RibbonPaletteSet_StateChanged;
        //    }
        //}

        //private void RibbonPaletteSetCreated(object sender, EventArgs e)
        //{
        //    // 通过捕捉Ribbon创建事件，在Ribbon创建完成后监听Ribbon状态改变事件
        //    // 在这一刻，Ribbon未必完全创建完毕，这里通过Idle事件故意延后一段时间
        //    AcadApp.Idle += Application_OnIdle_RibbonPaletteSet;
        //    RibbonServices.RibbonPaletteSetCreated -= RibbonPaletteSetCreated;
        //}

        //private void RibbonPaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        //{
        //    if (e.NewState == StateEventIndex.Show)
        //    {
        //        // 使用AutoCAD Windows runtime API来配置自定义Tab中的Panels
        //        // 需要保证Ribbon自定义Tab是存在的，并且自定义Tab中的Panels也是存在的。
        //        if (ThRibbonUtils.Tab != null && ThRibbonUtils.Tab.Panels.Count != 0)
        //        {
        //            ThRibbonUtils.ConfigPanelsWithCurrentUser();
        //        }
        //    }
        //}

        private void DocCollEvent_DocumentLockModeChanged_Handler(object sender, DocumentLockModeChangedEventArgs e)
        {
            if (!e.GlobalCommandName.StartsWith("#"))
            {
                // Lock状态，可以看做命令开始状态
                var cmdName = e.GlobalCommandName;

                // 过滤""命令
                // 通常发生在需要“显式”锁文档的场景中
                if (cmdName == "")
                {
                    return;
                }

                //// 天华Lisp命令都是以“TH”开头
                //// 特殊情况（C:THZ0）
                //bool bVeto = false;
                //if (Regex.Match(cmdName, @"^\([cC]:THZ0\)$").Success)
                //{
                //    bVeto = !ThAcsSystemService.Instance.IsLogged;
                //}

                //// 正常情况（C:THXXX）
                //if (Regex.Match(cmdName, @"^\([cC]:TH[A-Z]{3,}\)$").Success)
                //{
                //    bVeto = !ThAcsSystemService.Instance.IsLogged;
                //}

                // 天华ARX命令
                bool bVeto = false;
                if (ThMEPCmdService.Instance.IsTHCommand(cmdName))
                {
                    // 在未登陆的情况下，不能运行
                    bVeto = !ThAcsSystemService.Instance.IsLogged;

                    // 授权用户
                    if (!bVeto)
                    {
                        var eid = ThAcsSystemService.Instance.EmployeeId;
                        bVeto = !ThMEPCmdService.Instance.IsAuthorizedTHCommand(cmdName, eid);
                    }
                }

                // Veto
                if (bVeto)
                {
                    e.Veto();
                    AcadApp.Idle += Application_OnIdle_Cmd_Veto;
                }
            }
        }

        //private void SystemVariableChangedHandler(object sender, SystemVariableChangedEventArgs e)
        //{
        //    if (e.Name == "WSCURRENT")
        //    {
        //        // CUI界面更新
        //        AcadApp.Idle += Application_OnIdle_Ribbon;
        //    }
        //}

        //        public void RegisterCommands()
        //        {
        //            ////注册登录命令
        //            //Utils.AddCommand(
        //            //    ThCuiCommon.CMD_GROUPNAME, 
        //            //    ThCuiCommon.CMD_THLOGIN_GLOBAL_NAME, 
        //            //    ThCuiCommon.CMD_THLOGIN_GLOBAL_NAME, 
        //            //    CommandFlags.Modal, 
        //            //    new CommandCallback(OnLogIn));
        //            ////注册退出命令
        //            //Utils.AddCommand(
        //            //    ThCuiCommon.CMD_GROUPNAME,
        //            //    ThCuiCommon.CMD_THLOGOUT_GLOBAL_NAME,
        //            //    ThCuiCommon.CMD_THLOGOUT_GLOBAL_NAME, 
        //            //    CommandFlags.Modal, 
        //            //    new CommandCallback(OnLogOut));

        //#if DEBUG
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THDUMPCUI_GLOBAL_NAME, 
        //                ThCuiCommon.CMD_THDUMPCUI_GLOBAL_NAME, 
        //                CommandFlags.Modal, 
        //                new CommandCallback(OnDumpCui));
        //#endif

        //            //注册帮组和命令
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THHLP_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THHLP_GLOBAL_NAME, 
        //                CommandFlags.Modal, 
        //                new CommandCallback(OnHelp));

        //            //天华反馈
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THFBK_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THFBK_GLOBAL_NAME,
        //                CommandFlags.Modal,
        //                new CommandCallback(OnFeedback));

        //            //注册工具选项板开关命令
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THBLI_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THBLI_GLOBAL_NAME, 
        //                CommandFlags.Modal, 
        //                new CommandCallback(OnShowToolPalette));

        //            //注册下载T20天正插件命令
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME, 
        //                ThCuiCommon.CMD_THT20PLUGINV4_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THT20PLUGINV4_GLOBAL_NAME, 
        //                CommandFlags.Modal, 
        //                new CommandCallback(DownloadT20PlugInV4));
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THT20PLUGINV5_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THT20PLUGINV5_GLOBAL_NAME, 
        //                CommandFlags.Modal, 
        //                new CommandCallback(DownloadT20PlugInV5));

        //            //专业切换
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THPROFILE_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THPROFILE_GLOBAL_NAME,
        //                CommandFlags.Modal,
        //                new CommandCallback(OnSwitchProfile));

        //            //天华PURGE
        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THPURGE_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THPURGE_GLOBAL_NAME,
        //                CommandFlags.Modal,
        //                new CommandCallback(ThPurge));

        //            Utils.AddCommand(
        //                ThCuiCommon.CMD_GROUPNAME,
        //                ThCuiCommon.CMD_THRESETCUI_GLOBAL_NAME,
        //                ThCuiCommon.CMD_THRESETCUI_GLOBAL_NAME,
        //                CommandFlags.Modal,
        //                new CommandCallback(OnResetCui));
        //        }

        //        public void UnregisterCommands()
        //        {
        //            //Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THLOGIN_GLOBAL_NAME);
        //            //Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THLOGOUT_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THHLP_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THBLI_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THFBK_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THT20PLUGINV4_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THT20PLUGINV5_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THPROFILE_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THPURGE_GLOBAL_NAME);
        //            Utils.RemoveCommand(ThCuiCommon.CMD_GROUPNAME, ThCuiCommon.CMD_THRESETCUI_GLOBAL_NAME);
        //        }

        //        private void OverwritePlotConfigurations()
        //        { 
        //            if (Properties.Settings.Default.OverwritePlotConfigurations == true)
        //            {
        //                return;
        //            }

        //            string roaming = (string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX");
        //            if (!string.IsNullOrEmpty(roaming))
        //            {
        //                var plotters = new DirectoryInfo(ThCADCommon.PlottersPath());
        //                foreach(var file in plotters.GetFiles("*.pc3", SearchOption.TopDirectoryOnly))
        //                {
        //                    file.CopyTo(Path.Combine(roaming, "Plotters", file.Name), true);
        //                }

        //                var printerDesc = new DirectoryInfo(ThCADCommon.PrinterDescPath());
        //                foreach(var file in printerDesc.GetFiles("*.pmp", SearchOption.TopDirectoryOnly))
        //                {
        //                    file.CopyTo(Path.Combine(roaming, "Plotters", "PMP Files", file.Name), true);
        //                }

        //                var printerStyleSheet = new DirectoryInfo(ThCADCommon.PrinterStyleSheetPath());
        //                foreach(var file in printerStyleSheet.GetFiles("*.ctb", SearchOption.TopDirectoryOnly))
        //                {
        //                    file.CopyTo(Path.Combine(roaming, "Plotters", "Plot Styles", file.Name), true);
        //                }

        //                //
        //                Properties.Settings.Default.OverwritePlotConfigurations = true;
        //                Properties.Settings.Default.Save();
        //            }
        //        }

        //        private void OverridePreferences(bool bOverride = true)
        //        {
        //            OverrideSupportPathPreferences(bOverride);
        //            OverridePrinterPathPreferences(bOverride);
        //        }

        //        private void OverridePrinterPathPreferences(bool bOverride = true)
        //        {
        //#if ACAD_ABOVE_2014
        //            var path = ThCADCommon.PlottersPath();
        //            var printerConfigPath = new PrinterConfigPath(AcadApp.Preferences);
        //            if (bOverride && !printerConfigPath.Contains(path))
        //            {
        //                printerConfigPath.Add(path);
        //                printerConfigPath.SaveChanges();
        //            }
        //            else if (!bOverride && printerConfigPath.Contains(path))
        //            {
        //                printerConfigPath.Remove(path);
        //                printerConfigPath.SaveChanges();
        //            }

        //            path = ThCADCommon.PrinterDescPath();
        //            var printerDescPath = new PrinterDescPath(AcadApp.Preferences);
        //            if (bOverride && !printerDescPath.Contains(path))
        //            {
        //                printerDescPath.Add(path);
        //                printerDescPath.SaveChanges();
        //            }
        //            else if (!bOverride && printerDescPath.Contains(path))
        //            {
        //                printerDescPath.Remove(path);
        //                printerDescPath.SaveChanges();
        //            }

        //            path = ThCADCommon.PrinterStyleSheetPath();
        //            var printerStyleSheetPath = new PrinterStyleSheetPath(AcadApp.Preferences);
        //            if (bOverride && !printerStyleSheetPath.Contains(path))
        //            {
        //                printerStyleSheetPath.Add(path);
        //                printerStyleSheetPath.SaveChanges();
        //            }
        //            else if (!bOverride && printerStyleSheetPath.Contains(path))
        //            {
        //                printerStyleSheetPath.Remove(path);
        //                printerStyleSheetPath.SaveChanges();
        //            }
        //#else
        //            // 在CAD 2012和2014中，通过上面修改Preferences搜索路径的方式不能成功
        //            // 这里采用一个变通办法，即把相应的文件拷贝到Roamable路径中 
        //            var roamableRootPath = (string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX");
        //            var roamableRootPlottersPath = Path.Combine(roamableRootPath, "Plotters");
        //            foreach (var file in Directory.GetFiles(ThCADCommon.PlottersPath(), "*.pc3"))
        //            {
        //                try
        //                {
        //                    var plotter = Path.GetFileName(file);
        //                    var plotterPath = Path.Combine(roamableRootPlottersPath, plotter);
        //                    if (bOverride)
        //                    {
        //                        File.Copy(file, plotterPath, true);
        //                    }
        //                }
        //                catch
        //                {
        //                    //
        //                }
        //            }

        //            foreach(var file in Directory.GetFiles(ThCADCommon.PrinterDescPath(), "*.pmp"))
        //            {
        //                try
        //                {
        //                    var printerDesc = Path.GetFileName(file);
        //                    var printerDescPath = Path.Combine(roamableRootPlottersPath, "PMP Files", printerDesc);
        //                    if (bOverride)
        //                    {
        //                        File.Copy(file, printerDescPath, true);
        //                    }
        //                }
        //                catch
        //                {
        //                    //
        //                }
        //            }

        //            foreach(var file in Directory.GetFiles(ThCADCommon.PrinterStyleSheetPath(), "*.ctb"))
        //            {
        //                try
        //                {
        //                    var printerStyleSheet = Path.GetFileName(file);
        //                    var printerStyleSheetPath = Path.Combine(roamableRootPlottersPath, "Plot Styles", printerStyleSheet);
        //                    if (bOverride && !File.Exists(printerStyleSheetPath))
        //                    {
        //                        File.Copy(file, printerStyleSheetPath);
        //                    }
        //                }
        //                catch
        //                {
        //                    //
        //                }
        //            }
        //#endif
        //        }

        //        private void OverrideSupportPathPreferences(bool bOverride = true)
        //        {
        //            // ATC文件中的"SourceFile"指向一个图纸，这个图纸提供了ATC文件中的块定义
        //            // 为了能正确的正确的部署ATC文件以及其引用的图纸，不建议为"SourceFile"指定一个绝对路径
        //            // 一个更好的做法是：
        //            //  1. 在"SourceFile"中只指定图纸名
        //            //  2. 将图纸的路径添加到"支持文件搜索路径"中
        //            // 剩下的就交给CAD去根据"支持文件搜索路径"指定的路径来寻找图纸
        //            // https://knowledge.autodesk.com/support/autocad/troubleshooting/caas/sfdcarticles/sfdcarticles/Using-Source-File-field-for-tool-palette-block-data.html
        //            var supportPath = new SupportPath(AcadApp.Preferences);
        //            foreach (var item in new string[] { ThCuiCommon.PATH_ELECTRICAL, ThCuiCommon.PATH_WSS, ThCuiCommon.PATH_HAVC })
        //            {
        //                var path = Path.Combine(ThCADCommon.ToolPalettePath(), item);
        //                if (bOverride && !supportPath.Contains(path))
        //                {
        //                    supportPath.Add(path);
        //                }
        //                else if (!bOverride && supportPath.Contains(path))
        //                {
        //                    supportPath.Remove(path);
        //                }
        //            }
        //            supportPath.SaveChanges();
        //        }

        //private void OnLogIn()
        //{
        //    using (var dlg = new ThLoginDlg())
        //    {
        //        if (AcadApp.ShowModalDialog(dlg) != DialogResult.OK)
        //        {
        //            return;
        //        }
        //    }

        //    // 更新Ribbon
        //    if (ThIdentityService.IsLogged())
        //    {
        //        ThRibbonUtils.OpenAllPanels();
        //        ThToolbarUtils.OpenAllToolbars();
        //        ThMenuBarUtils.EnableMenuItems();
        //    }

        //    // 根据当前的Profile配置Panels
        //    ThRibbonUtils.ConfigPanelsWithCurrentProfile();
        //    // 根据当前的Profile配置Toolbars
        //    ThToolbarUtils.ConfigToolbarsWithCurrentProfile();
        //    // 根据当前的Profile配置Menubar
        //    ThMenuBarUtils.ConfigMenubarWithCurrentProfile();
        //    // 根据当前的Profile配置ToolPalette
        //    ThToolPaletteUtils.ConfigToolPaletteWithCurrentProfile();
        //}

        //private void OnLogOut()
        //{
        //    ThIdentityService.Logout();
        //    ThCuiProfileManager.Instance.Reset();

        //    // 更新Ribbon
        //    if (!ThIdentityService.IsLogged())
        //    {
        //        ThRibbonUtils.CloseAllPanels();
        //        ThToolbarUtils.CloseAllToolbars();
        //        ThMenuBarUtils.DisableMenuItems();
        //        ThToolPaletteUtils.RemoveAllToolPalettes();
        //    }
        //}

        //        private void OnHelp()
        //        {
        //            Process.Start(ThCADCommon.OnlineHelpUrl);
        //        }

        //        private void OnFeedback()
        //        {
        //            using (var dlg = new ThFeedBackDlg())
        //            {
        //                AcadApp.ShowModalDialog(dlg);
        //            }
        //        }

        //        private void OnResetCui()
        //        {
        //            // 先卸载ThCAD
        //            Active.Editor.Cuiunload();

        //            // 删除Roaming目录中的CUIX文件缓存
        //            string roaming = (string)AcadApp.GetSystemVariable("ROAMABLEROOTPREFIX");
        //            if (!string.IsNullOrEmpty(roaming))
        //            {
        //                var filter = string.Format("{0}.*", ThCADCommon.CuixMenuGroup);
        //                var support = new DirectoryInfo(Path.Combine(roaming, "Support"));
        //                foreach (var file in support.GetFiles(filter, SearchOption.TopDirectoryOnly))
        //                {
        //                    file.Delete();
        //                }
        //            }
        //        }

        //        private void OnSwitchProfile()
        //        {
        //            // 指定专业
        //            PromptKeywordOptions keywordOptions = new PromptKeywordOptions("\n请指定专业：")
        //            {
        //                AllowNone = true
        //            };
        //            keywordOptions.Keywords.Add("ARCHITECTURE", "ARCHITECTURE", "建筑(A)");
        //            keywordOptions.Keywords.Add("STRUCTURE", "STRUCTURE", "结构(S)");
        //            keywordOptions.Keywords.Add("HAVC", "HAVC", "暖通(H)");
        //            keywordOptions.Keywords.Add("ELECTRICAL", "ELECTRICAL", "电气(E)");
        //            keywordOptions.Keywords.Add("WATER", "WATER", "给排水(W)");
        //            keywordOptions.Keywords.Add("PROJECT", "PROJECT", "方案(P)");
        //            keywordOptions.Keywords.Default = "ARCHITECTURE";
        //            PromptResult result = Active.Editor.GetKeywords(keywordOptions);
        //            if (result.Status != PromptStatus.OK)
        //            {
        //                return;
        //            }

        //            switch(result.StringResult)
        //            {
        //                case "ARCHITECTURE":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.CONSTRUCTION;
        //                    }
        //                    break;
        //                case "STRUCTURE":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.STRUCTURE;
        //                    }
        //                    break;
        //                case "HAVC":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.HAVC;
        //                    }
        //                    break;
        //                case "ELECTRICAL":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.ELECTRICAL;
        //                    }
        //                    break;
        //                case "WATER":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.WSS;
        //                    }
        //                    break;
        //                case "PROJECT":
        //                    {
        //                        ThCuiProfileManager.Instance.CurrentProfile = Profile.ARCHITECTURE;
        //                    }
        //                    break;
        //                default:
        //                    return;
        //            }

        //            ThRibbonUtils.ConfigPanelsWithCurrentProfile();
        //            ThMenuBarUtils.ConfigMenubarWithCurrentProfile();
        //            ThToolbarUtils.ConfigToolbarsWithCurrentProfile();
        //            ThToolPaletteUtils.ConfigToolPaletteWithCurrentProfile();
        //        }


        //        // DGN清理
        //        // 从AutoCAD 2016开始，“PURGE”命令可以实现“DGNPURGE”的功能
        //        // 这里直接将“DGNPURGE”切换到“PURGE”命令
        //        private void ThPurge()
        //        {
        //#if ACAD_ABOVE_2014
        //            Active.Document.SendStringToExecute("_.PURGE ", true, false, true);
        //#else
        //            Active.Document.SendStringToExecute("_.DGNPURGE ", true, false, true);
        //#endif
        //        }

        //        private void OnShowToolPalette()
        //        {
        //            Active.Document.SendStringToExecute("_.TOOLPALETTES ", true, false, true);
        //        }

        //        /// <summary>
        //        /// 下载T20天正插件
        //        /// </summary>
        //        public void DownloadT20PlugInV4()
        //        {
        //            DownloadT20PlugInWorker("T20-PlugIn", "V4.0");
        //        }

        //        public void DownloadT20PlugInV5()
        //        {
        //            DownloadT20PlugInWorker("T20-PlugIn", "V5.0");
        //        }

        //        private void DownloadT20PlugInWorker(string appName, string appVersion)
        //        {
        //            string fileName = string.Format("{0} {1}.exe", appName, appVersion);
        //            Uri downloadUri = new Uri(string.Join("/", ThCADCommon.ServerUrl, "Release", fileName));
        //            string downloadFile = Path.Combine(Path.GetTempPath(), fileName);

        //            try
        //            {
        //                var wc = new WebClient();
        //                var dlg = new ThT20PlugInDownloadDlg(appName, appVersion);
        //                dlg.FormClosing += (o, e) =>
        //                {
        //                    // 在下载中关闭进度条窗口，取消下载
        //                    if ((wc != null) && wc.IsBusy)
        //                    {
        //                        wc.CancelAsync();
        //                    }
        //                };
        //                wc.DownloadProgressChanged += (o, e) =>
        //                {
        //                    // 在下载中更新进度条窗口
        //                    dlg.OnDownloadProgressChanged(o, e);
        //                };
        //                wc.DownloadFileCompleted += (o, e) => {
        //                    // 取消下载
        //                    if (e.Cancelled)
        //                    {
        //                        return;
        //                    }
        //                    // 下载完成
        //                    if (e.Error == null)
        //                    {
        //                        // 关闭进度条窗口
        //                        dlg.Close();

        //                        // 执行下载安装程序
        //                        if (File.Exists(downloadFile))
        //                        {
        //                            Process.Start(downloadFile);
        //                        }
        //                    }
        //                };

        //                AcadApp.ShowModelessDialog(dlg);
        //                wc.DownloadFileAsync(downloadUri, downloadFile);
        //            }
        //            catch
        //            {
        //            }
        //        }
    }
}
