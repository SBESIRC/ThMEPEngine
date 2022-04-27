using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Command;

using ThMEPWSS.Common;
using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Model;
using ThMEPWSS.DrainageADPrivate.Engine;
using ThMEPWSS.DrainageADPrivate.Service;

namespace ThMEPWSS.DrainageADPrivate.Cmd
{
    public class ThDrainageADPCmd : ThMEPBaseCommand, IDisposable
    {
        private Dictionary<string, List<string>> _BlockNameDict;
        private double _qL = 230;
        private double _m = 3.5;
        private double _Kh = 1.5;
        public ThDrainageADPCmd()
        {
            InitialCmdInfo();
            InitialSetting();

        }
        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THHXDY";
        }
        private void InitialSetting()
        {


            _qL = ThDrainageADSetting.Instance.qL;
            _m = ThDrainageADSetting.Instance.m;
            _Kh = ThDrainageADSetting.Instance.Kh;
            _BlockNameDict = ThDrainageADSetting.Instance.BlockNameDict;

        }

        public override void SubExecute()
        {
            DrainageADPExecute();
        }
        public void Dispose()
        {
        }

        public void DrainageADPExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPtsTop = ThSelectFrameUtil.SelectFramePointCollection("框选俯视", "框选俯视");
                if (selectPtsTop.Count == 0)
                {
                    return;
                }

                var selectPtPrint = ThSelectFrameUtil.SelectPoint("轴侧起点");
                if (selectPtPrint == Point3d.Origin)
                {
                    return;
                }

                var selectPts = new Point3dCollection();
                selectPtsTop.Cast<Point3d>().ForEach(x => selectPts.Add(x));

                //插入图层
                var blkNameValve = new List<string> { ThDrainageADCommon.BlkName_WaterHeater, ThDrainageADCommon.BlkName_AngleValve,
                                                        ThDrainageADCommon.BlkName_ShutoffValve, ThDrainageADCommon.BlkName_GateValve,
                                                        ThDrainageADCommon.BlkName_CheckValve, ThDrainageADCommon.BlkName_AntifoulingCutoffValve,
                                                        ThDrainageADCommon.BlkName_Casing,
                                                        };
                var angleValve = ThDrainageADCommon.Terminal_end_name.Select(x => x.Value).ToList();
                var blkNameOutputList = new List<string>
                {
                    ThDrainageADCommon.BlkName_Dim, ThDrainageADCommon.BlkName_OpeningSign, ThDrainageADCommon.BlkName_Casing_AD,
                };
                blkNameOutputList.AddRange(angleValve);
                blkNameOutputList.AddRange(blkNameValve);

                var layerNameOutputList = new List<string>();
                ThInsertOutputService.LoadBlockLayerToDocument(acadDatabase.Database, blkNameOutputList, layerNameOutputList);

                //转换器
                //var transformer = ThMEPWSSUtils.GetTransformer(selectPts);
                var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));
                selectPtPrint = transformer.Transform(selectPtPrint);

                //提取数据
                var dataFactory = new ThDrainageADPrivateDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = _BlockNameDict,
                    BlockNameValve = blkNameValve,
                    BlockNameTchValve = new List<string> { ThDrainageADCommon.BlkName_ShutoffValve_TchTag.ToUpper(), ThDrainageADCommon.BlkName_GateValve_TchTag.ToUpper(),
                                                        ThDrainageADCommon.BlkName_CheckValve_TchTag.ToUpper(), ThDrainageADCommon.BlkName_AntifoulingCutoffValve_TchTag.ToUpper(),
                                                        ThDrainageADCommon.BlkName_OpeningSign_TchTag.ToUpper(),ThDrainageADCommon .BlkName_WaterMeteValve_TchTag.ToUpper(),
                                                        },
                };

                dataFactory.GetElements(acadDatabase.Database, selectPts);

                //处理数据
                var dataQuery = new ThDrainageADDataProcessService()
                {
                    BlockNameDict = _BlockNameDict,
                    VerticalPipeData = dataFactory.VerticalPipe,
                    HorizontalPipe = dataFactory.TCHPipe,
                    SelectPtsTopView = selectPtsTop,
                    //SelectPtsAD = selectPtsAD,
                    SanitaryTerminal = dataFactory.SanitaryTerminal,
                    ValveWaterHeater = dataFactory.ValveWaterHeater,
                    TchValve = dataFactory.TchValve,
                    TchOpeningSign = dataFactory.TchOpeningSign,
                    OpeningSign = dataFactory.OpeningSign,
                };

                dataQuery.Transform(transformer);
                dataQuery.SaperateTopViewAD();
                dataQuery.CreateVerticalPipe();
                dataQuery.BuildTermianlMode();
                dataQuery.BuildValve();
                dataQuery.Print();

                var dataPass = new ThDrainageADPDataPass();
                dataPass.CoolPipeTopView.AddRange(dataQuery.CoolPipeTopView);
                dataPass.HotPipeTopView.AddRange(dataQuery.HotPipeTopView);
                dataPass.VerticalPipe.AddRange(dataQuery.VerticalPipe);
                dataPass.Terminal.AddRange(dataQuery.Terminal);
                dataPass.Valve.AddRange(dataQuery.Valve);
                dataPass.AngleValve.AddRange(dataQuery.AngleValve);
                dataPass.Casing.AddRange(dataQuery.Casing);
                dataPass.qL = _qL;
                dataPass.m = _m;
                dataPass.Kh = _Kh;
                dataPass.PrintBasePt = selectPtPrint;

                //转换引擎
                ThDrainageADEngine.DrainageTransADEngine(dataPass);

                //转换到原位置
                dataPass.OutputDim.ForEach(x => x.TransformBy(transformer.Displacement.Inverse()));
                dataPass.OutputAngleValve.ForEach(x => x.TransformBy(transformer.Displacement.Inverse()));
                dataPass.OutputValve.ForEach(x => x.TransformBy(transformer.Displacement.Inverse()));
                dataPass.OutputCoolPipe.ForEach(x => x.TransformBy(transformer.Displacement.Inverse()));
                dataPass.OutputHotPipe.ForEach(x => x.TransformBy(transformer.Displacement.Inverse()));

                //插入
                ThInsertOutputService.InsertBlk(dataPass.OutputDim);
                ThInsertOutputService.InsertBlk(dataPass.OutputAngleValve);
                ThInsertOutputService.InsertBlk(dataPass.OutputValve);
                ThInsertOutputService.InsertLine(dataPass.OutputCoolPipe, ThDrainageADCommon.Layer_CoolPipe);
                ThInsertOutputService.InsertLine(dataPass.OutputHotPipe, ThDrainageADCommon.Layer_HotPipe);

            }
        }

    }
}
