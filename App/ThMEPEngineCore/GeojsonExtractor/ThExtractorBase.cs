using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public abstract class ThExtractorBase
    {
        public string Category { get; set; }      

        public short ColorIndex { get; set; }

        public bool UseDb3Engine { get; set; }
        public bool GroupSwitch { get; set; }
        public bool IsolateSwitch { get; set; }

        public ThExtractorBase()
        {
            Category = "";
            GroupSwitch = false;
            IsolateSwitch = false;
        }
        public abstract void Extract(Database database, Point3dCollection pts);
        public abstract List<ThGeometry> BuildGeometries();
        public virtual void SetRooms(List<ThIfcRoom> rooms)
        {
            //
        }
    }
    public enum SwitchStatus
    {
        Open,
        Close
    }
    public enum Privacy
    {
        Unknown,
        Private,
        Public
    }
}
