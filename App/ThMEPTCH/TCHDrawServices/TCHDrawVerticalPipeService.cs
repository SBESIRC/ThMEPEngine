using System.Collections.Generic;
using System.IO;
using ThMEPTCH.Data;
using ThMEPTCH.Model;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawVerticalPipeService : TCHDrawServiceBase
    {
        protected override string CmdName => "TH2T20";
        List<ThTCHVerticalPipe> thPipes;
        public TCHDrawVerticalPipeService()
        {
            TCHDBPath = Path.GetTempPath() + "TG20.db";
            ClearDataTables.Add("TwtPoint");
            ClearDataTables.Add("TwtVerticalPipe");
        }
        public void InitPipe(List<ThTCHVerticalPipe> tchVerticalPipes) 
        {
            thPipes = new List<ThTCHVerticalPipe>();
            if (null == tchVerticalPipes || tchVerticalPipes.Count < 1)
                return;
            foreach (var item in tchVerticalPipes) 
            {
                if (item == null)
                    continue;
                thPipes.Add(item);
            }
        }
        protected override void WriteModelToTCHDatabase()
        {
            if (null == thPipes || thPipes.Count < 1)
                return;
            ulong id = 10000;
            foreach (var item in thPipes) 
            {
                var twtBtPoint = ThSQLHelper.PointToTwtPointModel(id, item.PipeBottomPoint);
                WriteModelToTCH(twtBtPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref id);
                var twtTopPoint = ThSQLHelper.PointToTwtPointModel(id, item.PipeTopPoint);
                WriteModelToTCH(twtTopPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref id);
                var pipeModel = GetTCHPipe(id, twtBtPoint.ID, twtTopPoint.ID, item);
                WriteModelToTCH(pipeModel, ThMEPTCHCommon.TCHTableName_TwtVerticalPipe, ref id);

                if (string.IsNullOrEmpty(item.TextStyle))
                    continue;
                var dimTurnPoint = ThSQLHelper.PointToTwtPointModel(id, item.TurnPoint);
                WriteModelToTCH(dimTurnPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref id);
                var dimDir = ThSQLHelper.VectorToTwtVectorModel(id, item.TextDirection);
                WriteModelToTCH(dimDir, ThMEPTCHCommon.TCHTableName_TwtVector, ref id);
                var pipeDimModel = GetTCHPipeDim(id, pipeModel.ID, pipeModel.StartPtID, dimTurnPoint.ID, dimDir.ID, item);
                WriteModelToTCH(pipeDimModel, ThMEPTCHCommon.TCHTableName_TwtVerticalPipeDim, ref id);
            }
        }
        TCHTwtVerticalPipe GetTCHPipe(ulong pipeId, ulong startPtId, ulong endPtId,ThTCHVerticalPipe verticalPipe) 
        {
            TCHTwtVerticalPipe twtVerticalPipe = new TCHTwtVerticalPipe
            {
                ID = pipeId,
                StartPtID = startPtId,
                EndPtID = endPtId,
                System = verticalPipe.PipeSystem,
                Material = verticalPipe.PipeMaterial,
                DnType = verticalPipe.DnType,
                Dn = verticalPipe.PipeDN,
                FloorNumber =verticalPipe.FloorNum,
                ShowDn = 0.5,
                Number = verticalPipe.PipeNum,
                DocScale = verticalPipe.DocScale,
            };
            return twtVerticalPipe;
        }
        TCHTwtVerticalPipeDim GetTCHPipeDim(ulong dimId,ulong pipeId,ulong startPtId,ulong turnPtId,ulong dirId, ThTCHVerticalPipe verticalPipe) 
        {
            TCHTwtVerticalPipeDim pipeDim = new TCHTwtVerticalPipeDim
            {
                ID = dimId,
                StartPtID = startPtId,
                VPipeID = pipeId,
                DirectionID = dirId,
                TurnPtID = turnPtId,
                System = verticalPipe.PipeSystem,
                TextStyle = verticalPipe.TextStyle,
                TextHeight = verticalPipe.TextHeight,
                DimTypeText = verticalPipe.DimTypeText,
                FloorNum = verticalPipe.FloorNum,
                PipeNum = verticalPipe.PipeNum,
                FloorType = verticalPipe.FloorType,
                DimType = verticalPipe.DimType,
                DocScale = verticalPipe.DocScale,
                Spacing = verticalPipe.Spacing,
                Radius = verticalPipe.DimRadius,
            };
            return pipeDim;
        }
    }
}
