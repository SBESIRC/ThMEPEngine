using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamBuilderEngine : ThBuildingElementBuilder, IDisposable
    {
        public ThBeamBuilderEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }

        public void Dispose()
        {

        }

        public DBObjectCollection Geometries
        {
            get
            {
                return Elements.Select(e => e.Outline).ToCollection();
            }
        }

        public override List<ThRawIfcBuildingElementData> Extract(Database db)
        {
            var res = new List<ThRawIfcBuildingElementData>();
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                return ExtractDB3Beam(db);
            }
            else
            {
                return ExtractRawBeam(db);
            }
        }

        private List<ThRawIfcBuildingElementData> ExtractDB3Beam(Database db)
        {
            var extraction = new ThDB3BeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        private List<ThRawIfcBuildingElementData> ExtractRawBeam(Database db)
        {
            var extraction = new ThRawBeamExtractionEngine();
            extraction.Extract(db);
            return extraction.Results;
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection pts)
        {
            ThBuildingElementRecognitionEngine recognitionEngine;
            if (Convert.ToInt16(Application.GetSystemVariable("USERR1")) == 0)
            {
                recognitionEngine = new ThDB3BeamRecognitionEngine();
            }
            else
            {
                recognitionEngine = new ThRawBeamRecognitionEngine();
            }
            recognitionEngine.Recognize(datas, pts);
            Elements = recognitionEngine.Elements;
        }

        public void ResetSpatialIndex(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            spatialIndex.Reset(Geometries);
        }

        public IEnumerable<ThIfcBuildingElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }

        public ThIfcBuildingElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }
    }
}
