using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThRoomNameResetService
    {
        private List<string> ParkingStallNames { get; set; }
        private List<ThIfcRoom> Rooms { get; set; }
        private double OffsetDis { get; set; }
        private DBObjectCollection ParkingStalls { get; set; }
        private List<string> NecessaryArrangeSpaceNames { get; set; }
        public ThRoomNameResetService(List<ThIfcRoom> rooms, DBObjectCollection parkingStalls)
        {
            Rooms = rooms;
            OffsetDis = 500.0;
            ParkingStalls = parkingStalls;
            ParkingStallNames = new List<string>() { "停车", "汽车", "车库", "地库", "地下车库" };
            NecessaryArrangeSpaceNames = new List<string>() { "隔油", "水泵", "泵房" , "垃圾" }; // 持续丰富
        }
        public void Reset()
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ParkingStalls);
            Rooms.ForEach(o =>
            {
                var tags = o.Tags.Contains(o.Name) ? o.Tags : o.Tags.Append(o.Name);
                o.Name = string.Join(";", tags.ToArray());
                // 检查房间名称是否是停车区域
                if (o.Tags.Append(o.Name).ToList().Where(n => IsParkingStallArea(n)).Any())
                {
                    o.Name = "停车区域";
                }
                else
                {
                    IBuffer bufferService = new ThNTSBufferService();
                    var ent = bufferService.Buffer(o.Boundary, OffsetDis);
                    var objs = new DBObjectCollection();
                    if(ent!=null)
                    {
                        objs = spatialIndex.SelectWindowPolygon(ent);
                    }
                    else
                    {
                        objs = spatialIndex.SelectWindowPolygon(o.Boundary);
                    }
                    
                    if (objs.Count >= 6)
                    {
                        o.Name = "停车区域";
                    }
                }
                // 检查房间名称是否是必布空间名称
                if(!o.Name.Contains("停车区域"))
                {
                    if(!IsNecessaryArrangeRoomName(o.Name))
                    {
                        // 如果不是停车区域，也不是必布空间名称,默认为其他空间
                        o.Name = "";
                    }
                    else
                    {
                        // 保留房间名称
                    }
                }
            });
        }
        private bool IsParkingStallArea(string name)
        {
            return ParkingStallNames.Where(o => name.Contains(o)).Any();
        }

        private bool IsNecessaryArrangeRoomName(string name)
        {
            return NecessaryArrangeSpaceNames.Where(o => name.Contains(o)).Any();
        }
    }
}
