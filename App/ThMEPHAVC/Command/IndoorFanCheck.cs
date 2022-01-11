using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPHVAC.Common;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.DataEngine;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    class IndoorFanCheck : ThMEPBaseCommand, IDisposable
    {
        Dictionary<Polyline, List<Polyline>> _selectPLines;
        ThMEPOriginTransformer _originTransformer;
        public List<Polyline> ErrorRoomPolylines;
        ThIndoorFanData _indoorFanData;
        List<IndoorFanBlock> hisIndoorFans;
        List<ObjectId> haveRoomFanIds;
        IndoorFanCheckModel indoorFanCheck;
        public IndoorFanCheck(Dictionary<Polyline, List<Polyline>> selectRoomLines) 
        {
            CommandName = "THSNJJH";
            ActionName = "室内机校核";
            indoorFanCheck = IndoorFanParameter.Instance.CheckModel;
            haveRoomFanIds = new List<ObjectId>();
            ErrorRoomPolylines = new List<Polyline>();
            _selectPLines = new Dictionary<Polyline, List<Polyline>>();
            if (null == selectRoomLines || selectRoomLines.Count < 1)
                return;
            var pt = selectRoomLines.First().Key.StartPoint;
            _originTransformer = new ThMEPOriginTransformer(pt);
            foreach (var pline in selectRoomLines)
            {
                var copyOut = (Polyline)pline.Key.Clone();
                if (null != _originTransformer)
                    _originTransformer.Transform(copyOut);
                var innerPLines = new List<Polyline>();
                if (pline.Value != null)
                {
                    foreach (var item in pline.Value)
                    {
                        var copyInner = (Polyline)item.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copyInner);
                        innerPLines.Add(copyInner);
                    }
                }
                _selectPLines.Add(copyOut, innerPLines);
            }
        }
        public void Dispose() { }

        public override void SubExecute()
        { 
            //获取选中框线中的风机，风口，风箱，计算风机的位置大小
            if (null == _selectPLines || _selectPLines.Count < 1)
                return;
            _indoorFanData = new ThIndoorFanData(_originTransformer);
            using (var acdb = AcadDatabase.Active())
            {
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            hisIndoorFans = _indoorFanData.GetIndoorFanBlockModels();
            //获取房间负荷信息
            var thRoomLoadTool = new ThRoomLoadTable(_originTransformer);
            var allRoomLoads = thRoomLoadTool.GetAllRoomLoadTable();
            thRoomLoadTool.CreateSpatialIndex(allRoomLoads);
            if (null == allRoomLoads || allRoomLoads.Count < 1)
                return;
            var allLeadLines = _indoorFanData.GetAllLeadLine();
            bool isCool = IndoorFanParameter.Instance.CheckModel.HotColdType == EnumHotColdType.Cold;
            double checkPercent = IndoorFanParameter.Instance.CheckModel.MarkOverPercentage;
            using (var acdb = AcadDatabase.Active())
            {
                var fanChanges = new List<FanLayoutDetailed>();
                foreach (var pline in _selectPLines)
                {
                    //根据房间框线、负荷表、引线获取该房间的负荷表
                    var roomLoads = thRoomLoadTool.GetIndexTables(pline.Key);
                    roomLoads = thRoomLoadTool.GetRoomInnerTables(pline.Key, roomLoads);
                    if (roomLoads == null || roomLoads.Count < 1)
                        roomLoads = thRoomLoadTool.GetRoomLeadTables(pline.Key, allRoomLoads, allLeadLines);
                    if (roomLoads == null || roomLoads.Count < 1)
                        continue;
                    bool haveValue = IndoorFanCommon.RoomLoadTableReadLoad(roomLoads.First(), isCool, out double roomArea, out double roomLoad);
                    if (!haveValue)
                        continue;
                    var roomFans = GetRoomIndoorFans(pline.Key);
                    var realLoad = isCool ? roomFans.Sum(c => c.CoolLoad) : roomFans.Sum(c => c.HotLoad);
                    bool signRoom = indoorFanCheck.MarkNotEnoughRoom? realLoad < roomLoad:false;
                    if (!signRoom && indoorFanCheck.MarkOverRoom) 
                    {
                        //进一步判断是否超出
                        double percent = (realLoad - roomLoad) / roomLoad;
                        signRoom = (percent*100) > indoorFanCheck.MarkOverPercentage;
                    }
                    if (!signRoom)
                        continue;
                    //房间标记，
                    var roomPoints = IndoorFanCommon.GetPolylinePoints(pline.Key);
                    var points = new Point3dCollection();
                    roomPoints.ForEach(c => points.Add(c));
                    var addPLine = ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(points).ToDbCollection().OfType<Polyline>().FirstOrDefault();
                    if (null != addPLine)
                    {
                        if (null != _originTransformer)
                            _originTransformer.Reset(addPLine);
                        ErrorRoomPolylines.Add(addPLine);
                    }
                }
            }
        }
        List<IndoorFanBlock> GetRoomIndoorFans(Polyline roomPLine)
        {
            var roomFans = new List<IndoorFanBlock>();
            if (null == hisIndoorFans || hisIndoorFans.Count < 1)
                return roomFans;
            foreach (var block in hisIndoorFans)
            {
                if (haveRoomFanIds.Any(c => c.Equals(block.FanBlockId)))
                    continue;
                var point = block.BlockPosion;
                if (!roomPLine.Contains(point))
                    continue;
                roomFans.Add(block);
            }
            return roomFans;
        }
    }
}
