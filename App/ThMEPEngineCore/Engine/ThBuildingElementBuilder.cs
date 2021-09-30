using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementBuilder
    {
        protected const double AREATOLERANCE = 1.0;
        protected const double BUFFERTOLERANCE = 1.0;

        public List<ThIfcBuildingElement> Elements { get; set; }

        public abstract void Build(Database db, Point3dCollection pts);

        public abstract List<ThRawIfcBuildingElementData> Extract(Database db);

        public abstract List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts);
    }
}
