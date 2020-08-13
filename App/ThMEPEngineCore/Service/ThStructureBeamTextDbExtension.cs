using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThStructureBeamTextDbExtension : ThStructureDbExtension, IDisposable
    {
        public List<DBText> BeamTexts { get; set; }
        public ThStructureBeamTextDbExtension(Database db) : base(db)
        {
            BeamTexts = new List<DBText>();
            LayerFilter = ThBeamLayerManager.GeometryXrefLayers(db);
        }
        public void Dispose()
        {
            foreach (var text in BeamTexts)
            {
                text.Dispose();
            }
            BeamTexts.Clear();
        }
        public override void BuildElementCurves()
        {
            throw new NotSupportedException();
        }
        public override void BuildElementTexts()
        {
            //Todo
            throw new NotImplementedException();
        }
    }
}
