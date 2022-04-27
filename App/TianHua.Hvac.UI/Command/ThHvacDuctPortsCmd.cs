﻿using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using DotNetARX;
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
        private string curDbPath;
        private PortParam portParam;
        private ThDuctPortsDrawService service;
        private Dictionary<Polyline, ObjectId> allFansDic;
        public ThHvacDuctPortsCmd() { }
        public ThHvacDuctPortsCmd(string curDbPath, PortParam portParam, Dictionary<Polyline, ObjectId> allFansDic)
        {
            service = new ThDuctPortsDrawService(portParam.param.scenario, portParam.param.scale);
            this.curDbPath = curDbPath;
            this.portParam = portParam;
            this.allFansDic = allFansDic;
        }
        public void Dispose() { }
        public void Execute()
        {

        }
        public void Execute(ref ulong gId, ObjectIdList brokenLineIds)
        {
            if (portParam.centerLines.Count == 0)
            {
                ThMEPHVACService.PromptMsg("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点!!!");
                return;
            }
            var excludeLines = GetExcludeLine(brokenLineIds);
            var anayRes = new ThDuctPortsAnalysis(portParam, excludeLines, allFansDic);
            _ = new ThPortsDistribute(portParam, anayRes.endLinesInfos);
            anayRes.CreatePortDuctGeo();// 获得风口位置后再调用(同时获得管段间变径)
            anayRes.CreateReducing();
            var painter = new ThDuctPortsDraw(portParam, curDbPath, service);
            painter.Draw(anayRes, ref gId);
        }
        private DBObjectCollection GetExcludeLine(ObjectIdList brokenLineIds)
        {
            if (portParam.genStyle == GenerationStyle.Auto)
            {
                var excludeIds = new HashSet<ObjectId>();
                brokenLineIds.ForEach(id => excludeIds.Add(id));
                GetExcludeLine("请选择不布置风口的线", excludeIds, out DBObjectCollection excludeLines);
                if (excludeLines.Count >= portParam.centerLines.Count)
                {
                    ThMEPHVACService.PromptMsg("没有选择要布置风口的管段");
                    return new DBObjectCollection();
                }
                ThDuctPortsDrawService.MoveToZero(portParam.srtPoint, excludeLines);
                return excludeLines;
            }
            return new DBObjectCollection();
        }
        private void GetExcludeLine(string prompt, HashSet<ObjectId> excludeIds, out DBObjectCollection excludeLine)
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
                    excludeLine = objIds.Cast<ObjectId>().Where(o => excludeIds.Contains(o))
                                                         .Select(o => o.GetDBObject().Clone() as Line)
                                                         .Where(o => o != null).ToCollection();
                }
            }
        }
    }
}