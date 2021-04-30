using THMEPCore3D.Model;
using Dreambuild.AutoCAD;
using Newtonsoft.Json.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Service
{
    public class ThRoomJsonParseService
    {
        public Project Parse(string json)
        {
            JObject jObject = JObject.Parse(json);
            return new Project()
            {
                PrjId = (string)jObject["PrjId"],
                ModelId = (string)jObject["ModelId"],
                SubentryName = (string)jObject["SubentryName"],
                Floors = ToFloors(jObject["Floors"]),
                Propertys = ParseProperty(jObject["Propertys"])

            };
        }

        private List<Floor> ToFloors(JToken jToken)
        {
            var results = new List<Floor>();
            jToken.ForEach(o =>
            {
                var floor = new Floor()
                {
                    Name= ((JProperty)o).Name,
                    Elevation = (double)o.First["Elevation"],
                    Height = (double)o.First["Height"],
                    FloorUnits = ToFloorUnits(o.First["FloorUnits"]),
                    Propertys = ParseProperty(o.First["Propertys"])
                };
                results.Add(floor);
            });
            return results;
        }

        private List<FloorUnit> ToFloorUnits(JToken jToken)
        {
            var results = new List<FloorUnit>();
            jToken.ForEach(o =>
            {
                var floorUnit = new FloorUnit
                {
                    Houses = ToHouses(o["Houses"]),
                    Propertys = ParseProperty(o["Propertys"])
                };
                results.Add(floorUnit);
            });
            return results;
        }

        private List<House> ToHouses(JToken jToken)
        {
            var results = new List<House>();
            jToken.ForEach(o =>
            {
                var house = new House
                {
                    HouseDoor = ParseHouseDoor(o["HouseDoor"]),
                    Rooms = ToRooms(o["Rooms"]),
                    Propertys = ParseProperty(o["Propertys"])
                };
                results.Add(house);
            });
            return results;
        }   

        private List<Room> ToRooms(JToken jToken)
        {
            var results = new List<Room>();
            jToken.ForEach(o =>
            {
                var room = new Room();
                o.ForEach(t =>
                {
                    room.Id = (string)t["Id"];
                    room.Name = (string)t["Name"];
                    room.Location = ToPoint3d(t["Location"]);
                    room.Edges = ToLines(t["Edges"]);
                    room.TopEdges = ToLines(t["Edges"]);
                    room.BottomEdges = ToLines(t["BottomEdges"]);
                    room.SpaceAttachElements = ToSpaceAttachElements(t["SpaceAttachElements"]);
                    room.Propertys = ParseProperty("Propertys");
                });
                results.Add(room);
            });
            return results;
        }
        private HouseDoor ParseHouseDoor(JToken jToken)
        {
            return new HouseDoor()
            {
                Id = (string)jToken["Id"],
                Category = (string)jToken["Category"],
                Bottom = (double)jToken["Bottom"],
                Height = (double)jToken["Height"],
                Location = ToPoint3d(jToken["Location"]),
                Lines = ToLines(jToken["Lines"]),
                Propertys = ParseProperty(jToken["Propertys"])
            };
        }
        private Dictionary<string,object> ParseProperty(JToken jObject)
        {
            var result = new Dictionary<string, object>();
            jObject.ForEach(o =>
            {
                result.Add(((JProperty)o).Name, ((JValue)((JProperty)o).Value).Value);
            });
            return result;
        }
        private Point3d? ToPoint3d(JToken jToken)
        {
            if(jToken!=null)
            {
                var x = (double)jToken["X"];
                var y = (double)jToken["Y"];
                var z = (double)jToken["Z"];
                return new Point3d(x, y, z);
            }
            else
            {
                return null;
            }            
        }
        private List<Line> ToLines(JToken jToken)
        {
            var results = new List<Line>();
            jToken.ForEach(o =>
            {
                var startPt = ToPoint3d(o["StartPoint"]);
                var endPt = ToPoint3d(o["EndPoint"]);
                results.Add(new Line(startPt.Value, endPt.Value));
            });
            return results;
        }
        private List<SpaceAttachElement> ToSpaceAttachElements(JToken jObject)
        {
            var results = new List<SpaceAttachElement>();
            jObject.ForEach(o => results.Add(ToSpaceAttachElement(o)));
            return results;
        }
        private SpaceAttachElement ToSpaceAttachElement(JToken jToken)
        {
            return new SpaceAttachElement()
            {
                Id = (string)jToken["Id"],
                Category = (string)jToken["Category"],
                Bottom = (double)jToken["Bottom"],
                Height = (double)jToken["Height"],
                Location = ToPoint3d(jToken["Location"]),
                Lines = ToLines(jToken["Lines"]),
                Propertys = ParseProperty(jToken["Propertys"])
            };            
        }
    }
}
