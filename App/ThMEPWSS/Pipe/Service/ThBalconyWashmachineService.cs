using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Model.Plumbing;


namespace ThMEPWSS.Pipe.Service
{
   public class ThBalconyWashMachineService
    {
        private List<ThIfcWashMachine> WashmachineList { get; set; }
        private ThIfcSpace BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex WashmachineSpatialIndex { get; set; }
        public List<ThIfcWashMachine> Washmachines
        {
            get;
            set;
        }
        private ThBalconyWashMachineService(
           List<ThIfcWashMachine> washmachineList,
           ThIfcSpace balconySpace,
           ThCADCoreNTSSpatialIndex washmachineSpatialIndex)
        {
            WashmachineList = washmachineList;
            BalconySpace = balconySpace;
            WashmachineSpatialIndex = washmachineSpatialIndex;
            Washmachines = new List<ThIfcWashMachine>();
            if (WashmachineSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                WashmachineList.ForEach(o => dbObjs.Add(o.Outline));
                WashmachineSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThBalconyWashMachineService Find(
         List<ThIfcWashMachine> washmachineList,
         ThIfcSpace balconySpace,
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
            var includedWashmachines = crossWashmachines.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            });
            includedWashmachines.ForEach(o => Washmachines.Add(o));
        }
        private bool Contains(Polyline polyline, Polygon polygon)
        {
            return polyline.ToNTSPolygon().Contains(polygon);
        }
    }
}
