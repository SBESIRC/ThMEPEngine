using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADExtension;
using ThMEPElectrical;
using AcHelper.Commands;
using System.Windows.Forms;
using ThMEPElectrical.Model;
using TianHua.Electrical.UI.CAD;
using System.Collections.Generic;
using ThMEPElectrical.BlockConvert;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Electrical.UI
{
    public class ElectricalUIApp : IExtensionApplication
    {
        private fmSmokeLayout SmokeLayoutUI { get; set; }


        public void Initialize()
        {
            SmokeLayoutUI = null;
        }

        public void Terminate()
        {
            SmokeLayoutUI = null;
        }

        [CommandMethod("TIANHUACAD", "THYWG", CommandFlags.Modal)]
        public void THYWG()
        {
            if (SmokeLayoutUI == null)
            {
                SmokeLayoutUI = new fmSmokeLayout();
                SmokeLayoutDataModel _SmokeLayoutDataModel = new SmokeLayoutDataModel()
                {
                    LayoutType = LayoutType,
                    AreaLayout = AreaLayout,
                    RoofThickness = Parameter.RoofThickness,
                };
                SmokeLayoutUI.InitForm(_SmokeLayoutDataModel);
            }
            AcadApp.ShowModelessDialog(SmokeLayoutUI);
        }

        [CommandMethod("TIANHUACAD", "THTZL", CommandFlags.Modal)]
        public void ThBlockConvert()
        {
            using (var dlg = new fmBlockConvert())
            using (var blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            using (var manager = ThBConvertManager.CreateManager(blockDb.Database, ConvertMode.ALL))
            {
                // 获取转换条目
                foreach (var rule in manager.Rules.Where(o => (o.Mode & ConvertMode.STRONGCURRENT) == ConvertMode.STRONGCURRENT))
                {
                    if (dlg.m_ListStrongBlockConvert == null || dlg.m_ListStrongBlockConvert.Count == 0) { dlg.m_ListStrongBlockConvert = new List<ViewFireBlockConvert>(); }
                    dlg.m_ListStrongBlockConvert.Add(new ViewFireBlockConvert()
                    {
                        UpstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item1),
                        DownstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item2),
                    });
                }
                foreach (var rule in manager.Rules.Where(o => (o.Mode & ConvertMode.WEAKCURRENT) == ConvertMode.WEAKCURRENT))
                {
                   if (dlg.m_ListWeakBlockConvert == null || dlg.m_ListWeakBlockConvert.Count == 0) { dlg.m_ListWeakBlockConvert = new List<ViewFireBlockConvert>(); }
                    dlg.m_ListWeakBlockConvert.Add(new ViewFireBlockConvert()
                    {
                        UpstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item1),
                        DownstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item2),
                    });
                }

                var result = AcadApp.ShowModalDialog(dlg);
                if (result == DialogResult.OK)
                {
                    // 获取图块转换比例
                    ConvertParameter.Scale = new Scale3d(dlg.BlockScale);

                    // 获取用户指定的转换条目
                    var rules = new List<ThBConvertRule>();
                    dlg.m_ListStrongBlockConvert.ForEach(o =>
                    {
                        rules.AddRange(manager.Rules.Where(r =>
                        {
                            return o.IsSelect &&                                
                            r.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string == o.UpstreamID &&
                            r.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string == o.DownstreamID;
                        }));
                    });
                    dlg.m_ListWeakBlockConvert.ForEach(o =>
                    {
                        rules.AddRange(manager.Rules.Where(r =>
                        {
                            return o.IsSelect &&
                            r.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string == o.UpstreamID &&
                            r.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string == o.DownstreamID;
                        }));
                    });
                    ConvertParameter.Rules = rules;

                    // 发送命令
                    switch (dlg.ActiveConvertMode)
                    {
                        case ConvertMode.STRONGCURRENT:
                            CommandHandlerBase.ExecuteFromCommandLine(false, "THPBE");
                            break;
                        case ConvertMode.WEAKCURRENT:
                            CommandHandlerBase.ExecuteFromCommandLine(false, "THLBE");
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string BlockDwgPath()
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThBConvertCommon.BLOCK_MAP_RULES_FILE);
        }

        private string LayoutType
        {
            get
            {
                return Parameter.sensorType == SensorType.SMOKESENSOR ?
                    ElectricalUICommon.SMOKE_INDUCTION : ElectricalUICommon.TEMPERATURE_INDUCTION;
            }
        }

        private string AreaLayout
        {
            get
            {
                return ElectricalUICommon.AREA_COMMON;
            }
        }

        private PlaceParameter Parameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.Parameter == null)
                {
                    ThMEPElectricalService.Instance.Parameter = new PlaceParameter();
                }
                return ThMEPElectricalService.Instance.Parameter;
            }
        }

        private ThBConvertParameter ConvertParameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.ConvertParameter == null)
                {
                    ThMEPElectricalService.Instance.ConvertParameter = new ThBConvertParameter();
                }
                return ThMEPElectricalService.Instance.ConvertParameter;
            }
        }
    }
}
