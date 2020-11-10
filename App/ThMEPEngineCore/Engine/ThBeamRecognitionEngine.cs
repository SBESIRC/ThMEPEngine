using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var beamTextDbExtension = new ThStructureBeamAnnotationDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                beamTextDbExtension.Annotations.ForEach(o => Elements.Add(ThIfcLineBeam.Create(o)));
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    Elements.ForEach(o => dbObjs.Add(o.Outline));
                    ThCADCoreNTSSpatialIndex beamSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(polygon);
                    var filterObjs = beamSpatialIndex.SelectCrossingPolygon(pline);
                    Elements=Elements.Where(o => filterObjs.Contains(o.Outline)).ToList();
                }
            }
        }
    }
}
