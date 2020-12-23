using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPLighting.Garage;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Engine;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting
{
    public class ThMEPGarageLightingCmds
    {
        [CommandMethod("TIANHUACAD", "THCDZM", CommandFlags.Modal)]
        public void ThCdzm()
        {
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                var options = new PromptKeywordOptions("\n请指定布置方式")
                {
                    AllowNone = true
                };
                options.Keywords.Add("S", "S", "单排(S)");
                options.Keywords.Add("D", "D", "双排(D)");
                options.Keywords.Default = "S";
                var result3 = Active.Editor.GetKeywords(options);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }
                bool isSingleRow = result3.StringResult == "S" ? true : false;
                //输入参数
                var arrangeParameter = new ThLightArrangeParameter
                {
                    Width = 300,
                    Interval = 2700,
                    Margin = 800,
                    RacywaySpace = 2700,
                    IsSingleRow = isSingleRow,
                    LoopNumber = 4,
                    PaperRatio = 100
                };
                var racewayParameter = new ThRacewayParameter();
                var regionBorders = GetFireRegionBorders();
                //以上是准备输入参数
                ThArrangementEngine arrangeEngine = null;
                if (arrangeParameter.IsSingleRow)
                {
                    arrangeEngine = new ThSingleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                else
                {
                    arrangeEngine = new ThDoubleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                arrangeEngine.Arrange(regionBorders);
            }
        }
        private List<ThRegionBorder> GetFireRegionBorders()
        {
            var results = new List<ThRegionBorder>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var pso = new PromptSelectionOptions()
                {
                    MessageForAdding = "\n请选择布灯的区域框线",
                };
                TypedValue[] tvs = new TypedValue[]
                {
                     new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Polyline)).DxfName)
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(pso, sf);
                if (result.Status == PromptStatus.OK)
                {                    
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.Normalize(border);
                        var dxLines = GetRegionLines(newBorder,
                            new List<string> { ThGarageLightCommon.DxCenterLineLayerName },
                            new List<Type> { typeof(Line), typeof(Polyline) });
                        var fdxLines = GetRegionLines(newBorder,
                        new List<string> { ThGarageLightCommon.FdxCenterLineLayerName },
                        new List<Type> { typeof(Line), typeof(Polyline) });
                        var regionBorder = new ThRegionBorder
                        {
                            RegionBorder = newBorder,
                            DxCenterLines = dxLines,
                            FdxCenterLines = fdxLines
                        };
                        results.Add(regionBorder);
                    });
                }
            }
            return results;
        }
        private List<Line> GetRegionLines(Polyline region,List<string> layers,List<Type> types)
        {
            var results = new List<Line>();
            var curves=region.GetRegionCurves(layers, types)
                            .Where(k => k is Line || k is Polyline)
                            .Cast<Curve>().ToList();
            foreach(var item in curves)
            {
                if(item is Line line)
                {
                    results.Add(line);
                }
                else if(item is Polyline polyline)
                {
                    var objs = new DBObjectCollection(); //支持由Line组成的Polyline
                    polyline.Explode(objs);
                    objs.Cast<Line>().ForEach(o=> results.Add(o));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            return results;
        }
        [CommandMethod("TIANHUACAD", "THCDHL", CommandFlags.Modal)]
        public void THCDHL()
        {
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {                
                //输入参数来源于面板或(后期记录到灯块中)
                var arrangeParameter = new ThLightArrangeParameter
                {
                    Width = 300,
                    Interval = 2700,
                    Margin = 800,
                    RacywaySpace = 2700,
                    IsSingleRow = true,
                    LoopNumber = 4,
                    PaperRatio = 100,
                    AutoGenerate=false,
                };
                var racewayParameter = new ThRacewayParameter();
                var regionBorders = GetFireRegionBorders();
                //以上是准备输入参数
                ThArrangementEngine arrangeEngine = null;
                if (arrangeParameter.IsSingleRow)
                {
                    arrangeEngine = new ThSingleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                else
                {
                    arrangeEngine = new ThDoubleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                arrangeEngine.Arrange(regionBorders);
            }
        }
    }
}
