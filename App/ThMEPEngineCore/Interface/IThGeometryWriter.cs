using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Interface
{
    internal interface IThGeometryWriter
    {
        string Write(List<ThGeometry> geos);
        void Write(List<ThGeometry> geos, string path);
    }
}
