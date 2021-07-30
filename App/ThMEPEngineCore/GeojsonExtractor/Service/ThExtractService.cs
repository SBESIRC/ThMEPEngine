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
        public double TesslateLength { get; set; }
        public ThExtractService()
        {
            ElementLayer = "";
            TesslateLength = 10.0;
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
