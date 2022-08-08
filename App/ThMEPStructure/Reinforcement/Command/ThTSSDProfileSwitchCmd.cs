using System;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Command
{
    /// <summary>
    /// 切换探索者Profile
    /// </summary>
    public class ThTSSDProfileSwitchCmd : ThMEPBaseCommand, IDisposable
    {
        public readonly static string TSDPProfileName = "TSDP2022";
        private static string initProfileName = "";
        public ThTSSDProfileSwitchCmd()
        {
            ActionName = "切换TSDP";
            CommandName = "THTSDP";            
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            if(string.IsNullOrEmpty(initProfileName))
            {
                if (IsTSSDProfileName())
                {
                    return;
                }
                else
                {
                    initProfileName = ThCadOptionTool.GetActiveProfile();
                    ThCadOptionTool.SetActiveProfile(TSDPProfileName);
                }
            }
            else
            {
                var activeProfile = ThCadOptionTool.GetActiveProfile();
                if(activeProfile == initProfileName)
                {
                    return;
                }
                else
                {
                    ThCadOptionTool.SetActiveProfile(initProfileName);
                    initProfileName = "";
                }
            }
        }

        private bool IsTSSDProfileName()
        {
            var activeProfile = ThCadOptionTool.GetActiveProfile();
            return TSDPProfileName.ToUpper() == activeProfile.ToUpper();
        }
    }
}
