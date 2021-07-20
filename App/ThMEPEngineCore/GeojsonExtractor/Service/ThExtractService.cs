using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public abstract class ThExtractService
    {
        public string ElementLayer { get; set; }
        public List<System.Type> Types { get; set; }
        public string[] SplitLayers
        {
            get
            {
                return ElementLayer.Split(',');
            }
        }
        public ThExtractService()
        {
            ElementLayer = "";
            Types = new List<System.Type>();
        }
        public abstract void Extract(Database db, Point3dCollection pts);
        public virtual bool IsElementLayer(string layer)
        {
            foreach(string single in SplitLayers)
            {
                if(single.ToUpper()== layer.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }
        protected bool IsValidType(Entity ent)
        {
            return Types.Contains(ent.GetType());
        }
    }
}
