using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using ThMEPWSS.UndergroundWaterSystem.ViewModel;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class ThLayerInitializeService
    {
        public void Initialize(ThWaterSystemInfoModel infoModel)
        {
            var pipeLayers = new List<string>();
            var valveLayers = new List<string>();
            var markLayers = new List<string>();
            var markStyles = new List<string>();
            foreach (var floor in infoModel.FloorList)
            {
                pipeLayers.AddRange(floor.FloorInfo.PipeLines.Select(e => e.Layer));
                valveLayers.AddRange(floor.FloorInfo.ValveList.Where(e => e.Valve != null).Select(e => e.Valve.Layer));
                markLayers.AddRange(floor.FloorInfo.MarkList.Select(e => e.Layer));
                markStyles.AddRange(floor.FloorInfo.MarkList.Select(e => e.TextStyle));
            }
            pipeLayers = pipeLayers.Where(e => e != "").ToList();
            valveLayers = valveLayers.Where(e => e != "").ToList();
            markLayers = markLayers.Where(e => e != "").ToList();
            markStyles = markStyles.Where(e => e != "").ToList();
            var pipeLayerGroup = from n in pipeLayers
                                 group n by n into g
                                 orderby g.Count() descending
                                 select g;
            var valveLayerGroup = from n in valveLayers
                                  group n by n into g
                                  orderby g.Count() descending
                                  select g;
            var markLayerGroup = from n in markLayers
                                 group n by n into g
                                 orderby g.Count() descending
                                 select g;
            var markStyleGroup = from n in markStyles
                                 group n by n into g
                                 orderby g.Count() descending
                                 select g;
            var pipeLayer = pipeLayerGroup.Count() > 0 ? pipeLayerGroup.First().First() : "0";
            var valveLayer = valveLayerGroup.Count() > 0 ? valveLayerGroup.First().First() : "0";
            var markLayer = markLayerGroup.Count() > 0 ? markLayerGroup.First().First() : "0";
            var markStyle = markStyleGroup.Count() > 0 ? markStyleGroup.First().First() : "0";
            ThSystemMapService.PipeLayerName= pipeLayer;
            ThSystemMapService.ValveLayerName = valveLayer;
            ThSystemMapService.TextLayer = markLayer;
            ThSystemMapService.TextStyle= markStyle;
        }
    }
}
