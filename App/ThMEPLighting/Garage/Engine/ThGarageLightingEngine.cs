using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Garage.Service.LayoutResult;

namespace ThMEPLighting.Garage.Engine
{
    internal class ThGarageLightingEngine : IDisposable
    {
        private Database Db { get; set; }
        private ThCableTrayParameter CableTrayParameter { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        public ThGarageLightingEngine(Database db,ThLightArrangeParameter arrangeParameter)
        {
            Db = db;
            ArrangeParameter = arrangeParameter;
            CableTrayParameter = new ThCableTrayParameter();
        }
        public void Dispose()
        {
            //ToDO
        }
        public void Start(List<ThRegionBorder> regionBorders)
        {
            // 获取结构数据
            GetStructInfo(regionBorders);

            // 灯默认编号
            var defaultNumbers = GetDefaultStartNumbers();

            var normalRegionBorders = regionBorders.Where(o => o.ForSingleRowCableTrunking == false).ToList();
            var singRowRegionBorders = regionBorders.Where(o => o.ForSingleRowCableTrunking).ToList();

            // 布置 + 连线
            normalRegionBorders.ForEach(r =>
            {
                var singleRowRegionBorder = GetSubRegionBorder(singRowRegionBorders, r.Id);

                // begin
                r.Transform();
                singleRowRegionBorder.Transform();

                // 布置
                int loopNumber = 0;
                var lightGraphs = new List<ThLightGraphService>();               
                var centerSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
                var centerGroupLines = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
                Layout(r, ref loopNumber, ref lightGraphs, ref centerSideDicts,ref centerGroupLines);

                // 提示
                bool isReturn = ShowTip(loopNumber);
                if(!isReturn)
                {
                    // 对于双排线槽，要延伸非灯线和单排线槽中心线
                    r.FdxCenterLines = Extend(r.FdxCenterLines, centerSideDicts);
                    singleRowRegionBorder.DxCenterLines = Shorten(singleRowRegionBorder.DxCenterLines, centerSideDicts);

                    // 连线
                    ThLightWireBuilder lightWireBuilder = null;
                    if (ArrangeParameter.InstallWay == InstallWay.CableTray)
                    {
                        // 对单排线槽中心线布灯、编号
                        var singleGraphs=SingleRowCableTrunkingLayout(singleRowRegionBorder, loopNumber);
                        var totalGraphs = new List<ThLightGraphService>();
                        totalGraphs.AddRange(singleGraphs);
                        totalGraphs.AddRange(lightGraphs);
                        lightWireBuilder = CreateLightWireBuilder(totalGraphs, r.FdxCenterLines);
                    }
                    else if (ArrangeParameter.InstallWay == InstallWay.Chain)
                    {
                        var directionConfig = JumpWireDirectionConfig(
                            ArrangeParameter.DefaultStartNumber, ArrangeParameter.IsSingleRow, loopNumber);
                        lightWireBuilder = CreateLightWireBuilder(ArrangeParameter.ConnectMode,defaultNumbers,
                            lightGraphs, directionConfig);
                    }
                    if (lightWireBuilder == null)
                    {
                        return;
                    }
                    lightWireBuilder.Transformer = r.Transformer;
                    lightWireBuilder.CenterSideDicts = centerSideDicts;
                    lightWireBuilder.CenterGroupLines = centerGroupLines;
                    lightWireBuilder.ArrangeParameter = ArrangeParameter;
                    lightWireBuilder.CableTrayParameter = CableTrayParameter;
                    lightWireBuilder.Build();

                    r.Delete(ArrangeParameter, Db); //删除已生成的灯块、文字、灯线、线槽边线
                    ((IPrinter)lightWireBuilder).Print(Db); // 打印新生成的布置结果
                    lightWireBuilder.Reset(); // 对生成的结果进行还原
                }

                r.Reset(); // 对框中的元素进行还原
            });
        }

        private bool ShowTip(int loopNumber)
        {
            if (ArrangeParameter.InstallWay == InstallWay.Chain)
            {
                if (loopNumber > 3)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("采用吊链安装方式，每排灯具的回路不能超过3个，建议采取以下任意操作后重新布置：");
                    sb.AppendLine("1. 缩小布置区域");
                    sb.AppendLine("2. 放大自动计算每回路的灯具数（不超过25）");
                    sb.AppendLine("3. 改用指定数量（单排布置不超过3回路，双排布置不超过6回路）");
                    System.Windows.MessageBox.Show(sb.ToString(),
                        "天华提示", System.Windows.MessageBoxButton.OK);
                    return true;
                }
            }
            return false;
        }

        private ThLightWireBuilder CreateLightWireBuilder(
            ConnectMode connectMode,
            List<string> defaultNumbers,
            List<ThLightGraphService> lightGraphs,
            Dictionary<string,int> directionConfig)
        {
            switch (connectMode)
            {
                case ConnectMode.Linear:
                    return new ThChainConnectionBuilder(lightGraphs)
                    {
                        DefaultNumbers = defaultNumbers,
                        DirectionConfig = directionConfig,
                        CurrentUserCoordinateSystem = Active.Editor.CurrentUserCoordinateSystem,
                    };
                case ConnectMode.CircularArc:
                    return new ThCircularArcConnectionBuilder(lightGraphs)
                    {
                        DefaultNumbers = defaultNumbers,
                        DirectionConfig = directionConfig,
                        CurrentUserCoordinateSystem = Active.Editor.CurrentUserCoordinateSystem,
                    };
                default:
                    return null;
            }
        }

        private ThLightWireBuilder CreateLightWireBuilder(
            List<ThLightGraphService> lightGraphs,
            List<Line> fdxCenterLines)
        {
            return new ThCableTrayConnectionBuilder(lightGraphs)
            {
                FdxLines = fdxCenterLines,
            };
        }

        private List<Line> Extend(List<Line> lines, Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            // 对于双排线槽，延伸非灯线
            if (ArrangeParameter.InstallWay == InstallWay.CableTray &&
               ArrangeParameter.IsSingleRow == false)
            {
                var extendService = new ThExtendFdxLinesService(centerSideDicts);
                return extendService.Extend(lines);
            }
            return lines;
        }

        private List<Line> Shorten(List<Line> lines, Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            // 对于双排线槽，延伸单排线槽中心线
            if (ArrangeParameter.InstallWay == InstallWay.CableTray &&
               ArrangeParameter.IsSingleRow == false)
            {
                var extendService = new ThExtendFdxLinesService(centerSideDicts);
                return extendService.Shorten(lines);
            }
            return lines;
        }

        private void Layout(ThRegionBorder r, ref int loopNumber, 
            ref List<ThLightGraphService> lightGraphs,
            ref Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts,
            ref List<Tuple<Point3d, Dictionary<Line, Vector3d>>> centerGroupLines)
        {
            // 布置
            ThArrangementEngine arrangeEngine = null;
            if (ArrangeParameter.IsSingleRow)
            {
                arrangeEngine = new ThSingleRowArrangementEngine(ArrangeParameter);
            }
            else
            {
                arrangeEngine = new ThDoubleRowArrangementEngine(ArrangeParameter);
            }
            arrangeEngine.Arrange(r);
            if(!ArrangeParameter.IsSingleRow)
            {
                centerSideDicts = (arrangeEngine as ThDoubleRowArrangementEngine).CenterSideDicts;
                centerGroupLines = (arrangeEngine as ThDoubleRowArrangementEngine).CenterGroupLines;
            }
            lightGraphs = arrangeEngine.Graphs;
            loopNumber = arrangeEngine.LoopNumber;
        }

        private ThRegionBorder GetSubRegionBorder(List<ThRegionBorder> singRowRegionBorders,string borderId)
        {
            var res = singRowRegionBorders.Where(o => o.Id == borderId);
            return res.Count() == 1 ? res.First() : new ThRegionBorder() { RegionBorder=new Polyline()};
        }

        private List<ThLightGraphService> SingleRowCableTrunkingLayout(ThRegionBorder singleRowRegionBorder, int loopNumber)
        {
            if (singleRowRegionBorder.DxCenterLines.Count == 0 || ArrangeParameter.InstallWay != InstallWay.CableTray)
            {
                return new List<ThLightGraphService>();
            }
            var lightGraphs = new List<ThLightGraphService>();
            var arrangeEngine = new ThSingleRowArrangementEngine(ArrangeParameter);
            arrangeEngine.SetDefaultStartNumber(loopNumber * 2 + 1);
            arrangeEngine.Arrange(singleRowRegionBorder);
            lightGraphs = arrangeEngine.Graphs;
            return lightGraphs;
        }
        private void GetStructInfo(List<ThRegionBorder> regionBorders)
        {
            // 获取安装模式获取对应的柱、梁信息
            if (ArrangeParameter.LayoutMode == LayoutMode.ColumnSpan)
            {
                regionBorders.GetColumns(Db);
            }
            else if (ArrangeParameter.LayoutMode == LayoutMode.AvoidBeam ||
                ArrangeParameter.LayoutMode == LayoutMode.SpanBeam)
            {
                regionBorders.GetBeams(Db);
            }
        }
        private List<string> GetDefaultStartNumbers()
        {
            return ArrangeParameter.IsSingleRow ?
                new List<string>() { ArrangeParameter.DefaultStartNumber.GetLightNumber(2) } :
                new List<string>() { ArrangeParameter.DefaultStartNumber.GetLightNumber(2),
                    (ArrangeParameter.DefaultStartNumber+1).GetLightNumber(2) };
        }
        private Dictionary<string,int> JumpWireDirectionConfig(int defaultStartNumber, bool isSingleRow, int loopNumber)
        {
            var results = new Dictionary<string,int>();
            var directionConfig = ThJumpWireDirectionConfig.JumpWireDirectionConfig(
                defaultStartNumber, isSingleRow, loopNumber);
            foreach(var item in directionConfig)
            {
                var number = item.Key.GetLightNumber(2);
                results.Add(number, item.Value);
            }
            return results;
        }
    }
}
