using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Garage.Service.LayoutResult;
using NFox.Cad;

namespace ThMEPLighting.Garage.Engine
{
    internal class ThGarageLightingEngine : IDisposable
    {
        private Database Db { get; set; }
        private ThCableTrayParameter CableTrayParameter { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThQueryColumnService ColumnQuery { get; set; }
        private ThQueryBeamService BeamQuery { get; set; }
        public ThGarageLightingEngine(Database db, ThLightArrangeParameter arrangeParameter)
        {
            Db = db;
            ArrangeParameter = arrangeParameter;
            CableTrayParameter = new ThCableTrayParameter();
            if (ArrangeParameter.LayoutMode == LayoutMode.ColumnSpan)
            {
                ColumnQuery = new ThQueryColumnService(Db);
            }
            else if (ArrangeParameter.LayoutMode == LayoutMode.AvoidBeam ||
                ArrangeParameter.LayoutMode == LayoutMode.SpanBeam)
            {
                BeamQuery = new ThQueryBeamService(Db);
            }
        }
        public void Dispose()
        {
            if (ColumnQuery != null)
            {
                ColumnQuery.Dispose();
            }
            if (BeamQuery != null)
            {
                BeamQuery.Dispose();
            }
        }
        public void Start(List<ThRegionBorder> regionBorders)
        {
            // 获取结构数据
            GetStructInfo(regionBorders);

            // 灯默认编号
            var defaultNumbers = GetDefaultStartNumbers();

            // 布置 + 连线
            regionBorders.ForEach(r =>
            {
                // 移动到近原点位置，解决超远问题
                r.Transform();

                // 布置
                var loopNumber = 0;
                var lightGraphs = new List<ThLightGraphService>();
                Layout(r, ref loopNumber, ref lightGraphs);
                // 提示
                var isReturn = ShowTip(loopNumber);
                if (!isReturn)
                {
                    // 连线
                    ThLightWireBuilder lightWireBuilder = null;
                    if (ArrangeParameter.InstallWay == InstallWay.CableTray)
                    {
                        lightWireBuilder = CreateLightWireBuilder(lightGraphs, r.FdxCenterLines.Clone().ToList());
                    }
                    else if (ArrangeParameter.InstallWay == InstallWay.Chain)
                    {
                        var directionConfig = JumpWireDirectionConfig(
                            ArrangeParameter.DefaultStartNumber, ArrangeParameter.IsSingleRow, loopNumber);
                        lightWireBuilder = CreateLightWireBuilder(ArrangeParameter.ConnectMode, defaultNumbers,
                            lightGraphs, directionConfig);
                    }
                    if (lightWireBuilder == null)
                    {
                        return;
                    }
                    lightWireBuilder.ExtendLines = r.ExtendLines; // 用于跨区连线
                    lightWireBuilder.Transformer = r.Transformer;
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
            Dictionary<string, int> directionConfig)
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

        private ThLightWireBuilder CreateLightWireBuilder(List<ThLightGraphService> lightGraphs, List<Line> fdxCenterLines)
        {
            return new ThCableTrayConnectionBuilder(lightGraphs)
            {
                FdxLines = fdxCenterLines,
            };
        }

        private void Layout(ThRegionBorder r, ref int loopNumber, ref List<ThLightGraphService> lightGraphs)
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
            lightGraphs = arrangeEngine.Graphs;
            loopNumber = arrangeEngine.LoopNumber;
        }

        private void GetStructInfo(List<ThRegionBorder> regionBorders)
        {
            // 获取安装模式获取对应的柱、梁信息
            if (ArrangeParameter.LayoutMode == LayoutMode.ColumnSpan)
            {
                regionBorders.ForEach(o => o.Columns = ColumnQuery.SelectCrossPolygon(o.RegionBorder));
            }
            else if (ArrangeParameter.LayoutMode == LayoutMode.AvoidBeam ||
                ArrangeParameter.LayoutMode == LayoutMode.SpanBeam)
            {
                regionBorders.ForEach(o => o.Beams = BeamQuery.SelectCrossPolygon(o.RegionBorder));
            }
        }
        private List<string> GetDefaultStartNumbers()
        {
            return ArrangeParameter.IsSingleRow ?
                new List<string>() { ArrangeParameter.DefaultStartNumber.GetLightNumber(2) } :
                new List<string>() { ArrangeParameter.DefaultStartNumber.GetLightNumber(2),
                    (ArrangeParameter.DefaultStartNumber+1).GetLightNumber(2) };
        }
        private Dictionary<string, int> JumpWireDirectionConfig(int defaultStartNumber, bool isSingleRow, int loopNumber)
        {
            var results = new Dictionary<string, int>();
            var directionConfig = ThJumpWireDirectionConfig.JumpWireDirectionConfig(defaultStartNumber, isSingleRow, loopNumber);
            foreach (var item in directionConfig)
            {
                var number = item.Key.GetLightNumber(2);
                results.Add(number, item.Value);
            }
            return results;
        }
    }
}
