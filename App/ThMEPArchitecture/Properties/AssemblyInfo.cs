﻿using Autodesk.AutoCAD.Runtime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的一般信息由以下
// 控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("ThMEPArchitecture")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ThMEPArchitecture")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 会使此程序集中的类型
//对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型
//请将此类型的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("65557b89-67b5-4776-a2d5-68da51d5296a")]

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
[assembly: AssemblyVersion("0.3.4.7")]
[assembly: AssemblyFileVersion("0.3.4.7")]

[assembly: CommandClass(typeof(ThMEPArchitecture.ThParkingStallArrangement))]
[assembly: CommandClass(typeof(ThMEPArchitecture.ParkingStallArrangement.WithoutSegLineCmd))]
[assembly: CommandClass(typeof(ThMEPArchitecture.ThParkingStallArrangementByFixedLines))]
[assembly: CommandClass(typeof(ThMEPArchitecture.PartitionLayout.TestCommond))]
[assembly: CommandClass(typeof(ThMEPArchitecture.PartitionLayout.PartitionLayoutMultiThreadedTest))]
[assembly: CommandClass(typeof(ThMEPArchitecture.CreateAllSeglinesCmd))]//生成所有分割线
[assembly: CommandClass(typeof(ThMEPArchitecture.ParkingStallArrangement.ThBreakSegLinesCmd))]
[assembly: CommandClass(typeof(ThMEPArchitecture.ParkingStallArrangement.ThParkingStallPreprocessCmd))]//预处理
[assembly: CommandClass(typeof(ThMEPArchitecture.PartitionLayout.MultiProcessTestCommand))]
[assembly: CommandClass(typeof(ThMEPArchitecture.MultiProcess.ThMPArrangementCmd))]
[assembly: CommandClass(typeof(ThMEPArchitecture.ViewModel.CommandSetParamCmd))]
