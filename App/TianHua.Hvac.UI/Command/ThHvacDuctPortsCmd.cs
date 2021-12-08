﻿using System;
using System.Linq;
using Linq2Acad;
using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPHVAC.Model;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDuctPortsCmd : IAcadCommand, IDisposable
    {
        private PortParam portParam;
        public ThHvacDuctPortsCmd() { }
        public ThHvacDuctPortsCmd(PortParam portParam)
        {
            this.portParam = portParam;
        }
        public void Dispose() { }

        public void Execute()
        {
            if (portParam.centerLines.Count == 0)
            {
                ThMEPHVACService.PromptMsg("无用于布置风口的中心线");
                return;
            }
            var excludeLines = GetExcludeLine();            
            var anayRes = new ThDuctPortsAnalysis(portParam, excludeLines);
            _ = new ThPortsDistribute(portParam, anayRes.endLinesInfos);
            anayRes.CreatePortDuctGeo();// 获得风口位置后再调用(同时获得管段间变径)
            anayRes.CreateReducing();
            var painter = new ThDuctPortsDraw(portParam);
            painter.Draw(anayRes);
        }
        private DBObjectCollection GetExcludeLine()
        {
            if (portParam.genStyle == GenerationStyle.Auto)
            {
                GetExcludeLine("请选择不布置风口的线", out DBObjectCollection excludeLines);
                if (excludeLines.Count >= portParam.centerLines.Count)
                {
                    ThMEPHVACService.PromptMsg("没有选择要布置风口的管段");
                    return new DBObjectCollection ();
                }
                ThDuctPortsDrawService.MoveToZero(portParam.srtPoint, excludeLines);
                return excludeLines;
            }
            return new DBObjectCollection();
        }
        private void GetExcludeLine(string prompt, out DBObjectCollection excludeLine)
        {
            using (var db = AcadDatabase.Active())
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = prompt,
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                excludeLine = new DBObjectCollection();
                if (result.Status == PromptStatus.OK)
                {
                    var objIds = result.Value.GetObjectIds();
                    excludeLine = objIds.Cast<ObjectId>().Select(o => o.GetDBObject().Clone() as Line).ToCollection();
                }
            }
        }
    }
}