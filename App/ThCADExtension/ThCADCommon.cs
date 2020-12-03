﻿using System;
using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Geometry;

namespace ThCADExtension
{
    public class ThCADCommon
    {
        // Tolerance
        // Tolerance.Global默认值：new Tolerance(1e-10, 1e-12)
        public static Tolerance Global_Tolerance = new Tolerance(1e-4, 1e-4);

        // CUIX 
        public static readonly string CuixFile = "ThMEP.cuix";
        public static readonly string CuixResDll = "ThMEP.dll";
        public static readonly string CuixMenuGroup = "ThMEP";

        // Ribbon
        public static readonly string RibbonTabName = "ThMEPRibbonBar";
        public static readonly string RibbonTabTitle = "天华机电工具集";
        public static readonly string OnlineHelpUrl = @"http://info.thape.com.cn/AI/thmep/help.html";

        // RegAppName
        public static readonly string RegAppName = "THMEP";

        // DxfName
        public static readonly string DxfName_Text      = "TEXT";
        public static readonly string DxfName_MText     = "MTEXT";
        public static readonly string DxfName_Leader    = "LEADER";
        public static readonly string DxfName_Insert    = "INSERT";
        public static readonly string DxfName_Dimension = "DIMENSION";
        public static readonly string DxfName_Attdef    = "ATTDEF";

        // Tangent DxfName
        public static readonly string DxfName_TCH_Text = "TCH_TEXT";
        public static readonly string DxfName_TCH_MText = "TCH_MTEXT";
        public static readonly string DxfName_TCH_Dimension2 = "TCH_DIMENSION2";
        public static readonly string DxfName_TCH_Axis_Label = "TCH_AXIS_LABEL";
        public static readonly string DxfName_TCH_Space = "TCH_SPACE";
        public static readonly string DxfName_TCH_RadiusDim = "TCH_RADIUSDIM";
        public static readonly string DxfName_TCH_Coord = "TCH_COORD";
        public static readonly string DxfName_TCH_Arrow = "TCH_ARROW";
        public static readonly string DxfName_TCH_MLeader = "TCH_MULTILEADER";
        public static readonly string DxfName_TCH_IndexPointer = "TCH_INDEXPOINTER";
        public static readonly string DxfName_TCH_Composing = "TCH_COMPOSING";
        public static readonly string DxfName_TCH_Symb_Section = "TCH_SYMB_SECTION";
        public static readonly string DxfName_TCH_NorthThumb = "TCH_NORTHTHUMB";
        public static readonly string DxfName_TCH_RectStair = "TCH_RECTSTAIR";
        public static readonly string DxfName_TCH_MultiStair = "TCH_MULTISTAIR";
        public static readonly string DxfName_TCH_DrawingName = "TCH_DRAWINGNAME";
        public static readonly string DxfName_TCH_DrawingIndex = "TCH_DRAWINGINDEX";
        public static readonly string DxfName_TCH_Elevation = "TCH_ELEVATION";
        public static readonly string DxfName_TCH_Opening = "TCH_OPENING";

        // Area Frame
        public static readonly string RegAppName_AreaFrame = "THCAD_AF";
        public static readonly string RegAppName_AreaFrame_Version = "V2.2";
        public static readonly string RegAppName_AreaFrame_Version_Legacy = "V2.1";

        // Fire Compartment
        public static readonly string RegAppName_AreaFrame_FireCompartment_Fill = "THCAD_FCFill";
        public static readonly string RegAppName_AreaFrame_FireCompartment_Parking = "THCAD_FC_P";
        public static readonly string RegAppName_AreaFrame_FireCompartment_Commerce = "THCAD_FC_C";

        // Download server
        public static readonly string ServerUrl = "http://49.234.60.227/AI/thcad";

        // Support 路径
        public static string SupportPath()
        {
            return Path.Combine(ContentsPath(), "Support");
        }

        // 暖通风机块
        public static string HvacModelDwgPath()
        {
            return Path.Combine(SupportPath(), "暖通.选型.风机.dwg");
        }

        // 电气烟感/温感图块
        public static string ElectricalSensorDwgPath()
        {
            return Path.Combine(SupportPath(), "烟感温感图块.dwg");
        }

        // ToolPalette 路径
        public static string ToolPalettePath()
        {
            return Path.Combine(SupportPath(), "ToolPalette");
        }

        // Standard style 路径
        public static string StandardStylePath()
        {
            return Path.Combine(ContentsPath(), "Standards", "Style");
        }

        // Resources 路径
        public static string ResourcesPath()
        {
            return Path.Combine(ContentsPath(), "Resources");
        }

        // Plotters 路径
        public static string PlottersPath()
        {
            return Path.Combine(ContentsPath(), "Plotters");
        }

        // PrinterDescPath 路径
        public static string PrinterDescPath()
        {
            return Path.Combine(ContentsPath(), "Plotters", "PMP Files");
        }

        // PrinterStyleSheetPath 路径
        public static string PrinterStyleSheetPath()
        {
            return Path.Combine(ContentsPath(), "Plotters", "Plot Styles");
        }

        // Contents 路径
        private static string ContentsPath()
        {
            return Path.Combine(RootPath(), "Contents");
        }

        // 运行时根目录
        private static string RootPath()
        {
#if DEBUG
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Autodesk\ApplicationPlugins\ThMEPPlugin.bundle");
#else
            var fi = new FileInfo(Assembly.GetCallingAssembly().Location);
            return fi.Directory.Parent.Parent.Parent.FullName;
#endif
        }
    }
}
