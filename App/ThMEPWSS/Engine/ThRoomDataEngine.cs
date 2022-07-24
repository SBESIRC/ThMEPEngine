using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;

namespace ThMEPWSS.Engine
{
    /// <summary>
    /// 获取本地图纸中的房间信息
    /// </summary>
    class ThRoomDataEngine
    {
        /// <summary>
        /// 管道井，烟道井最大面积㎡，这里烟道井，管道井都不考虑太大面积的
        /// </summary>
        private double maxTubeFuleRoomArea=2*1000000;
        /// <summary>
        /// 最大管道井面积㎡
        /// </summary>
        private double maxTubeWellRoomArea = 0.22 * 1000000;
        /// <summary>
        /// 短边最大长度mm（矩形框的短边长度小于等于300可能是管道井）
        /// </summary>
        private double maxTubeWellRoomMinSideLength = 300;
        /// <summary>
        /// 最小烟道面积㎡
        /// </summary>
        private double minFlueWellRoomArea = 0.25 * 1000000;
        /// <summary>
        /// 最大烟道面积㎡
        /// </summary>
        private double maxFlueWellRoomArea = 0.5 * 1000000;
        /// <summary>
        /// 短边最小长度（矩形框的短边长度>=250）
        /// </summary>
        private double minFlueWellRoomMinSideLength = 250;

        private List<ThIfcRoom> _allRooms;
        /// <summary>
        /// </summary>
        public ThRoomDataEngine()
        {
            _allRooms = new List<ThIfcRoom>();
            ///一次性获取所有的房间信息
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var roomBuilder = new ThRoomBuilderEngineWSS();
                var rooms = roomBuilder.BuildFromMS(acdb.Database, new Point3dCollection());
                if (rooms != null && rooms.Count > 0)
                    _allRooms.AddRange(rooms);
            }
        }
        /// <summary>
        /// 获取框线内的所有房间
        /// </summary>
        /// <param name="point3DCollection"></param>
        /// <param name="originTransformer"></param>
        /// <returns></returns>
        public List<ThIfcRoom> GetAllRooms(Point3dCollection point3DCollection, ThMEPOriginTransformer originTransformer=null)
        {
            var rooms = new List<ThIfcRoom>();
            if (null == _allRooms || _allRooms.Count < 1)
                return rooms;
            if (point3DCollection.Count < 1)
            {
                //获取所有房间
                rooms.AddRange(_allRooms);
            }
            else 
            {
                var pline = new Polyline(){ Closed = true };
                pline.CreatePolyline(point3DCollection);
                var ntsGeom = pline.ToNTSPolygon();
                foreach (var room in _allRooms) 
                {
                    if (room == null)
                        continue;
                    var copyRoom = ThIfcRoom.CreateWithTags(room.Boundary.Clone() as Entity,room.Tags);
                    var roomGeo = copyRoom.Boundary.ToNTSPolygonalGeometry();
                    if (originTransformer != null)
                    {
                        //偏移数据，暂时不处理
                    }
                    else 
                    {
                        if (ntsGeom.Crosses(roomGeo) || ntsGeom.Intersects(roomGeo))
                            rooms.Add(copyRoom);
                    }
                }
            }
            return rooms;
        }
        /// <summary>
        /// 将ThIfcRoom转为RoomModel
        /// </summary>
        /// <param name="allRooms">通过框线获取的到room信息</param>
        /// <param name="tubeFlueWellRooms">通过外参中获取到的烟道井或管道井，没有可以传null,或空的list</param>
        /// <returns></returns>
        public List<RoomModel> GetRoomModelRooms(List<ThIfcRoom> allRooms, List<RoomModel> tubeFlueWellRooms)
        {
            List<RoomModel> pipeRooms = new List<RoomModel>();
            List<string> hisIds = new List<string>();
            foreach (var item in allRooms)
            {
                var pRoom = new RoomModel();
                pRoom.thIFCRoom = item;
                var ntsPLine = item.Boundary.ToNTSPolygonalGeometry();
                pRoom.outLine = ntsPLine.ToDbPolylines().FirstOrDefault();
                pRoom.roomTypeName = EnumRoomType.Other;
                if (item.Tags != null && item.Tags.Count > 0)
                {
                    //计算类别，这里是获取基本的房间
                    foreach (var name in item.Tags)
                    {
                        var checkName = RoomNameFormat(name);
                        if (pRoom.roomTypeName != EnumRoomType.Other)
                            break;
                        if (IsToilte(checkName))
                        {
                            pRoom.roomTypeName = EnumRoomType.Toilet;
                        }
                        else if (IsKitchen(checkName))
                        {
                            pRoom.roomTypeName = EnumRoomType.Kitchen;
                        }
                        else if (IsBalcony(checkName))
                        {
                            pRoom.roomTypeName = EnumRoomType.Balcony;
                        }
                        else if (IsCorridor(checkName))
                        {
                            pRoom.roomTypeName = EnumRoomType.Corridor;
                        }
                    }
                }
                if (pRoom.roomTypeName == EnumRoomType.Other || pRoom.roomTypeName == EnumRoomType.FlueWell || pRoom.roomTypeName == EnumRoomType.TubeWell)
                {
                    //进一步判断是否有和预留管道井合并的,这里不考虑融合后继续合并其它空间的问题
                    if (null != tubeFlueWellRooms && tubeFlueWellRooms.Count > 0)
                    {
                        //优先找烟道，有些烟道和管道井离的很近，如果可以合并，认为是管道井
                        foreach (var tfRoom in tubeFlueWellRooms)
                        {
                            if (tfRoom.roomTypeName != EnumRoomType.FlueWell)
                                continue;
                            if (hisIds.Any(c => c.Equals(tfRoom.thIFCRoom.Uuid)))
                                continue;
                            var thOutLine = tfRoom.outLine.ToNTSPolygon();
                            if (ntsPLine.Intersects(thOutLine) || ntsPLine.Crosses(thOutLine))
                            {
                                pRoom.roomTypeName = tfRoom.roomTypeName;
                                pRoom.outLine = ntsPLine.Union(thOutLine).ToDbCollection().ToNTSMultiPolygon().ToDbPolylines().FirstOrDefault();
                                hisIds.Add(tfRoom.thIFCRoom.Uuid);
                            }
                        }
                        foreach (var tfRoom in tubeFlueWellRooms)
                        {
                            if (tfRoom.roomTypeName != EnumRoomType.TubeWell)
                                continue;
                            if (hisIds.Any(c => c.Equals(tfRoom.thIFCRoom.Uuid)))
                                continue;
                            var thOutLine = tfRoom.outLine.ToNTSPolygon();
                            if (ntsPLine.Intersects(thOutLine) || ntsPLine.Crosses(thOutLine))
                            {
                                pRoom.roomTypeName = tfRoom.roomTypeName;
                                pRoom.outLine = ntsPLine.Union(thOutLine).ToDbCollection().ToNTSMultiPolygon().ToDbPolylines().FirstOrDefault();
                                hisIds.Add(tfRoom.thIFCRoom.Uuid);
                            }
                        }
                    }
                }
                pipeRooms.Add(pRoom);
            }
            if (null != tubeFlueWellRooms && tubeFlueWellRooms.Count > 0)
            {
                foreach (var item in tubeFlueWellRooms)
                {
                    if (hisIds.Any(c => c.Equals(item.thIFCRoom.Uuid)))
                        continue;
                    pipeRooms.Add(item);
                }
            }
            return pipeRooms;
        }
        /// <summary>
        /// 将烟道井块或管道井块转为房间模型
        /// </summary>
        /// <param name="tubeBlocks">管道井块</param>
        /// <param name="flueBlocks">烟道井块</param>
        /// <returns></returns>
        public List<RoomModel> TubeFlueWellToRoom(List<BlockReference> tubeBlocks, List<BlockReference> flueBlocks)
        {
            List<RoomModel> rooms = new List<RoomModel>();
            if (null != tubeBlocks && tubeBlocks.Count > 0)
            {
                foreach (var item in tubeBlocks)
                {
                    var room = new RoomModel();
                    room.thIFCRoom = ThIfcRoom.Create(item.GeometricExtents.ToNTSPolygon().ToDbEntity());
                    room.outLine = room.thIFCRoom.Boundary.ToNTSPolygonalGeometry().ToDbPolylines().FirstOrDefault();
                    if (room.outLine.Area < 10)
                        continue;
                    room.roomTypeName = EnumRoomType.TubeWell;
                    rooms.Add(room);
                }
            }
            if (null != flueBlocks && flueBlocks.Count > 0)
            {
                foreach (var item in flueBlocks)
                {
                    var room = new RoomModel();
                    room.thIFCRoom = ThIfcRoom.Create(item.GeometricExtents.ToNTSPolygon().ToDbEntity());
                    room.outLine = room.thIFCRoom.Boundary.ToNTSPolygonalGeometry().ToDbPolylines().FirstOrDefault();
                    if (room.outLine.Area < 10)
                        continue;
                    room.roomTypeName = EnumRoomType.FlueWell;
                    rooms.Add(room);
                }
            }
            return rooms;
        }
        /// <summary>
        /// 获取可能是管道井的房间（可能会获取到烟道井）
        /// </summary>
        /// <param name="targetRooms"></param>
        /// <returns></returns>
        public List<RoomModel> GetTubeWellRooms(List<RoomModel> targetRooms)
        {
            List<RoomModel> tubeRooms = new List<RoomModel>();
            foreach (var item in targetRooms)
            {
                if (item == null || item.thIFCRoom == null || (item.roomTypeName != EnumRoomType.TubeWell && item.roomTypeName != EnumRoomType.Other))
                    continue;
                if (item.roomTypeName != EnumRoomType.Other)
                {
                    tubeRooms.Add(item);
                    continue;
                }
                var area = item.outLine.Area;
                if (area > maxTubeFuleRoomArea)
                    continue;
                var gmtry = item.outLine.ToNTSGeometry().EnvelopeInternal;
                var minDis = Math.Min(gmtry.Width, gmtry.Height);
                //面积小于0.22㎡或宽度（短边）小于350的空间
                if (area < maxTubeWellRoomArea || minDis <= maxTubeWellRoomMinSideLength)
                    tubeRooms.Add(item);
            }
            return tubeRooms;
        }
        /// <summary>
        /// 获取可能是管井的区间（可能会获取到烟道井）
        /// </summary>
        /// <param name="targetRooms"></param>
        /// <returns></returns>
        public List<RoomModel> GetFlueRooms(List<RoomModel> targetRooms)
        {
            List<RoomModel> flueRooms = new List<RoomModel>();
            foreach (var item in targetRooms)
            {
                if (item == null || item.thIFCRoom == null || (item.roomTypeName != EnumRoomType.FlueWell && item.roomTypeName != EnumRoomType.Other))
                    continue;
                if (item.roomTypeName == EnumRoomType.FlueWell)
                {
                    flueRooms.Add(item);
                    continue;
                }
                //面积大于0.25㎡小于0.5㎡且宽度（短边）大于250的空间，都认为可能是排烟井
                var area = item.outLine.Area;
                if (area > maxFlueWellRoomArea || area < minFlueWellRoomArea)
                    continue;
                var gmtry = item.GetRoomOBBPolyline().ToNTSGeometry().EnvelopeInternal;
                var minDis = Math.Min(gmtry.Width, gmtry.Height);
                if (minDis >= minFlueWellRoomMinSideLength)
                    flueRooms.Add(item);
            }
            return flueRooms;
        }

        private string RoomNameFormat(string name) 
        {
            //将字符串中的非汉字移除，只保留汉字
            string strRet = "";
            if (string.IsNullOrEmpty(name))
                return strRet;
            var copyName = (string)name.Clone();
            MatchCollection results = Regex.Matches(copyName, "[\u4e00-\u9fa5]+");
            foreach (var v in results)
                strRet += v.ToString();
            return strRet;
        }
        private bool IsToilte(string roomName)
        {
            //1)	包含“卫生间” //2)	包含“主卫” //3)	包含“次卫”
            //4)	包含“客卫” //5)	单字“卫” //6)	包含“洗手间” //7)	卫 + 阿拉伯数字
            var roomNameContains = new List<string>
            {
                "卫生间","主卫","公卫",
                "次卫","客卫","洗手间",
            };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c=>roomName.Contains(c)))
                return true;
            if (roomName.Equals("卫"))
                return true;
            return Regex.IsMatch(roomName, @"^[卫]\d$");
        }
        private bool IsKitchen(string roomName)
        {
            //1)	包含“厨房” //2)	单字“厨” //3)	包含“西厨”
            var roomNameContains = new List<string>{"厨房","西厨"};
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            if (roomName.Equals("厨"))
                return true;
            return false;
        }
        private bool IsBalcony(string roomName)
        {
            //包含阳台
            var roomNameContains = new List<string> { "阳台" };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            return false;
        }
        private bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { "连廊" };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            return false;
        }
    }
}
