using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThMEPTCH.Data;
using ThMEPTCH.Moel;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawSprinklerService : TCHDrawServiceBase
    {
        List<ThTCHSprinkler> drawTCHSprinklers;
        public TCHDrawSprinklerService() 
        {
            TCHDBPath = Path.GetTempPath() + "TG20.db";
            ClearDataTables.Add("TwtPoint");
            ClearDataTables.Add("TwtVector");
            ClearDataTables.Add("TwtBlock");
            ClearDataTables.Add("TwtEquipment");
        }

        protected override string CmdName => "TH2T20";

        public void InitData(List<ThTCHSprinkler> sprinklers)
        {
            drawTCHSprinklers = new List<ThTCHSprinkler>();
            if (null == sprinklers || sprinklers.Count < 1)
                return;
            drawTCHSprinklers.AddRange(sprinklers);
        }
        protected override void WriteModelToTCHDatabase()
        {
            if (null == drawTCHSprinklers || drawTCHSprinklers.Count() < 1)
                return;
            ulong id = 10000;
            foreach (var sprinkler in drawTCHSprinklers)
            {
                var twtPoint = ThSQLHelper.PointToTwtPointModel(id, sprinkler.Location);
                WriteModelToTCH(twtPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref id);
                var twtSprinkler= SprinklerToTCHEquipment(id, twtPoint.ID,sprinkler);
                WriteModelToTCH(twtSprinkler, ThMEPTCHCommon.TCHTableName_TwtSprinkler, ref id);
            }
        }
        TCHTwtSprinkler SprinklerToTCHEquipment(ulong Id,ulong loctionId,ThTCHSprinkler sprinkler)
        {
            TCHTwtSprinkler equipment = new TCHTwtSprinkler
            {
                ID = Id,
                LocationID = loctionId,
                System = string.IsNullOrEmpty(sprinkler.System)? "喷淋": sprinkler.System,
                HidePipe = sprinkler.HidePipe,
                Type = sprinkler.Type,
                LinkMode = sprinkler.LinkMode,
                PipeDN = sprinkler.PipeDn,
                K = sprinkler.K,
                Angle = sprinkler.Angle,
                PipeLength = sprinkler.PipeLength,
                SizeX = sprinkler.SizeX,
                SizeY = sprinkler.SizeY,
                DocScale = sprinkler.DocScale,
                MirrorByY = sprinkler.MirrorByY,
                MirrorByX = sprinkler.MirrorByY,
            };
            return equipment;
        }
    }
}
