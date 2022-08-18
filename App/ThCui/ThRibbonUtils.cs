using System.Linq;
using Autodesk.Windows;
using System.Collections.Generic;
using ThCADExtension;
using Dreambuild.AutoCAD;

namespace TianHua.AutoCAD.ThCui
{
    public class ThRibbonUtils
    {
        public static RibbonTab Tab
        {
            get
            {
                if (ComponentManager.Ribbon == null)
                {
                    return null;
                }
                foreach (RibbonTab tab in ComponentManager.Ribbon.Tabs)
                {
                    if (tab.Name == ThCADCommon.RibbonTabName &&
                        tab.Title == ThCADCommon.RibbonTabTitle)
                    {
                        return tab;
                    }
                }
                return null;
            }
        }

        public static void OpenAllPanels()
        {
            if (Tab == null)
            {
                return;
            }

            // 登入状态，开启所有面板
            Tab.Panels.ForEach(o => o.IsEnabled = true);
            foreach (var panel in Tab.Panels.Where(o => o.UID == "pnl" + "Help"))
            {
                // 隐藏登陆按钮，显示退出按钮
                foreach (RibbonRowPanel rowPanel in panel.Source.Items)
                {
                    rowPanel.Items.Where(o => o.Text == "专业切换").ForEach(o => o.IsVisible = true);
                }
            }
        }

        public static void CloseAllPanels()
        {
            if (Tab == null)
            {
                return;
            }

            // 登出状态，关闭所有面板
            Tab.Panels.ForEach(o => o.IsEnabled = false);
            // 登出状态，隐藏所有面板
            Tab.Panels.ForEach(o => o.IsVisible = false);
            foreach (var panel in Tab.Panels.Where(o => o.UID == "pnl" + "Help"))
            {
                // 开启“登陆”Panel
                panel.IsEnabled = true;
                // 显示“登陆”Panel
                panel.IsVisible = true;
                // 显示登陆按钮，隐藏退出按钮
                foreach (RibbonRowPanel rowPanel in panel.Source.Items)
                {
                    rowPanel.Items.Where(o => o.Text == "专业切换").ForEach(o => o.IsVisible = false);
                }
            }
        }

        public static void ConfigPanelsWithProfile(string profile)
        {
            if (Tab == null)
            {
                return;
            }
            var panels = Tab.Panels;
            foreach (var panel in panels.Where(o => o.UID == "pnl" + "Help"))
            {
                foreach (RibbonRowPanel rowPanel in panel.Source.Items)
                {
                    rowPanel.Items.Where(o => o.Text == "专业切换").ForEach(o =>
                    {
                        if (o is RibbonSplitButton splitButton)
                        {
                            IEnumerable<RibbonItem> items = null;
                            switch (profile)
                            {
                                case "A":
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THMEPPROFILE _A");
                                    break;
                                case "S":
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THMEPPROFILE _S");
                                    break;
                                case "H":
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THMEPPROFILE _H");
                                    break;
                                case "E":
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THMEPPROFILE _E");
                                    break;
                                case "W":
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THMEPPROFILE _W");
                                    break;
                                default:
                                    break;
                            };

                            foreach (RibbonItem item in items)
                            {
                                splitButton.Current = item;
                            }
                        }
                    });
                }
            }
            ThCuiProfileYamlParser parser = new ThCuiProfileYamlParser(profile);
            panels.ForEach(o => parser.UpdateIsVisible(o));
        }
    }
}
