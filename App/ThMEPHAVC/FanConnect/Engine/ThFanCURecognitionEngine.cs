using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Command;

namespace ThMEPHVAC.FanConnect.Engine
{
    public class ThFanCURecognitionEngine
    {
        public List<ThFanCUModel> Extract(Database database, int sysType)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var retModel = new List<ThFanCUModel>();
                var Results = acadDatabase.ModelSpace.OfType<BlockReference>();
                foreach (var blk in Results)
                {
                    var blkName = blk.GetEffectiveName();
                    if (sysType == 0)//水系统
                    {
                        if (blkName == "AI-FCU(两管制)" ||
                            blkName == "AI-FCU(四管制)" ||
                            blkName == "AI-吊顶式空调箱")
                        {
                            retModel.Add(ThFanConnectUtils.GetFanFromBlockReference(blk));
                        }
                        else if (blkName == "AI-水管断线")
                        {
                            retModel.Add(ThFanConnectUtils.GetFanFromBlockReference(blk));
                        }
                    }
                    else if (sysType == 1)//冷媒系统
                    {
                        if (blkName == "AI-中静压VRF室内机(风管机)" ||
                            blkName == "AI-VRF室内机(四面出风型)")
                        {
                            retModel.Add(ThFanConnectUtils.GetFanFromBlockReference(blk));
                        }
                    }
                }
                return retModel;
            }
        }
    }
}
