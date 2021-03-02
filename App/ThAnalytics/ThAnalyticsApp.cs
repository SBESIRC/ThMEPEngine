using System;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThAnalytics
{
    public class ThAnalyticsApp : IExtensionApplication
    {
        // 命令“黑名单”，包括：
        //  1.最常用的命令
        //  2.不太重要的天华CAD命令
        // 这里的命令将不会出现在数据分析中
        private readonly ArrayList filters = new ArrayList {
            "QUIT",
            "OPEN",
            "CLOSE",
            "SAVE",
            "NEW",
            "SAVEAS",
            "UNDO",
            "MREDO",
            "LINE",
            "CIRCLE",
            "PLINE",
            "POLYGON",
            "RECTANG",
            "ARC",
            "SPLINE",
            "ELLIPSE",
            "INSERT",
            "BLOCK",
            "HATCH",
            "AcHatchViewWatcher",
            "TEXT",
            "MTEXT",
            "DDEDIT",
            "MTEDIT",
            "BEDIT",
            "DIST",
            "DIMLINEAR",
            "DIMALIGNED",
            "DIMARC",
            "DIMCONTINUE",
            "XREF",
            "-XREF",
            "REFEDIT",
            "LAYER",
            "-LAYER",
            "ERASE",
            "COPY",
            "COPYBASE",
            "MIRROR",
            "OFFSET",
            "MOVE",
            "ROTATE",
            "SCALE",
            "TRIM",
            "EXTEND",
            "BREAK",
            "FILLET",
            "EXPLODE",
            "PROPERTIES",
            "MATCHPROP",
            "REGEN",
            "ZOOM",
            "CUTCLIP",
            "AI_SELALL",
            "FIND",
            "UCS",
            "SETVAR",
            "U",
            "QSAVE",
            "LAYOUT",
            "UNDEFINE",
            "GRIP_STRETCH",
            "COPYCLIP",
            "LAYOUT_CONTROL",
            "PASTECLIP",
            "STRETCH",
            "PLOT",
            "EXTERNALREFERENCES",
            "AUDIT",
            "PURGE",
            "-PURGE",
            "OPTIONS",
            "DRAWORDER",
            "COLOR",
            "LINETYPE",
            "REFCLOSE",
            "MSPACE",
            "PSPACE",
            "GRIP_POPUP",
            "COMMANDLINE",
            "LAYOFF",
            "XOPEN",
            "DIMSTYLE",
            "HELP",
            "LAYON",
            "SELECT",
            "XATTACH",
            "APPLOAD",
            "NETLOAD",
        };

        private readonly Hashtable commandhashtable = new Hashtable();

        public void Initialize()
        {
            ThCybrosService.Instance.Initialize();
            AcadApp.Idle += new EventHandler(Application_OnIdle);
        }

        public void Terminate()
        {
            // unhook DocumentCollection reactors
            AcadApp.DocumentManager.DocumentLockModeChanged -= DocCollEvent_DocumentLockModeChanged_Handler;

            // unhook application event handlers
            //AcadApp.SystemVariableChanged -= AcadApp_SystemVariableChanged;

            //end the user session
            ThCybrosService.Instance.EndSession();
        }

        private void Application_OnIdle(object sender, EventArgs e)
        {
            AcadApp.Idle -= new EventHandler(Application_OnIdle);

            // hook DocumentCollection reactors
            AcadApp.DocumentManager.DocumentLockModeChanged += DocCollEvent_DocumentLockModeChanged_Handler;

            // hook event handlers
            // Fix THAI-868
            //  在某些场景下，捕捉系统变量变化事件，会有严重的效率问题
            //  我们不可能穷举所有可能会导致效率问题的系统变量，而且我们也对系统变量不敢兴趣
            //  这里我们就选择不再捕捉系统变量
            //AcadApp.SystemVariableChanged += AcadApp_SystemVariableChanged;

            //start the user session
            ThCybrosService.Instance.StartSession();
        }

        private void AcadApp_SystemVariableChanged(object sender, SystemVariableChangedEventArgs e)
        {
            ThCybrosService.Instance.RecordSysVerEvent(e.Name, AcadApp.GetSystemVariable(e.Name).ToString());
        }

        private void DocCollEvent_DocumentLockModeChanged_Handler(object sender, DocumentLockModeChangedEventArgs e)
        {
            if (e.GlobalCommandName.StartsWith("#"))
            {
                // Unlock状态，可以看做命令结束状态
                // 剔除掉最前面的“#”
                var cmdName = e.GlobalCommandName.Substring(1);

                // 过滤""命令
                // 通常发生在需要“显式”锁文档的场景中
                if (cmdName == "")
                {
                    return;
                }

                // 过滤“黑名单”里的命令
                if (filters.Contains(cmdName))
                {
                    return;
                }

                // 这里已经无法知道命令结束的状态（正常结束，取消，失败）
                // 但是对于数据分析来说，命令结束的状态并不重要
                // 数据分析关心的是：那些命令被执行过
                if (commandhashtable.ContainsKey(cmdName) & e.CurrentMode == DocumentLockMode.NotLocked)
                {
                    Stopwatch sw = (Stopwatch)commandhashtable[cmdName];
                    // Fix THAI-862
                    //  在某些场景下，捕捉非天华命令（CAD原生命令，其他第三方插件的命令），会有严重的效率问题
                    //  我们不可能穷举所有可能会导致效率问题的非天华命令，而且我们也对非天华命令不敢兴趣
                    //  这里我们就选择不再捕捉非天华命令
                    ThCybrosService.Instance.RecordTHCommandEvent(cmdName, sw.Elapsed.TotalSeconds);
                    commandhashtable.Remove(cmdName);
                }

                // 若有新的用户登陆，则开启Session
                if (cmdName == "THLOGIN")
                {
                    ThCybrosService.Instance.StartSession();
                }
            }
            else
            {
                // Lock状态，可以看做命令开始状态
                var cmdName = e.GlobalCommandName;

                // 过滤""命令
                // 通常发生在需要“显式”锁文档的场景中
                if (cmdName == "")
                {
                    return;
                }

                // 过滤“黑名单”里的命令
                if (filters.Contains(cmdName))
                {
                    return;
                }

                // 天华Lisp命令都是以“TH”开头，后面接不少于2个字母
                if (Regex.Match(cmdName, @"^\([cC]:TH[A-Z]{2,}\)$").Success)
                {
                    var lispCmdName = cmdName.Substring(3, cmdName.Length - 4);
                    ThCybrosService.Instance.RecordTHCommandEvent(lispCmdName, 0);
                    return;
                }

                // 特殊情况（C:THZ0）
                if (Regex.Match(cmdName, @"^\([cC]:THZ0\)$").Success)
                {
                    var lispCmdName = cmdName.Substring(3, cmdName.Length - 4);
                    ThCybrosService.Instance.RecordTHCommandEvent(lispCmdName, 0);
                    return;
                }

                // 记录命令开始
                // 有些CAD内部命令可能并不严格遵循开始-》结束的序列
                // 这些都是怪异的命令，数据分析对这些命令也不感兴趣
                // 这里我们可以忽略这些“异常”命令
                if (commandhashtable.ContainsKey(cmdName))
                {
                    commandhashtable.Remove(cmdName);
                }
                commandhashtable.Add(cmdName, Stopwatch.StartNew());

                // 若用户准备登出，则结束Session
                if (cmdName == "THLOGOUT")
                {
                    ThCybrosService.Instance.EndSession();
                }
            }
        }
    }
}
