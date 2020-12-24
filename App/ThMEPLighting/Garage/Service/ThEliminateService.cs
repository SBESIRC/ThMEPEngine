using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using Autodesk.AutoCAD.EditorInput;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using DotNetARX;

namespace ThMEPLighting.Garage.Service
{
    public class ThEliminateService
    {        
        private ObjectIdList CollectIds { get; set; }
        private Polyline FireRegion { get; set; }
        private ThRacewayParameter RacewayParameter { get; set; }
        private ThEliminateService(ThRacewayParameter racewayParameter, Polyline fireRegion, ObjectIdList collectIds)
        {
            CollectIds = collectIds;
            RacewayParameter = racewayParameter;
            FireRegion = fireRegion;
        }
        public static void Eliminate(ThRacewayParameter racewayParameter,Polyline fireRegion,ObjectIdList collectIds)
        {
            var instance = new ThEliminateService(racewayParameter, fireRegion, collectIds);
            instance.Eliminate();
        }
        private void Eliminate()
        {
            //删除线槽、文字编号、块
            using (var acdb = AcadDatabase.Active())
            {
                var eraseEnts = new List<Entity>();
                eraseEnts.AddRange(GetRegionCodeText());
                eraseEnts.AddRange(GetRegionLightBlock());
                eraseEnts.AddRange(GetRegionLightLines());
                //删除不在CollectIds集合里的对象
                //CollectIds表示当前命令生成的对象
                eraseEnts.Where(o => !CollectIds.Contains(o.Id)).ForEach(o =>
                 {
                     o.UpgradeOpen();
                     o.Erase();
                     o.DowngradeOpen();
                 });
            }
        }
        private List<DBText> GetRegionCodeText()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var dbTexts = new List<DBText>();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(DBText)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, RacewayParameter.NumberTextParameter.Layer));
                tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                    
                    psr.Value.GetObjectIds().ForEach(o => dbTexts.Add(acdb.Element<DBText>(o)));
                    dbTexts=dbTexts.Where(o => FireRegion.Contains(o.Position)).ToList();
                }
                return dbTexts;
            }
        }
        private List<BlockReference> GetRegionLightBlock()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var blocks = new List<BlockReference>();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(BlockReference)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, RacewayParameter.LaneLineBlockParameter.Layer));
                tvs.Add(new TypedValue((int)DxfCode.BlockName, ThGarageLightCommon.LaneLineLightBlockName));
                tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var pts = FireRegion.Vertices();
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                    
                    psr.Value.GetObjectIds().ForEach(o => blocks.Add(acdb.Element<BlockReference>(o)));
                    blocks = blocks.Where(o => FireRegion.Contains(o.Position)).ToList();
                }
                return blocks;
            }
        }
        private List<Line> GetRegionLightLines()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var lightLines = new List<Line>();
                var layers = new List<string>();
                layers.Add(RacewayParameter.SideLineParameter.Layer);
                layers.Add(RacewayParameter.PortLineParameter.Layer);
                layers.Add(RacewayParameter.CenterLineParameter.Layer);
                layers=layers.Distinct().ToList();
                List<TypedValue> tvs = new List<TypedValue>();
                tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Line)).DxfName));
                tvs.Add(new TypedValue((int)DxfCode.LayerName, string.Join(",",layers)));
                tvs.Add(new TypedValue((int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName));
                var pts = FireRegion.Vertices();
                var sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {                   
                    psr.Value.GetObjectIds().ForEach(o => lightLines.Add(acdb.Element<Line>(o)));
                    lightLines = lightLines.Where(o => FireRegion.Contains(o)).ToList();
                }
                return lightLines;
            }
        }
    }
}
