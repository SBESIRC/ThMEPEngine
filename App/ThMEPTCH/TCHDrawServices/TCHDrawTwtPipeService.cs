using System;
using System.IO;
using System.Collections.Generic;

using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawTwtPipeService : TCHDrawServiceBase
    {
        public List<ThTCHTwtPipe> Pipes;
        public List<ThTCHTwtPipeValve> Valves;

        protected override string CmdName => "TH2T20";

        public TCHDrawTwtPipeService()
        {
            Pipes = new List<ThTCHTwtPipe>();
            Valves = new List<ThTCHTwtPipeValve>();
            TCHDBPath = Path.GetTempPath() + "TG20W.db";
            TCHTemplateDBPath = ThCADCommon.TCHWSSDBPath();
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtPoint);
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtVector);
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtPipeDimStyle);
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtPipe);
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtBlock);
            ClearDataTables.Add(ThMEPTCHCommon.TCHTableName_TwtPipeValve);
        }

        protected override void WriteModelToTCHDatabase()
        {
            ulong objectId = 2000000;

            foreach (var pipe in Pipes)
            {
                WritePipeToTCH(pipe, ref objectId);
            }

            foreach (var valve in Valves)
            {
                var location = CreateTwtPoint(valve.LocationID, objectId);
                WriteModelToTCH(location, ThMEPTCHCommon.TCHTableName_TwtPoint, ref objectId);
                var direction = CreateTwtVector(valve.DirectionID, objectId);
                WriteModelToTCH(direction, ThMEPTCHCommon.TCHTableName_TwtVector, ref objectId);
                var block = CreateTwtBlock(valve.BlockID, objectId);
                WriteModelToTCH(block, ThMEPTCHCommon.TCHTableName_TwtBlock, ref objectId);

                var tchValve = CreateTwtPipeValve(valve, objectId, location.ID, direction.ID, block.ID);
                WriteModelToTCH(tchValve, ThMEPTCHCommon.TCHTableName_TwtPipeValve, ref objectId);
            }
        }

        private ulong WritePipeToTCH(ThTCHTwtPipe pipe, ref ulong objectId)
        {
            var startPoint = CreateTwtPoint(pipe.StartPtID, objectId);
            WriteModelToTCH(startPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref objectId);
            var endPoint = CreateTwtPoint(pipe.EndPtID, objectId);
            WriteModelToTCH(endPoint, ThMEPTCHCommon.TCHTableName_TwtPoint, ref objectId);

            var dimStyle = CreateTwtPipeDimStyle(pipe.DimID, objectId);
            WriteModelToTCH(dimStyle, ThMEPTCHCommon.TCHTableName_TwtPipeDimStyle, ref objectId);

            var tchPipe = CreateTwtPipe(pipe, objectId, startPoint.ID, endPoint.ID, dimStyle.ID);
            WriteModelToTCH(tchPipe, ThMEPTCHCommon.TCHTableName_TwtPipe, ref objectId);

            return tchPipe.ID;
        }

        private TCHTwtPoint CreateTwtPoint(ThTCHTwtPoint point, ulong id)
        {
            return new TCHTwtPoint
            {
                ID = id,
                X = point.Point.X.ToString(),
                Y = point.Point.Y.ToString(),
                Z = point.Point.Z.ToString(),
            };
        }

        private TCHTwtVector CreateTwtVector(ThTCHTwtVector direction, ulong id)
        {
            return new TCHTwtVector
            {
                ID = id,
                X = direction.Vector.X.ToString(),
                Y = direction.Vector.Y.ToString(),
                Z = direction.Vector.Z.ToString(),
            };
        }

        private TCHTwtBlock CreateTwtBlock(ThTCHTwtBlock block, ulong id)
        {
            return new TCHTwtBlock
            {
                ID = id,
                Type = block.Type,
                Number = block.Number,
            };
        }

        private TCHTwtPipeDimStyle CreateTwtPipeDimStyle(ThTCHTwtPipeDimStyle dimId, ulong id)
        {
            return new TCHTwtPipeDimStyle
            {
                ID = id,
                ShowDim = Convert.ToInt32(dimId.ShowDim),
                DnStyle = Convert.ToInt32(dimId.DnStyle),
                GradientStyle = Convert.ToInt32(dimId.GradientStyle),
                LengthStyle = Convert.ToInt32(dimId.LengthStyle),
                ArrangeStyle = Convert.ToInt32(dimId.ArrangeStyle),
                DelimiterStyle = Convert.ToInt32(dimId.DelimiterStyle),
                SortStyle = Convert.ToInt32(dimId.SortStyle),
            };
        }

        private TCHTwtPipe CreateTwtPipe(ThTCHTwtPipe pipe, ulong pipeId, ulong startPointId, ulong endPointId, ulong dimStyleId)
        {
            return new TCHTwtPipe
            {
                ID = pipeId,
                StartPtID = startPointId,
                EndPtID = endPointId,
                System = pipe.System,
                Material = pipe.Material,
                DnType = pipe.DnType,
                Dn = pipe.Dn,
                Gradient = pipe.Gradient,
                Weight = pipe.Weight,
                HideLevel = pipe.HideLevel,
                DocScale = pipe.DocScale,
                DimID = dimStyleId,
            };
        }

        private TCHTwtPipeValve CreateTwtPipeValve(ThTCHTwtPipeValve valve, ulong valveId, ulong pointId, ulong vectorId, ulong blockId)
        {
            // 经测试插入阀门时可不指定管线Id
            return new TCHTwtPipeValve
            {
                ID = valveId,
                LocationID = pointId,
                DirectionID = vectorId,
                BlockID = blockId,
                //PipeID = pipeId,
                System = valve.System,
                Length = valve.Length,
                Width = valve.Width,
                InterruptWidth = valve.InterruptWidth,
                DocScale = valve.DocScale,
            };
        }
    }
}
