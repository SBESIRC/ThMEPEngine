using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil.Cmd
{
    public class ThFloorHeatingCmd : ThMEPBaseCommand, IDisposable
    {
        private Dictionary<string, List<string>> _BlockNameDict;

        public ThFloorHeatingCmd()
        {
            InitialCmdInfo();
            InitialSetting();
        }
        private void InitialCmdInfo()
        {
            ActionName = "生成";
            CommandName = "THDNPG"; //地暖盘管
        }
        private void InitialSetting()
        {
            _BlockNameDict = ThFloorHeatingCoilSetting.Instance.BlockNameDict;
        }
        public override void SubExecute()
        {
            ThFlootingHeatingExecute();
        }
        public void Dispose()
        {
        }
        public void ThFlootingHeatingExecute()
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

                var transformer = ThMEPHVACCommonUtils.GetTransformer(selectPts);
                transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var dataFactory = new ThFloorHeatingDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = _BlockNameDict,
                };
                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThFloorHeatingDataProcessService()
                {
                    InputExtractors = dataFactory.Extractors,
                    FurnitureObstacleData = dataFactory.SanitaryTerminal,
                    RoomSeparateLine = dataFactory.RoomSeparateLine,
                    RoomSuggestDist = dataFactory.RoomSuggestDist,
                    WaterSeparatorData = dataFactory.WaterSeparator,
                    FurnitureObstacleDataTemp = dataFactory.SenitaryTerminalOBBTemp,
                    RoomSetFrame = dataFactory.RoomSetFrame,
                };

                //dataQuery.Transform(transformer);
                dataQuery.ProcessDoorData();
                dataQuery.CraeteRoomSapceModel();
                dataQuery.ProcessWaterSeparator();
                dataQuery.CreateFurnitureObstacle();
                dataQuery.CreateRoomSet();
                dataQuery.Print();
                //dataQuery.Reset(transformer);

                ///////
                ///

                //过程写在这里
                Run run0 = new Run(dataQuery);
                run0.Pipeline();

                /////////////








            }
        }
    }
}
