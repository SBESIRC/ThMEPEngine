using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Model
{
    public class Project
    {
        public string PrjId { get; set; } = "";
        public string ModelId { get; set; } = "";
        public string SubentryName { get; set; } = "";
        public List<Floor> Floors { get; set; } = new List<Floor>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }
    public class Floor
    {
        public string Name { get; set; } = "";
        public double Height { get; set; }
        public double Elevation { get; set; }
        public List<FloorUnit> FloorUnits { get; set; } = new List<FloorUnit>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }

    public class FloorUnit
    {
        public List<House> Houses { get; set; } = new List<House>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }

    public class House
    {
        public HouseDoor HouseDoor { get; set; } = new HouseDoor();
        public List<Room> Rooms { get; set; } = new List<Room>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }
    public class HouseDoor
    {
        public string Id { get; set; } = "";
        public string Category { get; set; } = "";
        public double Bottom { get; set; }
        public double Height { get; set; }
        public Point3d? Location { get; set; }
        public List<Line> Lines { get; set; } = new List<Line>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }
    public class Room
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public Point3d? Location { get; set; }
        public List<Line> Edges { get; set; } = new List<Line>();
        public List<Line> TopEdges { get; set; } = new List<Line>();
        public List<Line> BottomEdges { get; set; } = new List<Line>();
        public List<SpaceAttachElement> SpaceAttachElements { get; set; } = new List<SpaceAttachElement>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }
    public class SpaceAttachElement
    {
        public string Id { get; set; } = "";
        public string Category { get; set; } = "";
        public double Bottom { get; set; }
        public double Height { get; set; }
        public Point3d? Location { get; set; }
        public List<Line> Lines { get; set; } = new List<Line>();
        public Dictionary<string, object> Propertys { get; set; } = new Dictionary<string, object>();
    }
}
