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

        public ThDrainageADPCmd()
        {
            InitialCmdInfo();
            InitialSetting();

        }
        private void InitialCmdInfo()
        {
            ActionName = "优化布置";
            CommandName = "THZCGJ";
        }
        private void InitialSetting()
        {
            _BlockNameDict = new Dictionary<string, List<string>>() {
                                        { "拖把池", new List<string>() { }},
                                        { "洗衣机", new List<string>() {  "sdr ter t"} } ,
                                        { "阳台洗手盆",new List<string> { } },
                                        { "厨房洗涤盆", new List<string>() { "edrcgergc", } } ,
                                        { "坐便器", new List<string>() { "fdtes" } } ,
                                        { "单盆洗手台", new List<string>() { "EWRYTY","xishoupen" } } ,
                                        { "双盆洗手台", new List<string>() { } } ,
                                        { "淋浴器", new List<string>() { "QADAD", "WF" } } ,
                                        { "浴缸", new List<string>() { } } ,
                                        };

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
                var dataQuery = new ThDrainageADDataQueryService( )
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


                ThDrainageADEngine.DrainageTransADEngine(dataPass);




                //转换到原点
                //dataQuery.Transform(transformer);

            }
        }

    }
}
