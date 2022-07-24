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
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPWSS.HydrantLayout.Command
{
    public class ThHydrantLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private int _radius = 3000;
        private int _layoutMode = 2;
        private int _layoutObject = 2;
        private bool _avoidParking = true;
        private bool _layoutInMiddle = false;
        public Dictionary<string, List<string>> _BlockNameDict { get; set; } = new Dictionary<string, List<string>>();
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
            _BlockNameDict = HydrantLayoutSetting.Instance.BlockNameDict;
            _layoutInMiddle = HydrantLayoutSetting.Instance.LayoutInMiddle;
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
                var transformer = ThMEPWSSUtils.GetTransformer(selectPts);
                //var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                //插入图块
                var recordLayerStatus = new List<string> { ThHydrantCommon.Layer_Hydrant, ThHydrantCommon.Layer_Hydrant_Extinguisher };
                var layerStatusDict = InsertBlkService.RecordLayerStatus(recordLayerStatus);

                //var blkList = new List<string> { ThHydrantCommon.BlkName_Hydrant, ThHydrantCommon.BlkName_Hydrant_Extinguisher, ThHydrantCommon.BlkName_Vertical };
                var blkList = new List<string> { ThHydrantCommon.BlkName_Vertical, ThHydrantCommon.BlkName_Vertical150 };
                var layerList = new List<string> { ThHydrantCommon.Layer_Vertical };
                InsertBlkService.LoadBlockLayerToDocument(acadDatabase.Database, blkList, layerList);

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
                dataQuery.ProjectOntoXYPlane();
                dataQuery.Print();

                //Engine start
                DataPass dataPass0 = new DataPass(_radius, _layoutObject, _layoutMode, _avoidParking, _layoutInMiddle);
                Run run0 = new Run(dataQuery, dataPass0);
                run0.Pipeline();
                List<OutPutModel> outPutModels = run0.outPutModels;
                List<ThIfcVirticalPipe> VerticalPipeOut = run0.VerticalPipeOut;

                //转回到原位置
                dataQuery.Reset(transformer);
                outPutModels.ForEach(x => x.Reset(transformer));
                //VerticalPipeOut.ForEach(x => transformer.Reset(x.Outline));

                var validHydrant = outPutModels.Where(x => x.IfFind == true && (x.Type == 1 || x.Type == 2)).ToList();


                //插入真实块
                InsertBlkService.InsertBlock(outPutModels, 1);

                //插入过远提示
                var tooFarList = validHydrant.Where(x => (x.CenterPoint.DistanceTo(x.OriginModel.Center) >= ThHydrantCommon.DistTol)).ToList();
                InsertBlkService.InsertTooFar(tooFarList, ThHydrantCommon.Layer_Warning_TooFar, 2000, 2);
                //插入没做出来提示
                var notFoundList = outPutModels.Where(x => x.IfFind == false && (x.Type == 1 || x.Type == 2)).ToList();
                InsertBlkService.InsertWarning(notFoundList, ThHydrantCommon.Layer_Warning_NotDo, 2000, 1);

                //删除块
                validHydrant.ForEach(x => InsertBlkService.CleanEntity(x.OriginModel.Data));
                VerticalPipeOut.ForEach(x => InsertBlkService.CleanEntity(x.Data));

                InsertBlkService.ResetLayerStatus(layerStatusDict);
            }
        }

        private List<string> GetResultLayer(List<OutPutModel> validHydrant)
        {
            var layerList = new List<string>();
            var layers = validHydrant.Select(x => (x.OriginModel.Data as BlockReference).Layer);
            layerList.AddRange(layers);

            return layerList;
        }

    }
}
