using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;

using ThMEPElectrical.Staircase;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm;

using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;
using ThMEPElectrical.FireAlarmCombustibleGas.Model;
using ThFaAreaLayoutParameter = ThMEPElectrical.FireAlarmCombustibleGas.Model.ThFaAreaLayoutParameter;

namespace ThMEPElectrical.FireAlarmCombustibleGas
{
    public class ThfireAlarmGassCmdsNoUI
    {
        [CommandMethod("TIANHUACAD", "THGasLayout", CommandFlags.Modal)]
        public void FireAlarmSmokeHeatCmd()
        {
            using (var cmd = new ThFireAlarmGasCmd())
            {
                cmd.Execute();
            }
        }
    }

    public class ThFireAlarmGasCmd : ThMEPBaseCommand, IDisposable
    {
        readonly FireAlarmViewModel _UiConfigs;

        private double _scale = 100;
        private bool _referBeam = true;
        private double _radius = 8000;

        public ThFireAlarmGasCmd(FireAlarmViewModel uiConfigs)
        {
            _UiConfigs = uiConfigs;
            CommandName = "THFireGasLayout";
            ActionName = "布置";
         
            setInfo();
        }

        public ThFireAlarmGasCmd()
        {

        }

        public override void SubExecute()
        {
            FireAlarmGasLayoutExecute();
        }

        private void setInfo()
        {
            if (_UiConfigs != null)
            {
                _scale = _UiConfigs.BlockRatioIndex == 0 ? 100 : 150;
                _referBeam = _UiConfigs.ShouldConsiderBeam;
                _radius = _UiConfigs.ProtectRadius;
            }
        }

        public void Dispose()
        {

        }

        private void FireAlarmGasLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = ThFaCommon.BlkNameListAreaLayout;

                var cleanBlkName = new List<string>() { ThFaCommon.BlkName_Gas, ThFaCommon.BlkName_Gas_ExplosionProf };

                var avoidBlkName = ThFaCommon.BlkNameListAreaLayout.Where(x => cleanBlkName.Contains(x) == false).ToList();

                var layoutBlkNameGas = ThFaCommon.BlkName_Gas;
                var layoutBlkNameProfGas = ThFaCommon.BlkName_Gas_ExplosionProf;


                //画框，提数据，转数据
                var pts = ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                var geos = ThFireAlarmUtils.getSmokeData(pts, extractBlkList, _referBeam); //38s
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = ThFireAlarmUtils.transformToOrig(pts, geos);

                var dataQuery = new ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);//19s
                //洞,必须先做找到框线
                dataQuery.analysisHoles();
                //墙，柱，可布区域，避让
                dataQuery.ClassifyData();
                //dataQuery.getAreaSensorType();
                var roomType = ThMEPElectrical.FireAlarmCombustibleGas.Service.ThFaAreaLayoutRoomTypeService.getAreaSensorType(dataQuery.Rooms, dataQuery.roomFrameDict);

                foreach (var frame in dataQuery.FrameHoleList)
                {
                    DrawUtils.ShowGeometry(frame.Key, string.Format("l0room"), 30);
                    DrawUtils.ShowGeometry(frame.Value, string.Format("l0hole"), 140);
                }

                var gasResult = new ThFaAreaLayoutResult();
                var gasPrfResult = new ThFaAreaLayoutResult();

                var layoutParameter = new ThFaAreaLayoutParameter();

                layoutParameter.Scale = _scale;
                layoutParameter.AisleAreaThreshold = 0.025;
                layoutParameter.ProtectRadius = _radius;
                layoutParameter.RoomType = roomType;
                ThFireAlarmGasEngine.thFaGasLayoutEngine(dataQuery, gasResult, gasPrfResult, layoutParameter);

                //转回到原始位置
                gasResult.transformBack(transformer);
                gasPrfResult.transformBack(transformer);

                //打印
                ThFireAlarmInsertBlk.InsertBlock(gasResult.layoutPts.ToList(), _scale, layoutBlkNameGas, ThFaCommon.blk_layer[layoutBlkNameGas]);
                ThFireAlarmInsertBlk.InsertBlock(gasPrfResult.layoutPts.ToList(), _scale, layoutBlkNameProfGas, ThFaCommon.blk_layer[layoutBlkNameProfGas]);
            }
        }
    }
}
