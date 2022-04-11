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

                //var selectPtsAD = ThSelectFrameUtil.SelectFramePointCollection("框选轴侧", "框选轴侧");
                //if (selectPtsAD.Count == 0)
                //{
                //    return;
                //}

                var selectPtPrint = ThSelectFrameUtil.SelectPoint("轴侧起点");
                if (selectPtPrint == Point3d.Origin)
                {
                    return;
                }

                var selectPts = new Point3dCollection();
                selectPtsTop.Cast<Point3d>().ForEach(x => selectPts.Add(x));
                //selectPtsAD.Cast<Point3d>().ForEach(x => selectPts.Add(x));

                //转换器
                //var transformer = ThMEPWSSUtils.GetTransformer(selectPts);
                var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));


                //提取数据
                var dataFactory = new ThDrainageADPrivateDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = _BlockNameDict,
                    BlockNameValve = new List<string> { ThDrainageADCommon.BlkName_WaterHeater, ThDrainageADCommon.BlkName_AngleValve },
                    BlockNameTchValve = ThDrainageADCommon.BlkName_TchValve,
                };

                dataFactory.GetElements(acadDatabase.Database, selectPts);

                //处理数据
                var dataQuery = new ThDrainageADDataQueryService()
                {
                    BlockNameDict = _BlockNameDict,
                    VerticalPipeData = dataFactory.VerticalPipe,
                    HorizontalPipe = dataFactory.TCHPipe,
                    SelectPtsTopView = selectPtsTop,
                    //SelectPtsAD = selectPtsAD,
                    SanitaryTerminal = dataFactory.SanitaryTerminal,
                    AngleValveWaterHeater = dataFactory.AngleValveWaterHeater,
                    TchValve = dataFactory.TchValve,
                    StartPt = dataFactory.StartPt,
                };

                //dataQuery.Transform(transformer);
                dataQuery.SaperateTopViewAD();
                dataQuery.CreateVerticalPipe();
                dataQuery.BuildTermianlMode();
                dataQuery.Print();
                //dataQuery.Reset(transformer);

                var dataPass = new ThDrainageADPDataPass();
                dataPass.CoolPipeTopView = dataQuery.CoolPipeTopView;
                dataPass.HotPipeTopView = dataQuery.HotPipeTopView;
                dataPass.VerticalPipe = dataQuery.VerticalPipe;
                dataPass.StartPt = dataQuery.GetStartPt();
                dataPass.Terminal = dataQuery.Terminal;
                dataPass.qL = _qL;
                dataPass.m = _m;
                dataPass.Kh = _Kh;
                

                ThDrainageADEngine.DrainageTransADEngine(dataPass);




                //转换到原点
                //dataQuery.Transform(transformer);

            }
        }

    }
}
