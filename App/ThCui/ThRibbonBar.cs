using Autodesk.AutoCAD.Customization;
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
                CreateStructurePanel(tab);
                CreateArchitecturePanel(tab);
                CreateGeneralLibraryPanel(tab);
                CreateGeneralDrawingPanel(tab);
                CreateGeneralModifyPanel(tab);
                CreateGeneralSelectPanel(tab);
                CreateGeneralAuxiliaryPanel(tab);
            }
        }
        private static void CreateHVACPanel(RibbonTabSource tab)
        {
            CreateHVACCalculationPanel(tab);
            CreateHVACInstallationPanel(tab);
            CreateHVACVentilationPanel(tab);
            CreateHVACHeatingPanel(tab);
        }

        private static void CreateHVACCalculationPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACCALCULATION", "计算");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 室外参数
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("室外参数",
                    "天华室外参数设置",
                    "THSWSZ",
                    "天华室外参数设置",
                    "IDI_THCAD_THSWSZ_SMALL",
                    "IDI_THCAD_THSWSZ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 负荷计算
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("负荷计算",
                    "天华负荷通风计算",
                    "THFHJS",
                    "天华负荷通风计算",
                    "IDI_THCAD_THFHJS_SMALL",
                    "IDI_THCAD_THFHJS_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateHVACInstallationPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACINSTALLATION", "选型");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 风机选型
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风机选型",
                    "天华风机选型",
                    "THFJXX",
                    "天华风机选型",
                    "IDI_THCAD_THFJ_SMALL",
                    "IDI_THCAD_THFJ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 小风机
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("小风机",
                    "天华小风机",
                    "THXFJ",
                    "天华小风机",
                    "IDI_THCAD_THXFJ_SMALL",
                    "IDI_THCAD_THXFJ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 室内机
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("室内机",
                    "天华室内机布置",
                    "THSNJ",
                    "天华室内机布置",
                    "IDI_THCAD_THSNJ_SMALL",
                    "IDI_THCAD_THSNJ_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void  CreateHVACVentilationPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACPLANV", "风平面");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 风路由
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风路由",
                    "天华风管路由",
                    "THFGLY",
                    "天华风管路由",
                    "IDI_THCAD_THFGLY_SMALL",
                    "IDI_THCAD_THFGLY_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 风管留洞
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风管留洞",
                    "天华风管留洞",
                    "THFGLD",
                    "天华风管留洞",
                    "IDI_THCAD_THFGLD_SMALL",
                    "IDI_THCAD_THFGLD_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 插风口
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("插风口",
                    "天华插风口",
                    "THCRFK",
                    "天华插风口",
                    "IDI_THCAD_THCRFK_SMALL",
                    "IDI_THCAD_THCRFK_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 风管断线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风管断线",
                    "天华插风管断线",
                    "THFGDX",
                    "天华插风管断线",
                    "IDI_THCAD_THFGDX_SMALL",
                    "IDI_THCAD_TTHFGDX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 风立管
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风立管",
                    "天华插风管立管",
                    "THFGLG",
                    "天华插风管立管",
                    "IDI_THCAD_THFGLG_SMALL",
                    "IDI_THCAD_THFGLG_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

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
        }

        private static void CreateHVACHeatingPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("HVACPLANH", "水平面");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 水路由
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("水路由",
                    "天华水管路由",
                    "THSGLY",
                    "天华水管路由",
                    "IDI_THCAD_THSGLY_SMALL",
                    "IDI_THCAD_THSGLY_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 水管断线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("水管断线",
                    "天华插水管断线",
                    "THSGDX",
                    "天华插水管断线",
                    "IDI_THCAD_THSGDX_SMALL",
                    "IDI_THCAD_TTHSGDX_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 水平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("水平面",
                    "天华水平面",
                    "THSPM",
                    "天华水平面",
                    "IDI_THCAD_THSPM_SMALL",
                    "IDI_THCAD_THSPM_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateElectricPanel(RibbonTabSource tab)
        {
            CreateEMaterials(tab);
            CreatePowerSystemPanel(tab);
            CreateElectronicPanel(tab);
            CreateELightingPanel(tab);
            CreateElightningPanel(tab);
        }

        private static void CreateEMaterials(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("EMATERIALS", "提资处理");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 提资转换
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("提资转换",
                    "天华提资转换",
                    "THTZZH",
                    "天华提资转换",
                    "IDI_THCAD_THTZZH_SMALL",
                    "IDI_THCAD_THTZZH_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 用电负荷
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("用电负荷",
                    "天华用电负荷计算",
                    "THYDFHJS",
                    "天华用电负荷计算",
                    "IDI_THCAD_THYDFHJS_SMALL",
                    "IDI_THCAD_THYDFHJS_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreatePowerSystemPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("POWERSYSTEM", "电力系统");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 配电箱系统
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("配电箱系统",
                    "天华配电箱系统",
                    "THDLXT",
                    "天华配电箱系统",
                    "IDI_THCAD_THDLXT_SMALL",
                    "IDI_THCAD_THDLXT_LARGE",
                RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateElectronicPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ELECTRONIC", "消防弱电");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 报警平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("报警平面",
                    "天华火灾报警平面",
                    "THHZBJ",
                    "天华火灾报警平面",
                    "IDI_THCAD_THHZBJ_SMALL",
                    "IDI_THCAD_THHZBJ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 地库烟感
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("地库烟感",
                    "天华烟感温感布置",
                    "THYWG",
                    "天华烟感温感布置",
                    "IDI_THCAD_THYWG_SMALL",
                    "IDI_THCAD_THYWG_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 地库广播
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("地库广播",
                    "天华地库消防广播",
                    "THXFGB",
                    "天华地库消防广播",
                    "IDI_THCAD_THGB_SMALL",
                    "IDI_THCAD_THGB_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 火警系统
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("火警系统",
                    "天华火灾报警系统",
                    "THHZXT",
                    "天华火灾报警系统",
                    "IDI_THCAD_THHZXT_SMALL",
                    "IDI_THCAD_THHZXT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 校核明细
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("校核明细",
                    "天华总线校核明细",
                    "THZXJHMX",
                    "天华总线校核明细",
                    "IDI_THCAD_THZXJHMX_SMALL",
                    "IDI_THCAD_THZXJHMX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 安防平面
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("安防平面",
                    "天华安防平面",
                    "THAFPM",
                    "天华安防平面",
                    "IDI_THCAD_THAFPM_SMALL",
                    "IDI_THCAD_THAFPM_LARGE",
                RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateElightningPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ELIGHTNING", "防雷接地");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 接地网
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("接地网",
                    "天华接地网",
                    "THJDPM",
                    "天接地网",
                    "IDI_THCAD_THJDPM_SMALL",
                    "IDI_THCAD_THJDPM_LARGE",
                RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateELightingPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ELIGHTING", "照明平面");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 照明平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("照明平面",
                    "天华照明平面",
                    "THZM",
                    "天华照明平面",
                    "IDI_THCAD_THZM_SMALL",
                    "IDI_THCAD_THZM_LARGE",
                RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 车位照明
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("车位照明",
                    "天华地库车位照明",
                    "THCWZM",
                    "天华地库车位照明",
                    "IDI_THCAD_THCWZM_SMALL",
                    "IDI_THCAD_THCWZM_LARGE",
                RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 疏散指示
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("疏散指示",
                    "天华疏散指示灯",
                    "THSSZSD",
                    "天华疏散指示灯",
                    "IDI_THCAD_THSSZSD_SMALL",
                    "IDI_THCAD_THSSZSD_LARGE",
                RibbonButtonStyle.SmallWithText);

                // 车道应急照明
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("车道应急照明",
                    "天华车道应急照明",
                    "THYJZM",
                    "天华车道应急照明",
                    "IDI_THCAD_THYJZM_SMALL",
                    "IDI_THCAD_THYJZM_LARGE",
                RibbonButtonStyle.SmallWithText);

                // 应急照明连线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("应急照明连线",
                    "天华车道应急照明连线",
                    "THYJZMLX",
                    "天华车道应急照明连线",
                    "IDI_THCAD_THYJZMLX_SMALL",
                    "IDI_THCAD_THYJZMLX_LARGE",
                RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateWSSPanel(RibbonTabSource tab)
        {
            CreateWGroundPlanPanel(tab);
            CreateWUndergroundPlanPanel(tab);
            CreateWHydrantPanel(tab);
            CreateWSprinklerPanel(tab);
            CreateWDetailAxonometryPanel(tab);
        }

        private static void CreateWGroundPlanPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WGROUNDPLAN", "地上给排水（住宅）");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 立管布置
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("立管布置",
                    "天华立管布置",
                    "THPYSPM",
                    "天华立管布置",
                    "IDI_THCAD_THPYSPM_SMALL",
                    "IDI_THCAD_THPYSPM_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 一层排水
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("一层排水",
                    "天华一层排水",
                    "ThSCPSPM",
                    "天华一层排水",
                    "IDI_THCAD_ThSCPSPM_SMALL",
                    "IDI_THCAD_ThSCPSPM_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 排水系统
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("排水系统",
                    "天华地上排水系统",
                    "THPSXTT",
                    "天华地上排水系统",
                    "IDI_THCAD_THPSXTT_SMALL",
                    "IDI_THCAD_THPSXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 给水系统
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("给水系统",
                    "天华地上给水系统图",
                    "THJSXTT",
                    "天华地上给水系统图",
                    "IDI_THCAD_THJSXTT_SMALL",
                    "IDI_THCAD_THJSXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 消火栓系统
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("消火栓系统",
                    "天华地上消火栓系统图",
                    "THXHSXTT",
                    "天华地上消火栓系统图",
                    "IDI_THCAD_THXHSXTT_SMALL",
                    "IDI_THCAD_THXHSXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateWUndergroundPlanPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WUNDERGROUNDPLAN", "地库给排水");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 冲洗点位
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("冲洗点位",
                    "天华冲洗点位",
                    "THDXCX",
                    "天华冲洗点位",
                    "IDI_THCAD_THDXCX_SMALL",
                    "IDI_THCAD_THDXCX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 布潜水泵
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("布潜水泵",
                    "天华潜水泵布置",
                    "THSJSB",
                    "天华潜水泵布置",
                    "IDI_THCAD_THSJSB_SMALL",
                    "IDI_THCAD_THSJSB_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 给水系统
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("给水系统",
                    "天华给水系统",
                    "THDXJSXT",
                    "天华给水系统",
                    "IDI_THCAD_THDXJSXT_SMALL",
                    "IDI_THCAD_THDXJSXT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 排水系统
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("排水系统",
                    "天华地下压力排水系统图",
                    "THDXPSXTT",
                    "天华地下压力排水系统图",
                    "IDI_THCAD_THDXPSXTT_SMALL",
                    "IDI_THCAD_THDXPSXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateWHydrantPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WHYDRANT", "消火栓灭火器");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 布置优化
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("布置优化",
                    "天华布置优化",
                    "THXHSYH",
                    "天华布置优化",
                    "IDI_THCAD_THXHSYH_SMALL",
                    "IDI_THCAD_THXHSYH_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 连管编号
                subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton(
                        "连管编号",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 消火栓连管
                    splitButton.AddNewButton("消火栓连管",
                        "天华消火栓连管",
                        "THDXXHS",
                        "天华消火栓连管",
                        "IDI_THCAD_THDXXHS_SMALL",
                        "IDI_THCAD_THDXXHS_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 消火栓编号
                    splitButton.AddNewButton("消火栓编号",
                        "天华消火栓编号",
                        "THXHSBH",
                        "天华消火栓编号",
                        "IDI_THCAD_THXHSBH_SMALL",
                        "IDI_THCAD_THXHSBH_LARGE",
                        RibbonButtonStyle.SmallWithText);
                }

                // 消火栓系统
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("消火栓系统",
                    "天华地下消火栓系统图",
                    "THDXXHSXTT",
                    "天华地下消火栓系统图",
                    "IDI_THCAD_THDXXHSXTT_SMALL",
                    "IDI_THCAD_THDXXHSXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 范围校核
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("范围校核",
                    "天华消火栓校核",
                    "THXHSJH",
                    "天华消火栓校核",
                    "IDI_THCAD_THXHSJH_SMALL",
                    "IDI_THCAD_THXHSJH_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 距离校核
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("距离校核",
                    "天华距离校核",
                    "MEASUREPATH",
                    "天华距离校核",
                    "IDI_THCAD_MEASUREPATH_SMALL",
                    "IDI_THCAD_MEASUREPATH_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateWSprinklerPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WSPRINKLER", "喷淋灭火");
            var row = panel.AddNewRibbonRow();


            {
                var subPanel = row.AddNewPanel();

                // 喷头布置
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("喷头布置",
                    "天华喷头布置",
                    "THPL",
                    "天华喷头布置",
                    "IDI_THCAD_THPLPT_SMALL",
                    "IDI_THCAD_THPLPT_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 连管标注
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("连管标注",
                    "天华喷头连管标注",
                    "THPTLGBZ",
                    "天华喷头连管标注",
                    "IDI_THCAD_THPTLGBZ_SMALL",
                    "IDI_THCAD_THPTLGBZ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 喷淋系统
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("喷淋系统",
                    "天华地下喷淋系统图",
                    "THDXPLXTT",
                    "天华地下喷淋系统图",
                    "IDI_THCAD_THDXPLXTT_SMALL",
                    "IDI_THCAD_THDXPLXTT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 喷头校核
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("喷头校核",
                    "天华喷头校核",
                    "THPTJH",
                    "天华喷头校核",
                    "IDI_THCAD_THPTJH_SMALL",
                    "IDI_THCAD_THPTJH_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateWDetailAxonometryPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("WDETAILAXONOMETRY", "给排水详图");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 给水平面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("给水平面",
                    "天华给水大样图",
                    "THJSDY",
                    "天华给水大样图",
                    "IDI_THCAD_THJSDY_SMALL",
                    "IDI_THCAD_THJSDY_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 给水轴测
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("给水轴测",
                    "天华给水轴测图",
                    "THJSZC",
                    "天华给水轴测图",
                    "IDI_THCAD_THJSZC_SMALL",
                    "IDI_THCAD_THJSZC_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 户型详图
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("户型详图",
                    "天华户型详图",
                    "THHXDYZC",
                    "天华户型详图",
                    "IDI_THCAD_THHXDYZC_SMALL",
                    "IDI_THCAD_THHXDYZC_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateArchitecturePanel(RibbonTabSource tab)
        {
            CreateADrawingPanel(tab);
            CreateAFireCompartmentPanel(tab);
            CreateAWallPanel(tab);
            CreateAMaterialsPanel(tab);
        }

        private static void CreateAWallPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("AWALL", "墙身绘制");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 凸窗墙身
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("凸窗墙身",
                    "天华凸窗墙身",
                    "WND1",
                    "天华凸窗墙身",
                    "IDI_THCAD_WND1_SMALL",
                    "IDI_THCAD_WND1_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 平窗墙身
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("平窗墙身",
                    "天华平窗墙身",
                    "WND2",
                    "天华平窗墙身",
                    "IDI_THCAD_WND2_SMALL",
                    "IDI_THCAD_WND2_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 阳台墙身
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("阳台墙身",
                    "天华阳台墙身",
                    "WND3",
                    "天华阳台墙身",
                    "IDI_THCAD_WND3_SMALL",
                    "IDI_THCAD_WND3_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 平台墙身
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("平台墙身",
                    "天华平台墙身",
                    "WND4",
                    "天华平台墙身",
                    "IDI_THCAD_WND4_SMALL",
                    "IDI_THCAD_WND4_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 设置墙身
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("设置墙身",
                    "天华设置墙身",
                    "SETWALLXLINE",
                    "天华设置墙身",
                    "IDI_THCAD_SETWALLXLINE_SMALL",
                    "IDI_THCAD_SETWALLXLINE_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 绘制墙身
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("绘制墙身",
                    "天华绘制墙身",
                    "DRAWWALLXLINE",
                    "天华绘制墙身",
                    "IDI_THCAD_DRAWWALLXLINE_SMALL",
                    "IDI_THCAD_DRAWWALLXLINE_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateAFireCompartmentPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("AFIRECOMPARTMENT", "防火分区");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 分区填充
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("分区填充",
                    "天华分区填充",
                    "FILLFIREZONE",
                    "天华分区填充",
                    "IDI_THCAD_FILLFIREZONE_SMALL",
                    "IDI_THCAD_FILLFIREZONE_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateADrawingPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("ADRAWING", "快速绘制");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 坡道剖面
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("坡道剖面",
                    "天华坡道剖面",
                    "RAMPWAY2",
                    "天华坡道剖面",
                    "IDI_THCAD_RAMPWAY2_SMALL",
                    "IDI_THCAD_RAMPWAY2_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 滴水绘制
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("滴水绘制",
                    "天华滴水绘制",
                    "DRIPPING",
                    "天华滴水绘制",
                    "IDI_THCAD_DRIPPING_SMALL",
                    "IDI_THCAD_DRIPPING_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 水平栏杆
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("水平栏杆",
                    "天华水平栏杆",
                    "DRAWRAILING",
                    "天华水平栏杆",
                    "IDI_THCAD_DRAWRAILING_SMALL",
                    "IDI_THCAD_DRAWRAILING_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateAMaterialsPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("AMATERIALS", "机电提资");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 风机基础
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("风机基础",
                    "天华风机基础提资",
                    "THFJJC",
                    "天华风机基础提资",
                    "IDI_THCAD_THFJJC_SMALL",
                    "IDI_THCAD_THFJJC_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateSBeamPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("SBEAM", "智能布梁");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 主梁生成
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("主梁生成",
                    "天华主梁生成",
                    "THZLSCUI",
                    "天华主梁生成",
                    "IDI_THCAD_THZLSC_SMALL",
                    "IDI_THCAD_THZLSC_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 次梁生成
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("次梁生成",
                    "天华次梁生成",
                    "THCLSCUI",
                    "天华次梁生成",
                    "IDI_THCAD_THCLSC_SMALL",
                    "IDI_THCAD_THCLSC_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 双线生成
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("双线生成",
                    "天华双线生成",
                    "THSXSCUI",
                    "天华双线生成",
                    "IDI_THCAD_THSXSC_SMALL",
                    "IDI_THCAD_THSXSC_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateStructurePanel(RibbonTabSource tab)
        {
            CreateSBeamPanel(tab);
            CreateSReinforcementPanel(tab);
            CreateSTempateDrawingPanel(tab);
        }

        private static void CreateSTempateDrawingPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("STEMPATEDRAWING", "模板图助手");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 建立结构图层
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("建立结构\r\n图层",
                    "建立结构图层",
                    "THSLC",
                    "建立结构专业天华标准图层",
                    "IDI_THCAD_THSLC_SMALL",
                    "IDI_THCAD_THSLC_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 楼板洞口
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("楼板洞口",
                    "天华楼板洞口",
                    "FYDKFH",
                    "天华楼板洞口",
                    "IDI_THCAD_FYDKFH_SMALL",
                    "IDI_THCAD_FYDKFH_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 降板边线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("降板边线",
                    "天华降板边线",
                    "FYJBBX",
                    "天华降板边线",
                    "IDI_THCAD_FYJBBX_SMALL",
                    "IDI_THCAD_FYJBBX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 基础对柱中
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("基础对柱中",
                    "天华基础对柱中",
                    "BASICSALIGN",
                    "天华基础对柱中",
                    "IDI_THCAD_BASICSALIGN_SMALL",
                    "IDI_THCAD_BASICSALIGN_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateSReinforcementPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("SREINFORCEMENT", "配筋助手");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 墙身配筋
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("墙身配筋",
                    "天华墙身配筋",
                    "FYDYXT",
                    "天华墙身配筋",
                    "IDI_THCAD_FYDYXT_SMALL",
                    "IDI_THCAD_FYDYXT_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 梯板配筋
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("梯板配筋",
                    "天华梯板配筋",
                    "SFSB",
                    "天华梯板配筋",
                    "IDI_THCAD_SFSB_SMALL",
                    "IDI_THCAD_SFSB_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 梁纵筋
                subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton(
                        "梁纵筋",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 初始设置
                    splitButton.AddNewButton("初始设置",
                        "天华梁纵筋初始设置",
                        "CSLPJYKL",
                        "天华梁纵筋初始设置",
                        "IDI_THCAD_CSLPJYKL_SMALL",
                        "IDI_THCAD_CSLPJYKL_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 梁纵筋校改
                    splitButton.AddNewButton("梁纵筋校改",
                        "天华梁纵筋校改",
                        "LPJTOOL",
                        "天华梁纵筋校改",
                        "IDI_THCAD_LPJTOOL_SMALL",
                        "IDI_THCAD_LPJTOOL_LARGE",
                        RibbonButtonStyle.SmallWithText);
                }
            }
        }

        private static void CreatePreconditionPanel(RibbonTabSource tab)
        {
            var panel = tab.AddNewPanel("PRECONDITION", "前置输入");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 梁配置
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("梁配置",
                    "天华梁配置",
                    "THLPZ",
                    "天华梁配置",
                    "IDI_THCAD_THLPZ_SMALL",
                    "IDI_THCAD_THLPZ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 房间框线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("房间框线",
                    "天华房间框线",
                    "THFJKX2",
                    "天华房间框线",
                    "IDI_THCAD_THFJKX_SMALL",
                    "IDI_THCAD_THFJKX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 框线对比
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("框线对比",
                    "天华框线对比",
                    "THKXBD",
                    "天华框线对比",
                    "IDI_THCAD_THKXDB_SMALL",
                    "IDI_THCAD_THKXDB_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 房间名称
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("房间名称",
                    "天华房间名称",
                    "THKJMCTQ",
                    "天华房间名称",
                    "IDI_THCAD_THKJMCTQ_SMALL",
                    "IDI_THCAD_THKJMCTQ_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 提车道线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("提车道线",
                    "天华提车道中心线",
                    "THTCDX",
                    "天华提车道中心线",
                    "IDI_THCAD_THTCD_SMALL",
                    "IDI_THCAD_THTCD_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 楼层定义
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("楼层定义",
                    "天华楼层定义",
                    "THLCDY",
                    "天华楼层定义",
                    "IDI_THCAD_THLCDY_SMALL",
                    "IDI_THCAD_THLCDY_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 楼层框线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("楼层框线",
                    "天华楼层框线",
                    "THLCKX",
                    "天华楼层框线",
                    "IDI_THCAD_THLCKX_SMALL",
                    "IDI_THCAD_THLCKX_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 图块配置
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("图块配置",
                    "天华图块配置",
                    "THWTKSB",
                    "天华图块配置",
                    "IDI_THCAD_THWTKSB_SMALL",
                    "IDI_THCAD_THWTKSB_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 房间功能
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("房间功能",
                    "天华房间功能提取",
                    "THFJGN",
                    "天华房间功能提取",
                    "IDI_THCAD_THFJGN_SMALL",
                    "IDI_THCAD_THFJGN_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 房间编号
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("房间编号",
                    "天华房间编号",
                    "THFJBH",
                    "天华房间编号",
                    "IDI_THCAD_THFJBH_SMALL",
                    "IDI_THCAD_THFJBH_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 提中心线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("提中心线",
                    "天华空间中心线",
                    "THKJZX",
                    "天华空间中心线",
                    "IDI_THCAD_THKJZX_SMALL",
                    "IDI_THCAD_THKJZX_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
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

                // 结构专业
                splitButton.AddNewButton("结构专业",
                    "天华结构",
                    "THMEPPROFILE _S",
                    "切换到天华结构",
                    "IDI_THCAD_STRUCTURE_SMALL",
                    "IDI_THCAD_STRUCTURE_LARGE",
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

            //{
            //    var subPanel = row.AddNewPanel();

            //    // 帮助文档
            //    var subRow = subPanel.AddNewRibbonRow();
            //    subRow.AddNewButton("帮助文档",
            //        "天华帮助文档",
            //        "THMEPHELP",
            //        "获取帮助文档",
            //        "IDI_THCAD_THHLP_SMALL",
            //        "IDI_THCAD_THHLP_LARGE",
            //        RibbonButtonStyle.LargeWithText);
            //}
        }

        private static void CreateGeneralLibraryPanel(RibbonTabSource tab)
        {
            // 通用资料
            var panel = tab.AddNewPanel("GENERALLIBRARY", "通用资料");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 标准图库
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("标准图库",
                    "天华标准图库",
                    "XTZS",
                    "天华标准图库",
                    "IDI_THCAD_XTZS_SMALL",
                    "IDI_THCAD_XTZS_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }
        }

        private static void CreateGeneralDrawingPanel(RibbonTabSource tab)
        {
            // 通用绘制
            var panel = tab.AddNewPanel("GENERALDRAWING", "通用绘制");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 地库连线
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("地库连线",
                    "天华地库连线",
                    "THLX",
                    "天华地库连线",
                    "IDI_THCAD_THLX_SMALL",
                    "IDI_THCAD_THLX_LARGE",
                    RibbonButtonStyle.LargeWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 插块断线
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("插块断线",
                    "天华插块断线",
                    "THBBR",
                    "将选择的图块插入到直线/多段线时自动断线",
                    "IDI_THCAD_THBBR_SMALL",
                    "IDI_THCAD_THBBR_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 全选断线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("全选断线",
                    "天华全选断线",
                    "THBBS",
                    "批量选择需要断线的图块，根据各自所需断线的切线方向自动调整图块角度且完成断线",
                    "IDI_THCAD_THBBS_SMALL",
                    "IDI_THCAD_THBBS_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 选块断线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("选块断线",
                    "天华选块断线",
                    "THBBE",
                    "点选单个图块，根据所需断线的切线方向自动调整图块角度且完成断线",
                    "IDI_THCAD_THBBE_SMALL",
                    "IDI_THCAD_THBBE_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 文字镜像
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("文字镜像",
                    "文字块镜像",
                    "THMIR",
                    "镜像含文字块，使文字不反向",
                    "IDI_THCAD_THMIR_SMALL",
                    "IDI_THCAD_THMIR_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 刷新线长
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("刷新线长",
                    "天华刷新线长",
                    "REDIMLINE",
                    "天华刷新线长",
                    "IDI_THCAD_REDIMLINE_SMALL",
                    "IDI_THCAD_REDIMLINE_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 标多段线
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("标多段线",
                    "天华标多段线",
                    "DIMLINE",
                    "天华标多段线",
                    "IDI_THCAD_DIMLINE_SMALL",
                    "IDI_THCAD_DIMLINE_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }

        private static void CreateGeneralAuxiliaryPanel(RibbonTabSource tab)
        {
            // 通用辅助
            var panel = tab.AddNewPanel("GENERALAUXILIARY", "通用辅助");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // Z值归零
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("Z值归零",
                    "天华Z值归零",
                    "THZ0",
                    "将模型空间内所有对象Z值归零，使之处于同一平面",
                    "IDI_THCAD_THZ0_SMALL",
                    "IDI_THCAD_THZ0_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 坐标辅助
                subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton("坐标辅助",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 两点坐标系
                    splitButton.AddNewButton("两点坐标系",
                        "天华两点坐标系",
                        "U2P",
                        "天华两点坐标系",
                        "IDI_THCAD_U2P_SMALL",
                        "IDI_THCAD_U2P_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 世界坐标系
                    splitButton.AddNewButton("世界坐标系",
                        "天华世界坐标系",
                        "GOW",
                        "天华世界坐标系",
                        "IDI_THCAD_GOW_SMALL",
                        "IDI_THCAD_GOW_LARGE",
                        RibbonButtonStyle.SmallWithText);
                }
            }
        }

        private static void CreateGeneralSelectPanel(RibbonTabSource tab)
        {
            // 通用选择
            var panel = tab.AddNewPanel("GENERALSELECT", "通用选择");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 天华快选
                var subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton("天华快选",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 颜色
                    splitButton.AddNewButton("颜色",
                        "按颜色选取",
                        "THQS _COLOR",
                        "按颜色选取",
                        "IDI_THCAD_THQS_COLOR_SMALL",
                        "IDI_THCAD_THQS_COLOR_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 图层
                    splitButton.AddNewButton("图层",
                        "按图层选取",
                        "THQS _LAYER",
                        "按图层选取",
                        "IDI_THCAD_THQS_LAYER_SMALL",
                        "IDI_THCAD_THQS_LAYER_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 线型
                    splitButton.AddNewButton("线型",
                        "按线型选取",
                        "THQS _LINETYPE",
                        "按线型选取",
                        "IDI_THCAD_THQS_LINETYPE_SMALL",
                        "IDI_THCAD_THQS_LINETYPE_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 标注
                    splitButton.AddNewButton("标注",
                        "按标注选取",
                        "THQS _DIMENSION",
                        "按标注选取",
                        "IDI_THCAD_THQS_ANNOTATION_SMALL",
                        "IDI_THCAD_THQS_ANNOTATION_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 填充
                    splitButton.AddNewButton("填充",
                        "按填充选取",
                        "THQS _HATCH",
                        "按填充选取",
                        "IDI_THCAD_THQS_HATCH_SMALL",
                        "IDI_THCAD_THQS_HATCH_LARGE",
                        RibbonButtonStyle.LargeWithText);

                    // 文字
                    splitButton.AddNewButton("文字",
                        "按文字选取",
                        "THQS _TEXT",
                        "按文字选取",
                        "IDI_THCAD_THQS_TEXT_SMALL",
                        "IDI_THCAD_THQS_TEXT_LARGE",
                        RibbonButtonStyle.LargeWithText);

                    // 图块名
                    splitButton.AddNewButton("图块名",
                        "按图块名选取",
                        "THQS _BLOCK",
                        "按图块名选取",
                        "IDI_THCAD_THQS_BLOCK_SMALL",
                        "IDI_THCAD_THQS_BLOCK_LARGE",
                        RibbonButtonStyle.LargeWithText);

                    // 分割线
                    splitButton.AddNewSeparator(RibbonSeparatorStyle.Line);


                    // 上次建立
                    splitButton.AddNewButton("上次建立",
                        "按上次建立选择",
                        "THQS _LASTAPPEND",
                        "上次建立",
                        "IDI_THCAD_THQS_LASTAPPEND_SMALL",
                        "IDI_THCAD_THQS_LASTAPPEND_LARGE",
                        RibbonButtonStyle.LargeWithText);


                    // 上次选择
                    splitButton.AddNewButton("上次选择",
                        "按上次选择选择",
                        "THQS _PREVIOUS",
                        "上次选择",
                        "IDI_THCAD_THQS_LASTSELECT_SMALL",
                        "IDI_THCAD_THQS_LASTSELECT_LARGE",
                        RibbonButtonStyle.LargeWithText);
                }

                // 画线选文字
                subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton("画线选文字",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 相同文字选择
                    splitButton.AddNewButton("相同文字选择",
                        "天华相同文字选择",
                        "FINDTEXT2",
                        "天华相同文字选择",
                        "IDI_THCAD_FINDTEXT2_SMALL",
                        "IDI_THCAD_FINDTEXT2_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 包含文字选择
                    splitButton.AddNewButton("包含文字选择",
                        "天华包含文字选择",
                        "FINDTEXTINCLUDE2",
                        "天华包含文字选择",
                        "IDI_THCAD_FINDTEXTINCLUDE2_SMALL",
                        "IDI_THCAD_FINDTEXTINCLUDE2_LARGE",
                        RibbonButtonStyle.SmallWithText);
                }
            }
        }

        private static void CreateGeneralModifyPanel(RibbonTabSource tab)
        {
            // 通用修改
            var panel = tab.AddNewPanel("GENERALMODIFY", "通用修改");
            var row = panel.AddNewRibbonRow();

            {
                var subPanel = row.AddNewPanel();

                // 天华对齐
                var subRow = subPanel.AddNewRibbonRow();
                {
                    var splitButton = subRow.AddNewSplitButton("天华对齐",
                        RibbonSplitButtonBehavior.DropDownNoFollow,
                        RibbonSplitButtonListStyle.IconText,
                        RibbonButtonStyle.SmallWithText);

                    // 向上对齐
                    splitButton.AddNewButton("向上对齐",
                        "天华向上对齐",
                        "THAL _TOP",
                        "向上对齐",
                        "IDI_THCAD_THALIGN_TOP_SMALL",
                        "IDI_THCAD_THALIGN_TOP_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 向下对齐
                    splitButton.AddNewButton("向下对齐",
                        "天华向下对齐",
                        "THAL _BOTTOM",
                        "向下对齐",
                        "IDI_THCAD_THALIGN_BOTTOM_SMALL",
                        "IDI_THCAD_THALIGN_BOTTOM_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 向左对齐
                    splitButton.AddNewButton("向左对齐",
                        "天华向左对齐",
                        "THAL _LEFT",
                        "向左对齐",
                        "IDI_THCAD_THALIGN_LEFT_SMALL",
                        "IDI_THCAD_THALIGN_LEFT_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 向右对齐
                    splitButton.AddNewButton("向右对齐",
                        "天华向右对齐",
                        "THAL _RIGHT",
                        "向右对齐",
                        "IDI_THCAD_THALIGN_RIGHT_SMALL",
                        "IDI_THCAD_THALIGN_RIGHT_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 分割线
                    splitButton.AddNewSeparator(RibbonSeparatorStyle.Line);

                    // 水平居中
                    splitButton.AddNewButton("水平居中",
                        "天华水平居中",
                        "THAL _HORIZONTAL",
                        "水平居中",
                        "IDI_THCAD_THALIGN_HORIZONTAL_SMALL",
                        "IDI_THCAD_THALIGN_HORIZONTAL_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 垂直居中
                    splitButton.AddNewButton("垂直居中",
                        "天华垂直居中",
                        "THAL _VERTICAL",
                        "垂直居中",
                        "IDI_THCAD_THALIGN_VERTICAL_SMALL",
                        "IDI_THCAD_THALIGN_VERTICAL_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 分割线
                    splitButton.AddNewSeparator(RibbonSeparatorStyle.Line);

                    // 水平均分
                    splitButton.AddNewButton("水平均分",
                        "天华水平均分",
                        "THAL _XDISTRIBUTE",
                        "水平方向平均分布",
                        "IDI_THCAD_THALIGN_XDISTRIBUTE_SMALL",
                        "IDI_THCAD_THALIGN_XDISTRIBUTE_LARGE",
                        RibbonButtonStyle.SmallWithText);

                    // 垂直均分
                    splitButton.AddNewButton("垂直均分",
                        "天华垂直均分",
                        "THAL _YDISTRIBUTE",
                        "水平方向平均分布",
                        "IDI_THCAD_THALIGN_YDISTRIBUTE_SMALL",
                        "IDI_THCAD_THALIGN_YDISTRIBUTE_LARGE",
                        RibbonButtonStyle.SmallWithText);
                }

                // 天华复制
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("天华复制",
                    "天华复制",
                    "THCO",
                    "提供更灵活的均分和成倍复制",
                    "IDI_THCAD_THCP_SMALL",
                    "IDI_THCAD_THCP_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 批量缩放
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("批量缩放",
                    "天华批量缩放",
                    "THMSC",
                    "对多个选择对象以各自的开始点（插入点）为基准点进行批量比例缩放",
                    "IDI_THCAD_THMSC_SMALL",
                    "IDI_THCAD_THMSC_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 属性格式刷
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("属性格式刷",
                    "天华格式刷",
                    "THMA",
                    "将目标对象的某些属性刷取为源对象的对应属性",
                    "IDI_THCAD_THMA_SMALL",
                    "IDI_THCAD_THMA_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 数字批处理
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("数字批处理",
                    "天华数字批处理",
                    "WZJS",
                    "天华数字批处理",
                    "IDI_THCAD_WZJS_SMALL",
                    "IDI_THCAD_WZJS_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 尺寸避让
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("尺寸避让",
                    "天华尺寸避让",
                    "THDTA",
                    "调整交叉或重叠的标注文字以避免发生位置冲突",
                    "IDI_THCAD_THDTA_SMALL",
                    "IDI_THCAD_THDTA_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }

            {
                var subPanel = row.AddNewPanel();

                // 文字合并
                var subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("文字合并",
                    "天华文字合并",
                    "TEXTMERGE",
                    "天华文字合并",
                    "IDI_THCAD_TEXTMERGE_SMALL",
                    "IDI_THCAD_TEXTMERGE_LARGE",
                    RibbonButtonStyle.SmallWithText);

                // 云线提资
                subRow = subPanel.AddNewRibbonRow();
                subRow.AddNewButton("云线提资",
                    "天华云线提资",
                    "TH-TZ",
                    "天华云线提资",
                    "IDI_THCAD_THTZ_SMALL",
                    "IDI_THCAD_THTZ_LARGE",
                    RibbonButtonStyle.SmallWithText);
            }
        }
    }
}
