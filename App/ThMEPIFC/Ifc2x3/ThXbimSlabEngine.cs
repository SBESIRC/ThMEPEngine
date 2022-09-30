using Xbim.Common;
using Xbim.Ifc2x3;
using Xbim.IO.Memory;
using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;

namespace ThMEPIFC.Ifc2x3
{
    public class ThXbimSlabEngine
    {
        public MemoryModel Model { get; private set; }
        public IEntityFactory Factory { get; private set; }
        public IXbimGeometryEngine Engine { get; private set; }
        public ThXbimSlabEngine()
        {
            Engine = new XbimGeometryEngine();
            Factory = new EntityFactoryIfc2x3();
            Model = new MemoryModel(Factory);
        }
    }
}
