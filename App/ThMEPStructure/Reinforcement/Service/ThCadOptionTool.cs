using System.Collections.Generic;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPStructure.Reinforcement.Service
{
    public static class ThCadOptionTool
    {
        private static AcadPreferences preferences;
        private static AcadPreferencesProfiles profiles;
        static ThCadOptionTool()
        {
            preferences = Application.Preferences as AcadPreferences;
            if(preferences!=null)
            {
                profiles = preferences.Profiles;
            }
        }
        public static string GetActiveProfile()
        {
            if(profiles != null)
            {
                var profiles = preferences.Profiles;
                return profiles.ActiveProfile;
            }
            else
            {
                return "";
            }
        }

        public static void SetActiveProfile(string profile)
        {
            if(profiles != null && HasProfile(profile))
            {
                var profiles = preferences.Profiles;
                profiles.ActiveProfile = profile;
            }         
        }

        private static bool HasProfile(string profile)
        {
            object profileNames = null;
            var upperProfile = profile.ToUpper();
            profiles.GetAllProfileNames(out profileNames);
            if(profileNames.GetType() == typeof(string[]))
            {
                string[] values = profileNames as string[];
                foreach(string value in values)
                {
                    if(value.ToUpper() == upperProfile)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
