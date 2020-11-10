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
                foreach(RibbonRowPanel rowPanel in panel.Source.Items)
                {
                    rowPanel.Items.Where(o => o.Text == "专业切换").ForEach(o => o.IsVisible = true);
                    //rowPanel.Items.Where(o => o.Id == "ID_THLOGIN").ForEach(o => o.IsVisible = false);
                    //rowPanel.Items.Where(o => o.Id == "ID_THLOGOUT").ForEach(o => o.IsVisible = true);
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
                    //rowPanel.Items.Where(o => o.Id == "ID_THLOGIN").ForEach(o => o.IsVisible = true);
                    //rowPanel.Items.Where(o => o.Id == "ID_THLOGOUT").ForEach(o => o.IsVisible = false);
                }
            }
        }

        public static void ConfigPanelsWithCurrentUser()
        {
            OpenAllPanels();
            //if (ThIdentityService.IsLogged())
            //{
            //    OpenAllPanels();
            //}
            //else
            //{
            //    CloseAllPanels();
            //}
        }

        public static void ConfigPanelsWithCurrentProfile()
        {
            if(Tab==null)
            {
                return;
            }
            var panels = Tab.Panels;
            Profile profile = ThCuiProfileManager.Instance.CurrentProfile;
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
                                case Profile.ARCHITECTURE:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _P");
                                    break;
                                case Profile.CONSTRUCTION:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _A");
                                    break;
                                case Profile.STRUCTURE:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _S");
                                    break;
                                case Profile.HAVC:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _H");
                                    break;
                                case Profile.ELECTRICAL:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _E");
                                    break;
                                case Profile.WSS:
                                    items = splitButton.Items.Where(bt => bt.Id == "ID_THPROFILE _W");
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
