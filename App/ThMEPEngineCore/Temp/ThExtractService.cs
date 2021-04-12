using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public abstract class ThExtractService
    {
        public string ElementLayer { get; set; }
        public ThExtractService()
        {
            ElementLayer = "";
        }
        public abstract void Extract(Database db, Point3dCollection pts);
        public abstract bool IsElementLayer(string layer);
    }
}
