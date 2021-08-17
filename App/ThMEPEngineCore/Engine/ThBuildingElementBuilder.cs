using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementBuilder
    {
        protected const double AREATOLERANCE = 1.0;
        protected const double BUFFERTOLERANCE = 1.0;
        public abstract List<ThRawIfcBuildingElementData> Extract(Database db);

        public abstract List<ThIfcBuildingElement> Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts);
        public abstract List<ThIfcBuildingElement> Build(Database db, Point3dCollection pts);
    }
}
