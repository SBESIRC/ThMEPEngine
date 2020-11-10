using Autodesk.AutoCAD.Customization;
using DotNetARX;

namespace TianHua.AutoCAD.ThCui
{
    public class ThToolBar
    {
        public static void CreateToolbars(CustomizationSection cs)
        {
            CreateThWSSToolbar(cs);
            CreateThHAVCToolbar(cs);
            CreateThGeneralToolbar(cs);
            CreateThStructureToolbar(cs);
            CreateThConstructionToolbar(cs);
            CreateThElectricalToolbar(cs);
            CreateThArchitectureToolbar(cs);
        }

        public static void CreateThGeneralToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_GENERAL);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                // 设置
                toolbar.AddToolbarButton(-1, "快捷键", "ID_THALIAS");
                toolbar.AddSeparator(-1);

                // 快速选择命令集
                toolbar.AddToolbarButton(-1, "颜色选择", "ID_THQS _COLOR");
                toolbar.AddToolbarButton(-1, "图层选择", "ID_THQS _LAYER");
                toolbar.AddToolbarButton(-1, "线型选择", "ID_THQS _LINETYPE");
                toolbar.AddToolbarButton(-1, "标注选择", "ID_THQS _DIMENSION");
                toolbar.AddToolbarButton(-1, "填充选择", "ID_THQS _HATCH");
                toolbar.AddToolbarButton(-1, "文字选择", "ID_THQS _TEXT");
                toolbar.AddToolbarButton(-1, "图块名选择", "ID_THQS _BLOCK");
                toolbar.AddToolbarButton(-1, "上次建立", "ID_THQS _LASTAPPEND");
                toolbar.AddToolbarButton(-1, "上次选取", "ID_THQS _PREVIOUS");
                toolbar.AddSeparator(-1);

                // 对齐命令集
                toolbar.AddToolbarButton(-1, "向上对齐", "ID_THAL _TOP");
                toolbar.AddToolbarButton(-1, "水平居中", "ID_THAL _HORIZONTAL");
                toolbar.AddToolbarButton(-1, "向下对齐", "ID_THAL _BOTTOM");
                toolbar.AddToolbarButton(-1, "向左对齐", "ID_THAL _LEFT");
                toolbar.AddToolbarButton(-1, "垂直居中", "ID_THAL _VERTICAL");
                toolbar.AddToolbarButton(-1, "向右对齐", "ID_THAL _RIGHT");
                toolbar.AddToolbarButton(-1, "水平均分", "ID_THAL _XDISTRIBUTE");
                toolbar.AddToolbarButton(-1, "垂直均分", "ID_THAL _YDISTRIBUTE");
                toolbar.AddSeparator(-1);

                // 块断线命令集
                toolbar.AddToolbarButton(-1, "插块断线", "ID_THBBR");
                toolbar.AddToolbarButton(-1, "选块断线", "ID_THBBE");
                toolbar.AddToolbarButton(-1, "全选断线", "ID_THBBS");
                toolbar.AddSeparator(-1);

                // 布图打印
                toolbar.AddToolbarButton(-1, "批量打印PPT", "ID_THBPP");
                toolbar.AddToolbarButton(-1, "批量打印DWF", "ID_THBPD");
                toolbar.AddToolbarButton(-1, "批量打印PDF", "ID_THBPT");
                toolbar.AddSeparator(-1);

                // 辅助编辑工具
                toolbar.AddToolbarButton(-1, "天华复制", "ID_THCO");
                toolbar.AddToolbarButton(-1, "天华格式刷", "ID_THMA");
                toolbar.AddToolbarButton(-1, "文字块镜像", "ID_THMIR");
                toolbar.AddToolbarButton(-1, "批量缩放", "ID_THMSC");
                toolbar.AddToolbarButton(-1, "版次信息修改", "ID_THSVM");
                toolbar.AddToolbarButton(-1, "尺寸避让", "ID_THDTA");
                toolbar.AddToolbarButton(-1, "Z值归零", "ID_THZ0");
                toolbar.AddToolbarButton(-1, "DGN清理", "ID_THPURGE");
            }
        }

        public static void CreateThArchitectureToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_ARCHITECTURE);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                // 图层
                toolbar.AddToolbarButton(-1, "建立总图图层", "ID_THAPL");
                toolbar.AddToolbarButton(-1, "建立单体图层", "ID_THAUL");
                toolbar.AddSeparator(-1);

                // 一键彩总
                toolbar.AddToolbarButton(-1, "一键生成", "ID_THPGE");
                toolbar.AddToolbarButton(-1, "局部刷新", "ID_THPUD");
                toolbar.AddToolbarButton(-1, "映射管理", "ID_THPCF");
                toolbar.AddToolbarButton(-1, "常用设置", "ID_THPOP");
                toolbar.AddToolbarButton(-1, "生成查看", "ID_THPBR");
            }
        }

        public static void CreateThStructureToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_STRUCTURE);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                toolbar.AddToolbarButton(-1, "建立结构图层", "ID_THSLC");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "柱校核（公测）", "ID_THCRC");
            }
        }

        public static void CreateThHAVCToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_HAVC);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                toolbar.AddToolbarButton(-1, "房间面积框线", "ID_THABC");
                toolbar.AddToolbarButton(-1, "管线断线", "ID_THLTR");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "建立暖通图层", "ID_THMLC");
                toolbar.AddToolbarButton(-1, "处理底图", "ID_THLPM");
                toolbar.AddToolbarButton(-1, "锁定暖通图层", "ID_THMLK");
                toolbar.AddToolbarButton(-1, "隔离暖通图层", "ID_THMUK");
                toolbar.AddToolbarButton(-1, "解锁所有图层", "ID_THUKA");
                toolbar.AddToolbarButton(-1, "关闭暖通图层", "ID_THMOF");
                toolbar.AddToolbarButton(-1, "开启暖通图层", "ID_THMON");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "通风模式", "ID_THTF");
                toolbar.AddToolbarButton(-1, "水管模式", "ID_THSG");
                toolbar.AddToolbarButton(-1, "消防模式", "ID_THXF");
                toolbar.AddToolbarButton(-1, "暖通全显", "ID_THNT");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "图块集", "ID_THBLI");
            }
        }

        public static void CreateThElectricalToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_ELECTRICAL);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                toolbar.AddToolbarButton(-1, "管线断线", "ID_THLTR");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "建立电气图层", "ID_THELC");
                toolbar.AddToolbarButton(-1, "处理底图", "ID_THLPE");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "提电气块转换", "ID_THBEE");
                toolbar.AddToolbarButton(-1, "图块集", "ID_THBLI");
            }
        }

        public static void CreateThWSSToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_WSS);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                // 暂时移除"喷头布置"
                //toolbar.AddToolbarButton(-1, "喷头布置", "ID_THSPC");
                toolbar.AddToolbarButton(-1, "管线断线", "ID_THLTR");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "建立给排水图层", "ID_THPLC");
                toolbar.AddToolbarButton(-1, "处理底图", "ID_THLPP");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "图块集", "ID_THBLI");
            }
        }

        public static void CreateThConstructionToolbar(CustomizationSection cs)
        {
            Toolbar toolbar = cs.MenuGroup.AddToolbar(ThCuiCommon.PROFILE_CONSTRUCTION);
            if (toolbar != null)
            {
                // 隐藏
                toolbar.ToolbarVisible = ToolbarVisible.hide;

                toolbar.AddToolbarButton(-1, "建立建筑图层", "ID_THALC");
                toolbar.AddToolbarButton(-1, "车位编号", "ID_THCNU");
                toolbar.AddSeparator(-1);
                toolbar.AddToolbarButton(-1, "天华单体规整", "ID_THBPS");
                toolbar.AddToolbarButton(-1, "天华总平规整", "ID_THSPS");
                toolbar.AddToolbarButton(-1, "单体面积汇总", "ID_THBAC");
                toolbar.AddToolbarButton(-1, "综合经济技术指标表", "ID_THTET");
                toolbar.AddToolbarButton(-1, "防火分区疏散表", "ID_THFET");
            }
        }
    }
}