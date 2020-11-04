using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices.PreferencesFiles;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThCADExtension;

namespace TianHua.AutoCAD.ThCui
{
    public class ThToolPaletteUtils
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

        public static void ConfigToolPaletteWithCurrentProfile()
        {
            foreach (var item in Profiles)
            {
                string path = null;
                switch (item.Key)
                {
                    case Profile.ELECTRICAL:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_ELECTRICAL);
                        break;
                    case Profile.WSS:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_WSS);
                        break;
                    case Profile.HAVC:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_HAVC);
                        break;
                    default:
                        path = "";
                        break;
                }
                if (path.IsNullOrEmpty())
                {
                    continue;
                }

                var paths = new ToolPalettePath(AcadApp.Preferences);
                if (item.Key == ThCuiProfileManager.Instance.CurrentProfile)
                {
                    if (!paths.Contains(path))
                    {
                        paths.Add(path);
                        paths.SaveChanges();
                    }
                }
                else
                {
                    if (paths.Contains(path))
                    {
                        paths.Remove(path);
                        paths.SaveChanges();
                    }
                }
            }
        }

        public static void RemoveAllToolPalettes()
        {
            foreach (var item in Profiles)
            {
                string path = null;
                switch (item.Key)
                {
                    case Profile.ELECTRICAL:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_ELECTRICAL);
                        break;
                    case Profile.WSS:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_WSS);
                        break;
                    case Profile.HAVC:
                        path = Path.Combine(ThCADCommon.ToolPalettePath(), ThCuiCommon.PATH_HAVC);
                        break;
                    default:
                        path = "";
                        break;
                }
                if (path.IsNullOrEmpty())
                {
                    continue;
                }

                var paths = new ToolPalettePath(AcadApp.Preferences);
                if (paths.Contains(path))
                {
                    paths.Remove(path);
                    paths.SaveChanges();
                }
            }
        }
    }
}
