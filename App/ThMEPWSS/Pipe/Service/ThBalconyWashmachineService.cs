using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyWashMachineService
    {
        public List<ThWWashingMachine> Washmachines { get; private set; }
        private List<ThWWashingMachine> WashmachineList { get; set; }
        private ThIfcRoom BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex WashmachineSpatialIndex { get; set; }
        private ThBalconyWashMachineService(
           List<ThWWashingMachine> washmachineList,
           ThIfcRoom balconySpace,
           ThCADCoreNTSSpatialIndex washmachineSpatialIndex)
        {
            WashmachineList = washmachineList;
            BalconySpace = balconySpace;
            WashmachineSpatialIndex = washmachineSpatialIndex;
            if (WashmachineSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                WashmachineList.ForEach(o => dbObjs.Add(o.Outline));
                WashmachineSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThBalconyWashMachineService Find(
         List<ThWWashingMachine> washmachineList,
         ThIfcRoom balconySpace,
         ThCADCoreNTSSpatialIndex washmachineSpatialIndex = null)
        {
            var instance = new ThBalconyWashMachineService(washmachineList, balconySpace, washmachineSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var balconyBoundary = BalconySpace.Boundary as Polyline;
            var crossObjs = WashmachineSpatialIndex.SelectCrossingPolygon(balconyBoundary);
            var crossWashmachines = WashmachineList.Where(o => crossObjs.Contains(o.Outline));
            Washmachines = crossWashmachines.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
        }
    }
}
