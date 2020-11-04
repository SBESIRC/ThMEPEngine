using System;
using System.Linq;
using Autodesk.AutoCAD.Interop;
using System.Collections.Generic;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThCADExtension;
using Dreambuild.AutoCAD;

namespace TianHua.AutoCAD.ThCui
{
    public class ThToolbarUtils
    { 
        public static readonly Dictionary<Profile, string> Profiles = new Dictionary<Profile, string>()
        {
            { Profile.WSS, ThCuiCommon.PROFILE_WSS },
            { Profile.HAVC, ThCuiCommon.PROFILE_HAVC },
            { Profile.STRUCTURE, ThCuiCommon.PROFILE_STRUCTURE },
            { Profile.ELECTRICAL, ThCuiCommon.PROFILE_ELECTRICAL },
            { Profile.CONSTRUCTION, ThCuiCommon.PROFILE_CONSTRUCTION },
            { Profile.ARCHITECTURE, ThCuiCommon.PROFILE_ARCHITECTURE },
        };

        public static AcadToolbars Toolbars
        {
            get
            {

#if ACAD_ABOVE_2014
                //  2016启动时可能进入Zero doc state，
                //  这时候获取MenuGroups会抛出COM Exception
                //  http://help.autodesk.com/view/ACD/2016/ENU/?guid=GUID-CB7D7DC2-C8C1-4EF9-A638-C4C6184BFC85
                if (AcadApp.DocumentManager.Count == 0)
                {
                    return null;
                }
#endif
                var acadApp = AcadApp.AcadApplication as AcadApplication;
                foreach (AcadMenuGroup menuGroup in acadApp.MenuGroups)
                {
                    if (string.Equals(menuGroup.Name,
                        ThCADCommon.CuixMenuGroup,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return menuGroup.Toolbars;
                    }
                }
                return null;
            }
        }

        public static void OpenAllToolbars()
        {
            var toolbars = Toolbars;
            if (toolbars == null)
            {
                return;
            }

            try
            {
                toolbars.Cast<AcadToolbar>().Where(o => o.Name == ThCuiCommon.PROFILE_GENERAL).ForEach(o => o.Visible = true);
                foreach (var item in Profiles)
                {
                    toolbars.Cast<AcadToolbar>().Where(o => o.Name == item.Value).ForEach(o => o.Visible = true);
                }
            }
            catch
            {
                // 在某些CAD 2012环境下，启动CAD后装载效率工具时，这里会抛出异常。
                // 具体原因未知，这里只是暂时捕捉异常，避免CAD崩溃
            }
        }

        public static void CloseAllToolbars()
        {
            var toolbars = Toolbars;
            if (toolbars == null)
            {
                return;
            }

            try
            {
                toolbars.Cast<AcadToolbar>().Where(o => o.Name == ThCuiCommon.PROFILE_GENERAL).ForEach(o => o.Visible = false);
                foreach (var item in Profiles)
                {
                    toolbars.Cast<AcadToolbar>().Where(o => o.Name == item.Value).ForEach(o => o.Visible = false);
                }
            }
            catch
            {
                // 在某些CAD 2012环境下，启动CAD后装载效率工具时，这里会抛出异常。
                // 具体原因未知，这里只是暂时捕捉异常，避免CAD崩溃
            }
        }

        public static void ConfigToolbarsWithCurrentProfile()
        {
            var toolbars = Toolbars;
            if (toolbars == null)
            {
                return;
            }

            try
            {
                toolbars.Cast<AcadToolbar>().Where(o => o.Name == ThCuiCommon.PROFILE_GENERAL).ForEach(o => o.Visible = true);
                foreach (var item in Profiles)
                {
                    if (item.Key == ThCuiProfileManager.Instance.CurrentProfile)
                    {
                        toolbars.Cast<AcadToolbar>().Where(o => o.Name == item.Value).ForEach(o => o.Visible = true);
                    }
                    else
                    {
                        toolbars.Cast<AcadToolbar>().Where(o => o.Name == item.Value).ForEach(o => o.Visible = false);
                    }
                }
            }
            catch
            {
                // 在某些CAD 2012环境下，启动CAD后装载效率工具时，这里会抛出异常。
                // 具体原因未知，这里只是暂时捕捉异常，避免CAD崩溃
            }
        }

        public static void ConfigToolbarsWithCurrentUser()
        {
            ConfigToolbarsWithCurrentProfile();
            //if (ThIdentityService.IsLogged())
            //{
            //    ConfigToolbarsWithCurrentProfile();
            //}
            //else
            //{
            //    CloseAllToolbars();
            //}
        }
    }
}