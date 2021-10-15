﻿using Autodesk.AutoCAD.Customization;
using ThCADExtension;

namespace TianHua.AutoCAD.ThCui
{
    public class ThRibbonBar
    { 
        public static void CreateThRibbonBar(CustomizationSection cs)
        {
            var tab = cs.AddNewTab(ThCADCommon.RibbonTabName, ThCADCommon.RibbonTabTitle);
            if (tab != null)
            {
                CreateHelpPanel(tab);
                CreatePreconditionPanel(tab);
                CreateWSSPanel(tab);
                CreateHVACPanel(tab);
                CreateElectricPanel(tab);
                CreateArchitecturePanel(tab);
            }
        }
        private static void CreateHVACPanel(RibbonTabSource tab)
        {
            CreateHVACInstallationPanel(tab);
            CreateHVACPlanPanel(tab);
        }

        private static void CreateHVACInstallationPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACINSTALLATION", "设备选型");
            var row = panel.AddNewRibbonRow();

            // 风机选型
            row.AddNewButton("风机选型",
                "天华风机选型",
                "THFJ",
                "为各种应用场景自动选型风机，可插入图块、导出数据表。包含防排烟计算功能。设计数据与图纸绑定。",
                "IDI_THCAD_THFJ_SMALL",
                "IDI_THCAD_THFJ_LARGE",
                RibbonButtonStyle.LargeWithText);

            // 小风机
            row.AddNewButton("小风机",
                "天华小风机",
                "THXFJ",
                "天华小风机",
                "IDI_THCAD_THXFJ_SMALL",
                "IDI_THCAD_THXFJ_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateHVACPlanPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACPLAN", "平面图");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 风平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风平面",
                    "天华风平面",
                    "THFPM",
                    "天华风平面",
                    "IDI_THCAD_THFPM_SMALL",
                    "IDI_THCAD_THFPM_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 机房平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("机房平面",
                    "天华机房平面",
                    "THFJF",
                    "基于风机图块与风管中心线，自动生成风管、风阀、开洞等元素。",
                    "IDI_THCAD_THFJF_SMALL",
                    "IDI_THCAD_THFJF_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 地库风平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("地库风平面",
                    "天华地库风平面",
                    "THDKFPM",
                    "天华地库风平面",
                    "IDI_THCAD_THDKFPM_SMALL",
                    "IDI_THCAD_THDKFPM_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 风管修改
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风管修改",
                    "天华风管修改",
                    "THDKFPMFG",
                    "天华风管修改",
                    "IDI_THCAD_THDKFPMFG_SMALL",
                    "IDI_THCAD_THDKFPMFG_SMALL",
                    RibbonButtonStyle.SmallWithText);

                // 整体修改
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("整体修改",
                    "天华整体修改",
                    "THDKFPMXG",
                    "天华整体修改",
                    "IDI_THCAD_THDKFPMXG_SMALL",
                    "IDI_THCAD_THDKFPMXG_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }


        private static void CreateElectricPanel(RibbonTabSource tab)
        {
            CreateEAFASPanel(tab);
            CreateELightingPanel(tab);
            CreateElectronicPanel(tab);
            CreateEExchangePanel(tab);
        }

        private static void CreateEAFASPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("EAFAS", "火灾报警系统");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 火灾报警
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("火灾报警",
                    "天华火灾报警",
                    "THHZBJ",
                    "天华火灾报警",
                    "IDI_THCAD_THHZBJ_SMALL",
                    "IDI_THCAD_THHZBJ_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 烟感温感布置
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("烟感温感布置",
                    "天华烟感温感布置",
                    "THYWG",
                    "一键布置烟感和温感点位",
                    "IDI_THCAD_THYWG_SMALL",
                    "IDI_THCAD_THYWG_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 火灾报警系统
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("火灾报警系统",
                    "天华火灾报警系统",
                    "THHZXT",
                    "天华火灾报警系统",
                    "IDI_THCAD_THHZXT_SMALL",
                    "IDI_THCAD_THHZXT_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 地库消防广播
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("地库消防广播",
                    "天华地库消防广播",
                    "THGB",
                    "基于提取出的车道中心线，一键布置地库广播点位",
                    "IDI_THCAD_THGB_SMALL",
                    "IDI_THCAD_THGB_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 消防广播连线
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("消防广播连线",
                    "天华广播连线",
                    "THGBLX",
                    "基于提取出的车道中心线，一键完成地库广播连线",
                    "IDI_THCAD_THGBLX_SMALL",
                    "IDI_THCAD_THGBLX_SMALL",
                    RibbonButtonStyle.SmallWithText);

                // 广播盲区显示
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("广播盲区显示",
                    "天华广播盲区",
                    "THGBMQ",
                    "基于提取出的车道中心线，检测广播点位布置后，可能的走道端头盲区",
                    "IDI_THCAD_THGBMQ_SMALL",
                    "IDI_THCAD_THGBMQ_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }
        private static void CreateElectronicPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ELECTRONIC", "弱电系统");
            var row = panel.AddNewRibbonRow();

            // 安防平面
            row.AddNewButton("安防平面",
            "天华安防平面",
            "THAFPM",
            "天华安防平面",
            "IDI_THCAD_THAFPM_SMALL",
            "IDI_THCAD_THAFPM_LARGE",
            RibbonButtonStyle.LargeWithText);
        }

        private static void CreateELightingPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ELIGHTING", "照明系统");
            var row = panel.AddNewRibbonRow();

            // 地库车道照明
            row.AddNewButton("地库车道照明",
            "天华车道照明",
            "THCDZM",
            "基于提取出的车道中心线，一键布置车道线槽灯",
            "IDI_THCAD_THCDZM_SMALL",
            "IDI_THCAD_THCDZM_LARGE",
            RibbonButtonStyle.LargeWithText);

            // 地库车位照明
            row.AddNewButton("地库车位照明",
            "天华地库车位照明",
            "THCWZM",
            "天华地库车位照明",
            "IDI_THCAD_THCWZM_SMALL",
            "IDI_THCAD_THCWZM_LARGE",
            RibbonButtonStyle.LargeWithText);

            // 车道应急照明
            row.AddNewButton("车道应急照明",
            "天华车道应急照明",
            "THYJZM",
            "基于提取出的车道中心线，一键布置车道壁装应急照明点位",
            "IDI_THCAD_THYJZM_SMALL",
            "IDI_THCAD_THYJZM_LARGE",
            RibbonButtonStyle.LargeWithText);

            // 地库疏散指示
            row.AddNewButton("地库疏散指示",
            "天华疏散指示灯",
            "THSSZSD",
            "天华疏散指示灯",
            "IDI_THCAD_THSSZSD_SMALL",
            "IDI_THCAD_THSSZSD_LARGE",
            RibbonButtonStyle.LargeWithText);

            // 地库应急照明连线
            row.AddNewButton("地库应急\r\n照明连线",
            "天华车道应急照明连线",
            "THYJZMLX",
            "天华车道应急照明连线",
            "IDI_THCAD_THYJZMLX_SMALL",
            "IDI_THCAD_THYJZMLX_LARGE",
            RibbonButtonStyle.LargeWithText);
        }

        private static void CreateEExchangePanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("EEXCHANGE", "提资接收");
            var row = panel.AddNewRibbonRow();

            // 提资转换
            row.AddNewButton("提资转换",
            "天华提资转换",
            "THTZZH",
            "天华提资转换",
            "IDI_THCAD_THTZZH_SMALL",
            "IDI_THCAD_THTZZH_LARGE",
            RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWSSPanel(RibbonTabSource tab)
        {
            CreateWGroundPlanPanel(tab);
            CreateWGroundSystemPanel(tab);
            CreateWUndergroundPlanPanel(tab);
            CreateWUndergroundSystemPanel(tab);
            CreateWSprinklerPanel(tab);
            CreateWDetailAxonometryPanel(tab);
            CreateWValidationPanel(tab);
        }

        private static void CreateWGroundPlanPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WGROUNDPLAN", "地上平面图");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("住宅排水雨水",
                "地上排水平面",
                "THPYSPM",
                "地上排水平面",
                "IDI_THCAD_THPYSPM_SMALL",
                "IDI_THCAD_THPYSPM_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWGroundSystemPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WGROUNDSYSTEM", "地上系统图");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("给水",
                "天华地上给水系统图",
                "THJSXTT",
                "天华地上给水系统图",
                "IDI_THCAD_THJSXTT_SMALL",
                "IDI_THCAD_THJSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("雨水",
                "天华地上雨水系统图",
                "THYSXTT",
                "天华地上雨水系统图",
                "IDI_THCAD_THYSXTT_SMALL",
                "IDI_THCAD_THYSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("排水",
                "天华地上排水系统图",
                "THPSXTT",
                "天华地上排水系统图",
                "IDI_THCAD_THPSXTT_SMALL",
                "IDI_THCAD_THPSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("消火栓",
                "天华地上消火栓系统图",
                "THXHSXTT",
                "天华地上消火栓系统图",
                "IDI_THCAD_THXHSXTT_SMALL",
                "IDI_THCAD_THXHSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWUndergroundPlanPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WUNDERGROUNDPLAN", "地下平面图");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("冲洗点位",
                "天华冲洗点位",
                "THDXCX",
                "天华冲洗点位",
                "IDI_THCAD_THDXCX_SMALL",
                "IDI_THCAD_THDXCX_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("潜水泵",
                "天华潜水泵布置",
                "THSJSB",
                "天华潜水泵布置",
                "IDI_THCAD_THSJSB_SMALL",
                "IDI_THCAD_THSJSB_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("消火栓连管",
                "天华消火栓连管",
                "THDXXHS",
                "天华消火栓连管",
                "IDI_THCAD_THDXXHS_SMALL",
                "IDI_THCAD_THDXXHS_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWUndergroundSystemPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WUNDERGROUNDSYSTEM", "地下系统图");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("消火栓",
                "天华地下消火栓系统图",
                "THDXXHSXTT",
                "天华地下消火栓系统图",
                "IDI_THCAD_THDXXHSXTT_SMALL",
                "IDI_THCAD_THDXXHSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("压力排水",
                "天华地下压力排水系统图",
                "THDXPSXTT",
                "天华地下压力排水系统图",
                "IDI_THCAD_THDXPSXTT_SMALL",
                "IDI_THCAD_THDXPSXTT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("喷淋",
                "天华地下喷淋系统图",
                "THDXPLXTT",
                "天华地下喷淋系统图",
                "IDI_THCAD_THDXPLXTT_SMALL",
                "IDI_THCAD_THDXPLXTT_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWSprinklerPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WSPRINKLER", "消防喷淋");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("喷头布置",
                "天华喷头布置",
                "THPL",
                "天华喷头布置",
                "IDI_THCAD_THPLPT_SMALL",
                "IDI_THCAD_THPLPT_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("喷头校核",
                "天华喷头校核",
                "THPTJH",
                "天华喷头校核",
                "IDI_THCAD_THPTJH_SMALL",
                "IDI_THCAD_THPTJH_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWDetailAxonometryPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WDETAILAXONOMETRY", "大样轴侧图");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("给水\r\n大样",
                "天华给水大样图",
                "THJSDY",
                "天华给水大样图",
                "IDI_THCAD_THJSDY_SMALL",
                "IDI_THCAD_THJSDY_LARGE",
                RibbonButtonStyle.LargeWithText);

            row.AddNewButton("给水\r\n轴测",
                "天华给水轴测图",
                "THJSZC",
                "天华给水轴测图",
                "IDI_THCAD_THJSZC_SMALL",
                "IDI_THCAD_THJSZC_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateWValidationPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WVALIDATION", "校核");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("消火栓\r\n灭火器",
                "天华消火栓校核",
                "THXHSJH",
                "天华消火栓校核",
                "IDI_THCAD_THXHSJH_SMALL",
                "IDI_THCAD_THXHSJH_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateArchitecturePanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("AEXCHANGE", "机电提资");
            var row = panel.AddNewRibbonRow();

            row.AddNewButton("风机基础",
                "天华风机基础提资",
                "THFJJC",
                "天华风机基础提资",
                "IDI_THCAD_THFJJC_SMALL",
                "IDI_THCAD_THFJJC_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreatePreconditionPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("PRECONDITION", "前置输入");
            var row = panel.AddNewRibbonRow();

            // 楼层定义
            row.AddNewButton("楼层定义",
                "天华楼层定义",
                "THLCDY",
                "天华楼层定义",
                "IDI_THCAD_THLCDY_SMALL",
                "IDI_THCAD_THLCDY_LARGE",
                RibbonButtonStyle.LargeWithText);

            // 提车道中心线
            row.AddNewButton("提车道中心线",
                "天华提车道中心线",
                "THTCD",
                "提取建筑底图的车道中心线到本图中，用于车道照明、车道应急照明、广播的布点和连线",
                "IDI_THCAD_THTCD_SMALL",
                "IDI_THCAD_THTCD_LARGE",
                RibbonButtonStyle.LargeWithText);

            // 图块配置
            row.AddNewButton("图块配置",
                "天华图块配置",
                "THWTKSB",
                "天华图块配置",
                "IDI_THCAD_THWTKSB_SMALL",
                "IDI_THCAD_THWTKSB_LARGE",
                RibbonButtonStyle.LargeWithText);

            // 房间框线
            row.AddNewButton("房间框线",
                "天华房间框线",
                "THFJKX",
                "天华房间框线",
                "IDI_THCAD_THFJKX_SMALL",
                "IDI_THCAD_THFJKX_LARGE",
                RibbonButtonStyle.LargeWithText);

            // 房间名称
            row.AddNewButton("房间名称",
                "天华房间名称",
                "THKJMCTQ",
                "天华房间名称",
                "IDI_THCAD_THKJMCTQ_SMALL",
                "IDI_THCAD_THKJMCTQ_LARGE",
                RibbonButtonStyle.LargeWithText);
        }

        private static void CreateHelpPanel(RibbonTabSource tab)
        {
            // 登录界面
            var panel = tab.AddNewPanel("Help", "帮助");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 专业切换
                var subRow = subPanel.AddNewRibbonRow();
                var splitButton = subRow.AddNewSplitButton(
                    "专业切换",
                    RibbonSplitButtonBehavior.SplitFollow,
                    RibbonSplitButtonListStyle.IconText,
                    RibbonButtonStyle.LargeWithText);

                // 建筑专业
                splitButton.AddNewButton("建筑专业",
                    "天华建筑",
                    "THMEPPROFILE _A",
                    "切换到天华建筑",
                    "IDI_THCAD_ARCHITECTURE_SMALL",
                    "IDI_THCAD_ARCHITECTURE_LARGE",
                    RibbonButtonStyle.LargeWithText);

                // 暖通专业
                splitButton.AddNewButton("暖通专业",
                    "天华暖通",
                    "THMEPPROFILE _H",
                    "切换到天华暖通",
                    "IDI_THCAD_HAVC_SMALL",
                    "IDI_THCAD_HAVC_LARGE",
                    RibbonButtonStyle.LargeWithText);

                // 电气专业
                splitButton.AddNewButton("电气专业",
                    "天华电气",
                    "THMEPPROFILE _E",
                    "切换到天华电气",
                    "IDI_THCAD_ELECTRICAL_SMALL",
                    "IDI_THCAD_ELECTRICAL_LARGE",
                    RibbonButtonStyle.LargeWithText);

                // 给排水专业
                splitButton.AddNewButton("给排水专业",
                    "天华给排水",
                    "THMEPPROFILE _W",
                    "切换到天华给排水",
                    "IDI_THCAD_WATER_SMALL",
                    "IDI_THCAD_WATER_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }
        }
    }
}
