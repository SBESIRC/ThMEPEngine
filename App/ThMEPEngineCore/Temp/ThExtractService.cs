using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.Temp
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
        protected bool IsValidType(Entity ent)
        {
            return Types.Contains(ent.GetType());
        }
        public virtual bool IsElementLayer(string layer)
        {
            foreach (string elementLayer in SplitLayers)
            {
                if(layer.ToUpper() == elementLayer.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
