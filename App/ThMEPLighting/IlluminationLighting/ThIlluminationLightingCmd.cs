using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThMEPEngineCore.Command;
using ThMEPLighting.EmgLight.Assistant;

using ThMEPLighting.Lighting.ViewModels;
using ThMEPLighting.IlluminationLighting.Common;
using ThMEPLighting.IlluminationLighting.Data;
using ThMEPLighting.IlluminationLighting.Model;
using ThMEPLighting.IlluminationLighting.Service;

namespace ThMEPLighting.IlluminationLighting
{
    public class IlluminationLightingCmd : ThMEPBaseCommand, IDisposable
    {
        readonly LightingViewModel _UiConfigs;

        private LightTypeEnum _lightType = LightTypeEnum.circleCeiling;
        private double _scale = 100;
        private bool _referBeam = true;
        private double _radiusN = 3000;
        private double _radiusE = 6000;
        private bool _ifLayoutEmg = true;
        private bool _ifEmgAsNormal = false;

        public IlluminationLightingCmd(LightingViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "IlluminationLightingLayout";
            ActionName = "布置";
            setInfo();
        }

        public IlluminationLightingCmd()
        {

        }

        public override void SubExecute()
        {
            ThIlluminationLightingLayoutExecute();
        }

        private void setInfo()
        {
            if (_UiConfigs != null)
            {
                _scale = _UiConfigs.ScaleSelectIndex == 0 ? 100 : 150;
                _lightType = _UiConfigs.LightingType;
                _radiusN = _UiConfigs.RadiusNormal;
                _radiusE = _UiConfigs.RadiusEmg;
                _referBeam = _UiConfigs.ShouldConsiderBeam;
                _ifLayoutEmg = _UiConfigs.IfLayoutEmgChecked;
                _ifEmgAsNormal = _UiConfigs.IfEmgUsedForNormal;
            }
        }

        public void Dispose()
        {
        }

        private void ThIlluminationLightingLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThIlluminationCommon.BlkNameListAreaLayout;
                var cleanBlkName = new List<string>() { ThIlluminationCommon.BlkName_CircleCeiling,
                                                        ThIlluminationCommon.BlkName_DomeCeiling,
                                                        ThIlluminationCommon.BlkName_InductionCeiling,
                                                        ThIlluminationCommon.BlkName_Downlight,
                                                       };
                if (_ifLayoutEmg)
                {
                    cleanBlkName.Add(ThIlluminationCommon.BlkName_EmergencyLight);
                }

                var avoidBlkName = ThIlluminationCommon.BlkNameListAreaLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();
                var layoutBlkNameN = ThIlluminationCommon.lightTypeDict[_lightType];
                var layoutBlkNameE = ThIlluminationCommon.BlkName_EmergencyLight;

                //画框，提数据，转数据
                var pts = ThIlluminationUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThIlluminationUtils.getIlluminationData(pts, extractBlkList, _referBeam);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThIlluminationUtils.transformToOrig(pts, geos);

                var dataQuery = new ThIlluminationDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.analysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                var roomType = ThFaAreaLayoutRoomTypeService.getAreaLightType(dataQuery.Rooms, dataQuery.roomFrameDict);

                foreach (var frame in dataQuery.FrameHoleList)
                {
                    DrawUtils.ShowGeometry(frame.Key, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(frame.Value, string.Format("l0hole"), 140);
                }

                var layoutParameter = new ThLayoutParameter();
                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = 0.025;
                layoutParameter.BlkNameN = layoutBlkNameN;
                layoutParameter.BlkNameE = layoutBlkNameE;
                layoutParameter.radiusN = _radiusN;
                layoutParameter.radiusE = _radiusE;
                layoutParameter.ifLayoutEmg = _ifLayoutEmg;
                layoutParameter.framePts = pts;
                layoutParameter.transformer = transformer;
                layoutParameter.roomType = roomType;

                //接入楼梯
                var stairBlkResult = ThStairService.layoutStair(dataQuery, layoutParameter);
                ////

                ThIlluminationEngine.thIlluminationLayoutEngine(dataQuery, layoutParameter,out var lightResult,out var blindsResult);

                //转回到原始位置
                lightResult.ForEach(x => x.transformBack(transformer));
                //stairBlkResult.ForEach(x => x.transformBack(transformer));

                //打印
                ThInsertBlk.prepareInsert(extractBlkList, extractBlkList.Select(x => ThIlluminationCommon.blk_layer[x]).ToList());
                ThInsertBlk.InsertBlock(lightResult, _scale);
                ThInsertBlk.InsertBlock(stairBlkResult, _scale);

            }
        }
    }
}
