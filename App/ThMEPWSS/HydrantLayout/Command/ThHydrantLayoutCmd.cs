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

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Command;

using ThMEPWSS.Common;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.tmp.Engine;

namespace ThMEPWSS.HydrantLayout.Command
{
    public class ThHydrantLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private int _radius = 3000;
        private int _layoutMode = 2;
        private int _layoutObject = 2;
        private bool _avoidParking = true; 
        private Dictionary<string, List<string>> _BlockNameDict;
        public ThHydrantLayoutCmd()
        {
            InitialCmdInfo();
            InitialSetting();

        }
        private void InitialCmdInfo()
        {
            ActionName = "优化布置";
            CommandName = "THXHSYH";
        }
        private void InitialSetting()
        {
            _radius = HydrantLayoutSetting.Instance.SearchRadius;
            _layoutObject = HydrantLayoutSetting.Instance.LayoutObject;
            _layoutMode = HydrantLayoutSetting.Instance.LayoutMode;
            _avoidParking = HydrantLayoutSetting.Instance.AvoidParking;

            _BlockNameDict = new Dictionary<string, List<string>>(){
                                { "集水井", new List<string>() { "A-Well-1" }},
                                { "非机械车位", new List<string>() { "car0" } }
                            };
        }

        public override void SubExecute()
        {
            HydrantLayoutExecute();
        }
        public void Dispose()
        {
        }

        public void HydrantLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPts = ThSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                //转换器
                //var transformer = ThMEPWSSUtils.GetTransformer(selectPts);
                var transformer = new ThMEPOriginTransformer(new Point3d(0,0,0));


                //提取数据
                var dataFactory = new ThHydrantLayoutDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = _BlockNameDict,
                };
                dataFactory.GetElements(acadDatabase.Database, selectPts);

                //处理数据
                var dataQuery = new ThHydrantLayoutDataQueryService()
                {
                    VerticalPipe = dataFactory.VerticalPipe,
                    Hydrant = dataFactory.Hydrant,
                    InputExtractors = dataFactory.Extractors,
                    Car = dataFactory.Car,
                    Well = dataFactory.Well,
                };

                dataQuery.ProcessArchitechData();
                dataQuery.ProcessHydrant();

                //转换到原点
                dataQuery.Transform(transformer);

                //Engine start
                DataPass dataPass0 = new DataPass(_radius,_layoutObject,_layoutObject,_avoidParking);
                Run run0 = new Run(dataQuery,dataPass0);
                List<OutPutModel> outPutModels = run0.outPutModels;

                //
                //打印
                dataQuery.Print(); //for debug

                //转回到原位置
                dataQuery.Reset(transformer);
                dataQuery.Print();
                //dataQuery.Clean();


            }
        }
    }
}
