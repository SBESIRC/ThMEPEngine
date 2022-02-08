using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThCADExtension;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.IO;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.ViewModel;
using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmFixLayout.Logic;

namespace ThMEPElectrical.FireAlarmFixLayout.Command
{
    class ThAFASDisplayDeviceLayoutCmd : ThMEPBaseCommand, IDisposable
    {
        private BuildingType _buildingType = BuildingType.None;
        private string layoutBlkName = ThFaCommon.BlkName_Display_District;
        private double _scale = 100;

        public ThAFASDisplayDeviceLayoutCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "布置";
            CommandName = "THFADISPLAY";
        }
        private void InitialSetting()
        {
            _buildingType = (BuildingType)FireAlarmSetting.Instance.DisplayBuilding;
            layoutBlkName = FireAlarmSetting.Instance.DisplayBlk == 0 ? ThFaCommon.BlkName_Display_Floor : ThFaCommon.BlkName_Display_District;
            _scale = FireAlarmSetting.Instance.Scale;
        }
        public override void SubExecute()
        {
            FireAlarmDisplayDeviceLayoutExecute();
        }
        public void Dispose()
        {
        }
        private void FireAlarmDisplayDeviceLayoutExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //------------
                var transformer = ThAFASDataPass.Instance.Transformer;
                var pts = ThAFASDataPass.Instance.SelectPts;

                if (_buildingType == BuildingType.None)
                {
                    return;
                }

                //--------------初始图块信息
                var extractBlkList = ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { layoutBlkName };
                var avoidBlkName = ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //--------------提取数据
                var geos = ThAFASUtils.GetFixLayoutData(ThAFASDataPass.Instance, extractBlkList);
                if (geos.Count == 0)
                {
                    return;
                }

                //------------转回原点
                ThAFASUtils.TransformToZero(transformer, geos);

                //--------------处理数据：找洞。分类数据：墙，柱，可布区域，避让。扩大避让。
                var dataQuery = new ThAFASFixDataQueryService(geos, avoidBlkName);
                dataQuery.ExtendEquipment(cleanBlkName, _scale);
                dataQuery.AddAvoidence();
                dataQuery.MapGeometry();

                //------------布置
                ThDisplayDeviceFixedPointLayoutService layoutService = null;
                layoutService = new ThDisplayDeviceFixedPointLayoutService(dataQuery, _buildingType);
                var results = layoutService.Layout();

                ////------------对结果的重设
                var pairs = new List<KeyValuePair<Point3d, Vector3d>>();
                results.ForEach(p =>
                {
                    var pt = p.Key;
                    transformer.Reset(ref pt);
                    pairs.Add(new KeyValuePair<Point3d, Vector3d>(pt, p.Value));
                });
                pairs.ForEach(x => ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(x.Key, x.Value, "l0DisplayResult", 1, 30));
                pairs.ForEach(x => ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(x.Key, "l0DisplayResult", 1, 30, 500));

                //------------对插入真实块
                ThFireAlarmInsertBlk.InsertBlock(pairs, _scale, layoutBlkName, ThFaCommon.Blk_Layer[layoutBlkName], true);
                ThAFASUtils.TransformReset(transformer, geos);

            }
        }
        private void SettingNoUI()
        {
            // select an option
            string strResident = "住宅";
            string strPublic = "公建";

            var options = new PromptKeywordOptions("");
            options.Message = "\nPlease select option:";
            options.Keywords.Add(strResident, "R", "住宅(R)");
            options.Keywords.Add(strPublic, "P", "公建(P)");

            var rst = Active.Editor.GetKeywords(options);
            if (rst.Status != PromptStatus.OK)
                return;

            if (rst.StringResult.Equals(strResident))
            {
                _buildingType = FireAlarmFixLayout.Data.BuildingType.Resident;
            }
            else if (rst.StringResult.Equals(strPublic))
            {
                _buildingType = FireAlarmFixLayout.Data.BuildingType.Public;
            }
            else return;
        }

    }
}
