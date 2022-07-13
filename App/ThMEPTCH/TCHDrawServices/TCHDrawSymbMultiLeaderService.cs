using System.Collections.Generic;
using System.IO;
using ThMEPTCH.Data;
using ThMEPTCH.Model;
using ThMEPTCH.TCHTables;
namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawSymbMultiLeaderService : TCHDrawServiceBase
    {
        protected override string CmdName => "TH2T20";
        List<ThTCHSymbMultiLeader> ThTCHSymbMultiLeaders;
        public TCHDrawSymbMultiLeaderService()
        {
            TCHDBPath = Path.GetTempPath() + "TG20.db";
            ClearDataTables.Add("TwtPoint");
            ClearDataTables.Add("TwtVector");
            ClearDataTables.Add("TwtBlock");
            ClearDataTables.Add("TwtEquipment");
        }
        public void InitDimensions(List<ThTCHSymbMultiLeader> thTCHSymbMultiLeaders)
        {
            ThTCHSymbMultiLeaders = new List<ThTCHSymbMultiLeader>();
            if (null == thTCHSymbMultiLeaders || thTCHSymbMultiLeaders.Count < 1)
                return;
            foreach (var item in thTCHSymbMultiLeaders)
            {
                if (item == null)
                    continue;
                ThTCHSymbMultiLeaders.Add(item);
            }
        }
        protected override void WriteModelToTCHDatabase()
        {
            if (null == ThTCHSymbMultiLeaders || ThTCHSymbMultiLeaders.Count < 1)
                return;
            ulong id = 10000;
            foreach (var item in ThTCHSymbMultiLeaders)
            {
                var point = ThSQLHelper.PointToTwtPointModel(id, item.Point);
                var dimen = GetDim(id, point.ID, point.ID, item);
                WriteModelToTCH(point, ThMEPTCHCommon.TCHTableName_TgSymbMultiLeader, ref id);
            }
        }
        TCHTgSymbMultiLeader GetDim(ulong pipeId, ulong startPtId, ulong endPtId, ThTCHSymbMultiLeader item)
        {
            TCHTgSymbMultiLeader thTCHSymbMultiLeader = new TCHTgSymbMultiLeader
            {
                ID = ((int)pipeId),
                DocScale = item.DocScale,
                LeaderPtID = item.LeaderPtID,
                Layer = item.Layer,
                TextStyle = item.TextStyle,
                UpText = item.UpText,
                DownText = item.DownText,
                AlignType = item.AlignType,
                ArrowType = item.ArrowType,
                ArrowSize = item.ArrowSize,
                TextHeight = item.TextHeight,
                BaseRatio = item.BaseRatio,
                BaseLen = item.BaseLen,
                IsParallel = item.IsParallel,
                IsMask = item.IsMask,
                VertexesPointStartID = item.VertexesPointStartID,
            };

            return thTCHSymbMultiLeader;
        }
    }
}
