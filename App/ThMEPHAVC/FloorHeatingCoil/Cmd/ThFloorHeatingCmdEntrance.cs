using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;


namespace ThMEPHVAC
{
    public class ThFloorHeatingCmdEntrance
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFloorHeatingData", CommandFlags.Modal)]
        public void ThFloorHeatingData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFloorHeatingCoilSetting.Instance.WithUI = false;
                var withUI = ThFloorHeatingCoilSetting.Instance.WithUI;
                //画框，提数据，转数据
                var selectFrames = ThSelectFrameUtil.SelectPolyline();
                if (selectFrames.Count == 0)
                {
                    return;
                }

                //var transformer = new ThMEPOriginTransformer(selectFrames[0].GetPoint3dAt(0));
                //transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));
                var transformer = ThFloorHeatingCoilUtilServices.GetTransformer(selectFrames, false);

                var dataFactory = new ThFloorHeatingDataFactory()
                {
                    Transformer = transformer,

                };

                var dataQuery = ThFloorHeatingCoilUtilServices.GetData(acadDatabase, selectFrames, transformer,withUI);
                dataQuery.Print();

                var roomPlSuggestDict = new Dictionary<Polyline, BlockReference>();
                var roomSuggest = ThFloorHeatingCoilUtilServices.GetRoomSuggestData(acadDatabase.Database, transformer);
                ThFloorHeatingCoilUtilServices.PairRoomPlWithRoomSuggest(dataQuery.RoomSet[0].Room, roomSuggest, ref roomPlSuggestDict);
                ThFloorHeatingCoilUtilServices.PairRoomWithRoomSuggest(ref dataQuery.RoomSet, roomPlSuggestDict, 200);

                dataQuery.RoomSet[0].Room.ForEach(x => ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(x.RoomBoundary.GetCenterInPolyline(), x.SuggestDist.ToString(), "l0roomSuggest", hight: 200));


            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFloorHeating", CommandFlags.Modal)]
        public void ThFloorHeating()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFloorHeatingCoilSetting.Instance.WithUI = false;

                using (var cmd = new ThFloorHeatingCmd())
                {
                    cmd.SubExecute();
                }
            }
        }
    }
}
