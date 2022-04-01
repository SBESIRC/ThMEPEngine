using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.ParkingStall.CAD
{
    public class InfoReader
    {
        private Point3dCollection m_previewWindow;
        private List<string> m_layerNames = new List<string>();
        private List<string> m_blockNames = new List<string>();
        public List<Polyline> ParkingStallPolys
        {
            get;
            set;
        } = new List<Polyline>();

        public InfoReader(Point3dCollection preViewWindow)
        {
            m_previewWindow = preViewWindow;
        }
        public static List<Polyline> MakeParkingStallPolys(Point3dCollection preViewWindow)
        {
            var infoReader = new InfoReader(preViewWindow);
            infoReader.Do();
            return infoReader.ParkingStallPolys;
        }

        public void Do()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var parkingStallRecognitionEngine = new ThParkingStallRecognitionEngine();
                GetLayerBlockNames(acadDb.Database);
                switch (ThParkingStallService.Instance.ParkingSource) 
                {
                    //case Common.EnumParkingSource.OnlyLayerName:
                    //    //仅图层获取
                    //    foreach (var layerName in m_layerNames)
                    //        parkingStallRecognitionEngine.Visitor.LayerFilter.Add(layerName);
                    //    m_blockNames.Clear();
                    //    m_layerNames.Clear();
                    //    break;
                    //case Common.EnumParkingSource.OnlyBlockName:
                    //    //仅通过图块名称获取
                    //    parkingStallRecognitionEngine.CheckQualifiedLayer = (Entity e) => true;
                    //    parkingStallRecognitionEngine.CheckQualifiedBlockName = CheckNameQualified;
                    //    m_layerNames.Clear();
                    //    break;
                    case Common.EnumParkingSource.BlokcAndLayer:
                        parkingStallRecognitionEngine.CheckQualifiedLayer = CheckLayerBlockNameQualified;
                        parkingStallRecognitionEngine.CheckQualifiedBlockName = CheckLayerBlockNameQualified;
                        break;
                    default:
                        throw new NotSupportedException();
                }
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
                        foreach (var entity in polyline.Buffer(ParkingStallCommon.ParkingPolyEnlargeLength))
                        {
                            if (entity is Polyline poly && poly.Closed)
                                ParkingStallPolys.Add(poly);
                        }
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
