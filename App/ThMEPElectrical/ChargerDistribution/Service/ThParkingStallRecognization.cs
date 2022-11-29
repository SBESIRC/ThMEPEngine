using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public class ThParkingStallRecognization
    {
        private Point3dCollection m_previewWindow;
        private List<string> m_layerNames = new List<string>();
        private List<string> m_blockNames = new List<string>();
        public List<Polyline> ParkingStallPolys
        {
            get;
            set;
        } = new List<Polyline>();

        public ThParkingStallRecognization(Point3dCollection preViewWindow)
        {
            m_previewWindow = preViewWindow;
        }

        public static List<Polyline> MakeParkingStallPolys(Point3dCollection preViewWindow)
        {
            var infoReader = new ThParkingStallRecognization(preViewWindow);
            infoReader.Recognize();
            return infoReader.ParkingStallPolys;
        }

        public void Recognize()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var parkingStallRecognitionEngine = new ThParkingStallRecognitionEngine();
                GetLayerBlockNames(acadDb.Database);
                parkingStallRecognitionEngine.CheckQualifiedLayer = CheckLayerBlockNameQualified;
                parkingStallRecognitionEngine.CheckQualifiedBlockName = CheckLayerBlockNameQualified;
                var visitor = new ThLightingParkingStallVisitor()
                {
                    LayerFilter = ThParkingStallLayerManager.XrefLayers(acadDb.Database),
                };
                parkingStallRecognitionEngine.ParkingStallVisitor = visitor;
                parkingStallRecognitionEngine.Recognize(acadDb.Database, m_previewWindow);

                foreach (var space in parkingStallRecognitionEngine.Elements.Cast<ThIfcParkingStall>().ToList())
                {
                    if (space.Boundary is Polyline polyline)
                    {
                        ParkingStallPolys.Add(polyline);
                    }
                }
            }
        }

        private void GetLayerBlockNames(Database database)
        {
            m_layerNames = ThParkingStallLayerManager.XrefLayers(database);
            foreach (var layerName in ThParkingStallService.Instance.ParkingLayerNames)
            {
                if (m_layerNames.Any(c => c.Equals(layerName)))
                    continue;
                m_layerNames.Add(layerName);
            }
            m_blockNames.Clear();
            foreach (var blockName in ThParkingStallService.Instance.ParkingBlockNames)
            {
                string name = blockName.ToUpper();
                if (m_blockNames.Any(c => c.Equals(name)))
                    continue;
                m_blockNames.Add(name);
            }
        }

        private bool CheckLayerBlockNameQualified(Entity entity)
        {
            if (entity is BlockReference br)
            {
                var layerName = ThMEPEngineCore.Algorithm.ThMEPXRefService.OriginalFromXref(entity.Layer);
                var blockName = ThMEPEngineCore.Algorithm.ThMEPXRefService.OriginalFromXref(br.GetEffectiveName().ToUpper());
                if (m_layerNames.Any(c => c.Equals(layerName)) || m_blockNames.Any(c => c.Equals(blockName)))
                    return true;
            }
            return false;
        }
    }
}
