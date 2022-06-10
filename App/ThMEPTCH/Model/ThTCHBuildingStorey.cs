using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHBuildingStorey : ThIfcBuildingStorey
    {
        public string FloorNum { get; set; }
        public double FloorElevation { get; set; }
        public double FloorHeight { get; set; }
        public Point3d FloorOrigin { get; set; }
        public ThTCHBuildingStorey()
        {
            ThTCHWalls = new List<ThTCHWall>();
            ThTCHWindows = new List<ThTCHWindow>();
            ThTCHDoors = new List<ThTCHDoor>();
            ThTCHSlabs = new List<ThTCHSlab>();
        }
        public List<ThTCHWall> ThTCHWalls { get; set; }
        public List<ThTCHWindow> ThTCHWindows { get; set; }
        public List<ThTCHDoor> ThTCHDoors { get; set; }
        public List<ThTCHSlab> ThTCHSlabs { get; set; }
    }
}
