using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override List<ThRawIfcBuildingElementData> Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var beamTextDbExtension = new ThStructureBeamAnnotationDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                return beamTextDbExtension.Annotations.Select(o => new ThRawIfcBuildingElementData()
                {
                    Data = o,
                }).ToList();
            }
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            datas.ForEach(o => Elements.Add(ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation)));
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
                Elements = Elements.Where(o => filterObjs.Contains(o.Outline)).ToList();
            }

        }
    }
}
