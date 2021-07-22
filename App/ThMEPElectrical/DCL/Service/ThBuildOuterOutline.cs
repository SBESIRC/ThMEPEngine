using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.DCL.Service
{
    public abstract class ThBuildOuterOutline
    {
        public List<Polyline> OuterOutlineList { get; set; }
        public List<Polyline> InnerOutlineList { get; set; }
        public string HoleLayer { get; set; }
        public ModelData ModelData { get; protected set; }
        public ThBuildOuterOutline()
        {
            HoleLayer = "";
            OuterOutlineList = new List<Polyline>();
            InnerOutlineList = new List<Polyline>();
        }
        public abstract void Extract(Database db,Point3dCollection pts);
        public virtual void ExtractHoles(Database db, Point3dCollection pts)
        {
            //提取内庭院洞线
            var polyExtract = new ThExtractPolylineService()
            {
                ElementLayer = HoleLayer,
            };
            polyExtract.Extract(db, pts);
            InnerOutlineList = polyExtract.Polys;
        }
    }
}
