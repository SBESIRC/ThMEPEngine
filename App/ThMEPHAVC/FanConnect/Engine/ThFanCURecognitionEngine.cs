using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Engine
{
    public class ThFanCURecognitionEngine
    {
        public List<ThFanCUModel> Extract(Database database,int sysType)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var retModel = new List<ThFanCUModel>();
                var Results = acadDatabase.ModelSpace.OfType<BlockReference>();
                foreach (var blk in Results)
                {
                    if(sysType == 0)//水系统
                    {
                        if(blk.GetEffectiveName() == "AI-FCU(两管制)" || blk.GetEffectiveName() == "AI-FCU(四管制)"
                            || blk.GetEffectiveName() == "AI-吊顶式空调箱")
                        {
                            retModel.Add(ThFanConnectUtils.GetFanFromBlockReference(blk));
                        }
                        else if(blk.GetEffectiveName() == "AI-水管断线")
                        {
                            retModel.Add(ThFanConnectUtils.GetFanFromBlockReference(blk));
                        }
                    }
                    else if( sysType == 1)//冷媒系统
                    {
                        if(blk.GetEffectiveName() == "AI-中静压VRF室内机(风管机)" || blk.GetEffectiveName() == "AI-VRF室内机(四面出风型)")
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
