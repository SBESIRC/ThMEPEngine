using Microsoft.Win32;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThRegistryTool
    {
        public static string GetLocalMachineRegistryValue(string path,string paramName)
        {
            return GetRegistryValue(Registry.LocalMachine,path,paramName);
        }

        public static string GetCurrentUserRegistryValue(string path, string paramName)
        {
            return GetRegistryValue(Registry.CurrentUser, path, paramName);
        }

        public static string GetClassesRootRegistryValue(string path, string paramName)
        {
            return GetRegistryValue(Registry.ClassesRoot, path, paramName);
        }

        public static string GetCurrentConfigRegistryValue(string path, string paramName)
        {
            return GetRegistryValue(Registry.CurrentConfig, path, paramName);
        }

        public static string GetUsersRegistryValue(string path, string paramName)
        {
            return GetRegistryValue(Registry.Users, path, paramName);
        }

        private static string GetRegistryValue(RegistryKey root,string path, string paramName)
        {
            string value = string.Empty;
            if(root==null || string.IsNullOrEmpty(path) || string.IsNullOrEmpty(paramName))
            {
                return value;
            }
            RegistryKey rk = root.OpenSubKey(path);
            if (rk != null)
            {
                value = (string)rk.GetValue(paramName, null);
            }
            return value;
        }
    }
}
