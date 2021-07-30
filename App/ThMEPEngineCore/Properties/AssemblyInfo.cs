﻿using Autodesk.AutoCAD.Runtime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("ThMEPEngineCore")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ThMEPEngineCore")]
[assembly: AssemblyCopyright("Copyright ©  2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("ad98390a-569d-4774-aa3e-d9f00bc84cd6")]

// 程序集的版本信息由下列四个值组成: 
//
//      主版本
//      次版本
//      生成号
//      修订号
//
// 可以指定所有值，也可以使用以下所示的 "*" 预置版本号和修订号
//通过使用 "*"，如下所示:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.4.5.0.1")]
[assembly: AssemblyFileVersion("1.4.5.0.1")]

#if (ACAD2016 || ACAD2018) 
[assembly: CommandClass(typeof(ThMEPEngineCore.ThMEPEngineCoreCGALCmds))]
#endif
[assembly: CommandClass(typeof(ThMEPEngineCore.ThMEPEngineCoreApp))]
[assembly: CommandClass(typeof(ThMEPEngineCore.ThMEPEngineCoreRoomCmds))]
[assembly: CommandClass(typeof(ThMEPEngineCore.ThMEPEngineCoreInternalCmds))]
