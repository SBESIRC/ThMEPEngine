using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc;
using Xbim.Common.Step21;

namespace ThMEPIFC.Ifc2x3
{
    public class ThXbimSlabEngine
    {
        public IfcStore Model { get; private set; }
        public IXbimGeometryEngine Engine { get; private set; }
        public ThXbimSlabEngine()
        {
            Engine = new XbimGeometryEngine();
            Model = IfcStore.Create(IfcSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
        }
    }
}
