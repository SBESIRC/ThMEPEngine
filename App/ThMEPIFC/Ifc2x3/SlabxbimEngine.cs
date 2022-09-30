using Xbim.Common;
using Xbim.IO.Memory;
using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;

namespace ThMEPIFC.Ifc2x3
{
    public class SlabxbimEngine
    {
        public IXbimGeometryEngine geomEngine;
        public IEntityFactory ef;
        public MemoryModel memoryModel;
        public SlabxbimEngine()
        {
            geomEngine = new XbimGeometryEngine();
            ef = new Xbim.Ifc2x3.EntityFactoryIfc2x3();
            memoryModel = new MemoryModel(ef);
        }
    }
}
