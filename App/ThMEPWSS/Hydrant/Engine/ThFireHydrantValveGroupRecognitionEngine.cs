using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
namespace ThMEPWSS.Hydrant.Engine
{


    public class ThFireHydrantValveGroupRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3RoomOutlineExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            var results = new List<ThRawIfcSpatialElementData>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                var filterObjs = spatialIndex.SelectCrossingPolygon(pline);
                results = datas.Where(o => filterObjs.Contains(o.Geometry as Curve)).ToList();
            }
            else
            {
                results = datas;
            }
            results.ForEach(o =>
            {
                if (o.Geometry is Polyline polyline && polyline.Area > 0.0)
                {
                    var curves = new DBObjectCollection() { polyline };
                    var simplifer = new ThRoomOutlineSimplifier();
                    curves = simplifer.Simplify(curves);
                    curves = simplifer.MakeValid(curves);
                    curves = simplifer.Simplify(curves);
                    if (curves.Count == 1)
                    {
                        var outline = curves[0] as Polyline;
                        var room = ThIfcRoom.Create(outline);
                        var properties = ThPropertySet.CreateWithHyperlink2(o.Data as string);
                        if (properties.Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY))
                        {
                            room.Name = properties.Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY];
                        }
                        Elements.Add(room);
                    }
                }
            });
        }

        public override void RecognizeMS(Database database, ObjectIdCollection dbObjs)
        {
            var engine = new ThDB3RoomOutlineExtractionEngine();
            engine.ExtractFromMS(database, dbObjs);
            Recognize(engine.Results, new Point3dCollection());
        }
    }

}