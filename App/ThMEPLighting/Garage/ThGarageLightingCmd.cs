using System;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Engine;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.ViewModel;

namespace ThMEPLighting.Garage
{
    public class ThGarageLightingCmd : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs;
        public ThGarageLightingCmd(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THZM";
            ActionName = "车道照明布置";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acadDatabase = AcadDatabase.Active())
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                var arrangeParameter = setInfo();
                if (arrangeParameter.LaneLineLayers.Count == 0)
                {
                    return;
                }
                var regionBorders = ThGarageInteractionUtils.GetFireRegionBorders(arrangeParameter.LaneLineLayers);
                if (regionBorders.Count == 0)
                {
                    return;
                }
                // 打开图层
                var openLayers = new List<string>();
                openLayers.AddRange(GetCommonLayers());
                openLayers.AddRange(arrangeParameter.LaneLineLayers);
                openLayers.AddRange(ThCableTrayParameter.Instance.AllLayers);
                acadDatabase.Database.OpenLayers(openLayers);

                //创建灯线
                using (var lightEngine = new ThGarageLightingEngine(acadDatabase.Database, arrangeParameter))
                {
                    lightEngine.Start(regionBorders);
                }
            }
        }

        private ThLightArrangeParameter setInfo()
        {
            var parameter = new ThLightArrangeParameter();
            if (_UiConfigs != null)
            {
                // 照度控制(单排or双排)
                parameter.IsSingleRow = _UiConfigs.IlluminanceControl == "单排布置";

                // 安装方式
                if (_UiConfigs.InstallationMode == "线槽安装")
                {
                    parameter.InstallWay = InstallWay.CableTray;
                }
                else if (_UiConfigs.InstallationMode == "吊链安装")
                {
                    parameter.InstallWay = InstallWay.Chain;
                }

                // 连线模式 (吊链安装有两种)
                if (_UiConfigs.ConnectMode == "弧线连接")
                {
                    parameter.ConnectMode = ConnectMode.CircularArc;
                }
                else if (_UiConfigs.ConnectMode == "直线连接")
                {
                    parameter.ConnectMode = ConnectMode.Linear;
                }

                // 布置模式
                switch (_UiConfigs.LayoutMode)
                {
                    case "按柱跨布置":
                        parameter.LayoutMode = LayoutMode.ColumnSpan;
                        break;
                    case "避梁布置":
                        parameter.LayoutMode = LayoutMode.AvoidBeam;
                        break;
                    case "可跨梁布置":
                        parameter.LayoutMode = LayoutMode.SpanBeam;
                        break;
                    default:
                        parameter.LayoutMode = LayoutMode.EqualDistance;
                        break;
                }

                // 其他布置参数
                parameter.AutoCalculate = _UiConfigs.NumberOfCircuits == "自动计算";
                parameter.LightNumberOfLoop = _UiConfigs.NumberOfCircuitsAutomaticCalculationOfNLoop;
                parameter.LoopNumber = _UiConfigs.NumberOfCircuitsSpecifyTheNumberOfNPerCircuits;
                parameter.DefaultStartNumber = _UiConfigs.StartingNumber;
                parameter.Height = Convert.ToDouble(_UiConfigs.Specification.Substring(_UiConfigs.Specification.IndexOf('*') + 1));
                parameter.Width = Convert.ToDouble(_UiConfigs.Specification.Substring(0, _UiConfigs.Specification.IndexOf('*')));
                parameter.IsTCHCableTray = _UiConfigs.IsTCHCableTray;
                parameter.DoubleRowOffsetDis = _UiConfigs.DoubleRowSpacing;
                parameter.Interval = _UiConfigs.LampSpacing;

                // 图纸比例
                switch (_UiConfigs.BlockRatio)
                {
                    case "1:1":
                        parameter.PaperRatio = 1.0;
                        break;
                    case "1:50":
                        parameter.PaperRatio = 50.0;
                        break;
                    case "1:100":
                        parameter.PaperRatio = 100.0;
                        break;
                    case "1:150":
                        parameter.PaperRatio = 150.0;
                        break;
                    default:
                        var strs = _UiConfigs.BlockRatio.Split(':');
                        if (strs.Length == 2)
                        {
                            double ratio = 0.0;
                            if (double.TryParse(strs[1], out ratio))
                            {
                                parameter.PaperRatio = ratio;
                            }
                        }
                        break;
                }
                parameter.LightNumberTextHeight *= parameter.PaperRatio / 100.0;

                // 获取建筑车道线图层
                parameter.LaneLineLayers = new List<string> { ThGarageLightCommon.DxCenterLineLayerName };
            }
            return parameter;
        }

        private List<string> GetCommonLayers()
        {
            var layers = new List<string>();
            layers.Add(ThGarageLightCommon.DxCenterLineLayerName);
            layers.Add(ThGarageLightCommon.FdxCenterLineLayerName);
            layers.Add(ThGarageLightCommon.SingleRowCenterLineLayerName);
            return layers;
        }
    }
}
