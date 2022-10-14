using Xbim.Ifc4.Interfaces;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc;

namespace ThMEPIFC.Ifc2x3
{
    public class ThXbimSlabEngine
    {
        public IfcStore Model { get; private set; }
        public IXbimGeometryEngine Engine { get; private set; }
        public ThXbimSlabEngine()
        {
            Engine = new XbimGeometryEngine();
            Model = ThIFC2x3Factory.CreateMemoryModel();
        }
    }
}
