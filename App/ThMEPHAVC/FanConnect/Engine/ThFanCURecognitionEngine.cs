using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Command;

namespace ThMEPHVAC.FanConnect.Engine
{
    public class ThFanCURecognitionEngine
    {
        public List<ThFanCUModel> ExtractEditor(int sysType)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                return Active.Editor.FilterBlocks(BlockNames(sysType))
                    .Select(o => acadDatabase.ElementOrDefault<BlockReference>(o))
                    .Where(o => o != null)
                    .Select(o => ThFanConnectUtils.GetFanFromBlockReference(o))
                    .ToList();
            }
        }

        private List<string> BlockNames(int sysType)
        {
            if (sysType == 0)//水系统
            {
                return new List<string> {
                    "AI-水管断线",
                    "AI-FCU(两管制)",
                    "AI-FCU(四管制)",
                    "AI-吊顶式空调箱",
                };
            }
            else if (sysType == 1)//冷媒系统
            {
                return new List<string> {
                    "AI-VRF室内机(四面出风型)",
                    "AI-中静压VRF室内机(风管机)",
                };
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
