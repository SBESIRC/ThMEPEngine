using System;
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
            ClearDataTables.Add("TgPublicList");
            ClearDataTables.Add("TgPoint");
            ClearDataTables.Add("TgSymbMultiLeader");
        }
        public void Init(List<ThTCHSymbMultiLeader> thTCHSymbMultiLeaders)
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
            ulong id = 4000000;
            foreach (var item in ThTCHSymbMultiLeaders)
            {
                var point1 = ThSQLHelper.PointToTCHTgPoint(id, item.BasePoint);
                WriteModelToTCH(point1, ThMEPTCHCommon.TCHTableName_TgPoint, ref id);
                var array1 = ThSQLHelper.ConvertToTCHTgPublicList(id, point1.ID, -1);
                WriteModelToTCH(array1, ThMEPTCHCommon.TCHTableName_TgPublicList, ref id);
                var point2 = ThSQLHelper.PointToTCHTgPoint(id, item.TextLineLocPoint);
                WriteModelToTCH(point2, ThMEPTCHCommon.TCHTableName_TgPoint, ref id);
                var array2 = ThSQLHelper.ConvertToTCHTgPublicList(id, point2.ID, array1.ID);
                WriteModelToTCH(array2, ThMEPTCHCommon.TCHTableName_TgPublicList, ref id);
                var symbMultiLeader = GetSymbMultiLeader(id, point2.ID, array2.ID, item);
                WriteModelToTCH(symbMultiLeader, ThMEPTCHCommon.TCHTableName_TgSymbMultiLeader, ref id);
            }
        }
        TCHTgSymbMultiLeader GetSymbMultiLeader(ulong id, int leaderPtID, int vertexesPointStartID, ThTCHSymbMultiLeader item)
        {
            TCHTgSymbMultiLeader thTCHSymbMultiLeader = new TCHTgSymbMultiLeader
            {
                ID = ((int)id),
                LeaderPtID = ((int)leaderPtID),
                VertexesPointStartID = ((int)vertexesPointStartID),
                DocScale = item.DocScale,
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
                LayoutRotation=item.LayoutRotation,
            };
            return thTCHSymbMultiLeader;
        }
    }
}
