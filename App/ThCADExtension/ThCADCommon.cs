using System;
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
        public static readonly string RibbonTabTitle = "天华AI工具集";
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
        public static readonly string DxfName_TCH_Pipe = "TCH_PIPE";
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
        public static readonly string DxfName_TCH_EQUIPMENT_16 = "TCH_EQUIPMENT";
        public static readonly string DxfName_TCH_EQUIPMENT_12 = "TCH EQUIPMENT";

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

        // 暖通图层图块
        public static string HvacPipeDwgPath()
        {
            return Path.Combine(SupportPath(), "暖通图层图块.dwg");
        }

        // 电气图层图块
        public static string ElectricalDwgPath()
        {
            return Path.Combine(SupportPath(), "电气图层图块.dwg");
        }

        // 火灾自动报警系统
        public static string AutoFireAlarmSystemDwgPath()
        {
            return ElectricalDwgPath();
        }

        // 结构专业图层
        public static string StructTemplatePath()
        {
            return Path.Combine(SupportPath(), "结构图层图块.dwg");
        }

        // 消防喷淋块
        public static string SprinklerDwgPath()
        {
            return Path.Combine(SupportPath(), "给排水.喷淋.dwg");
        }

        // 楼层框定图块
        public static string StoreyFrameDwgPath()
        {
            return Path.Combine(SupportPath(), "楼层定义工具.dwg");
        }

        // 给排水专业图纸
        public static string WSSDwgPath()
        {
            return Path.Combine(SupportPath(), "地上给水排水平面图模板.dwg");
        }

        /// <summary>
        /// 电力配电系统图元素
        /// </summary>
        /// <returns></returns>
        public static string PDSComponentDwgPath()
        {
            return Path.Combine(SupportPath(), "电力配电系统图元素.dwg");
        }

        /// <summary>
        /// 电力配电系统图生成
        /// </summary>
        /// <returns></returns>
        public static string PDSDiagramDwgPath()
        {
            return Path.Combine(SupportPath(), "电力配电系统图生成.dwg");
        }

        /// <summary>
        /// 平面关注对象
        /// </summary>
        /// <returns></returns>
        public static string PDSComponentsPath()
        {
            return Path.Combine(SupportPath(), "平面关注对象.xlsx");
        }

        // 房间名称分类处理
        public static string RoomConfigPath()
        {
            return Path.Combine(SupportPath(), "房间名称分类处理.xlsx");
        }
        //风机参数表
        public static string FanParameterTablePath()
        {
            return Path.Combine(SupportPath(), "风机参数表.xlsx");
        }
        public static string FanMaterialTablePath()
        {
            return Path.Combine(SupportPath(), "导出材料表.xlsx");
        }        
        /// <summary>
        /// 室内机信息表
        /// </summary>
        /// <returns></returns>
        public static string IndoorFanDataTablePath() 
        {
            return Path.Combine(SupportPath(), "室内机配置表.xlsx");
        }
        /// <summary>
        /// 室内机导出数据模板
        /// </summary>
        /// <returns></returns>
        public static string IndoorFanExportTablePath()
        {
            return Path.Combine(SupportPath(), "室内机材料表模板.xlsx");
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

        // 风管尺寸规格信息
        public static string DuctSizeParametersPath()
        {
            return Path.Combine(SupportPath(), "风管尺寸参数信息.json");
        }

        // 风口尺寸规格信息
        public static string PortSizeParametersPath()
        {
            return Path.Combine(SupportPath(), "风口尺寸参数信息.json");
        }

        public static string TCHHVACDBPath()
        {
            return Path.Combine(SupportPath(), "TG20.db");
        }

        public static string DuctInOutMapping()
        {
            return Path.Combine(SupportPath(), "管道内外段与进出风口对照.json");
        }

        // 运行时根目录
        private static string RootPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Autodesk\ApplicationPlugins\ThMEPPlugin.bundle");
        }
    }
}
