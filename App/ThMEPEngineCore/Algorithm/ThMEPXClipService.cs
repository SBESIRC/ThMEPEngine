using System;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;

namespace ThMEPEngineCore.Algorithm
{
    public static class ThXClipService
    {
        private const string SPATIAL_KEY = "SPATIAL";
        private const string SPATIAL_FILTER = "ACAD_FILTER";

        public static ThMEPXClipInfo XClipInfo(this BlockReference blockRef)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockRef.Database))
            {
                ThMEPXClipInfo xClipInfo = new ThMEPXClipInfo();
                var extdict = acadDatabase.ElementOrDefault<DBDictionary>(blockRef.ExtensionDictionary);
                if (extdict != null && extdict.Contains(SPATIAL_FILTER))
                {
                    var fildict = acadDatabase.Element<DBDictionary>(extdict.GetAt(SPATIAL_FILTER));
                    if (fildict != null)
                    {
                        if (fildict.Contains(SPATIAL_KEY))
                        {
                            SpatialFilter fil = acadDatabase.Element<SpatialFilter>(fildict.GetAt(SPATIAL_KEY));
                            if (fil != null && fil.Definition.Enabled)
                            {
#if ACAD2012 || ACAD2014
                                bool isInverted = false;
#else
                                bool isInverted = fil.Inverted;
#endif

                                var polygon = acadDatabase.Database.XClipPolygon(fil.Definition.GetPoints(),
                                    fil.ClipSpaceToWorldCoordinateSystemTransform);
                                xClipInfo.Polygon = polygon;
                                xClipInfo.Inverted = isInverted;
                            }
                        }
                    }
                }
                return xClipInfo;
            }
        }


        private static Polyline XClipPolygon(this Database database, Point2dCollection vertices, Matrix3d mat)
        {
            var poly = new Polyline()
            {
                Closed = true,
            };
            if (vertices.Count == 2)
            {
                poly.CreateRectangle(vertices[0], vertices[1]);
            }
            else if (vertices.Count > 2)
            {
                poly.CreatePolyline(vertices);
            }
            else
            {
                throw new NotSupportedException();
            }
            poly.TransformBy(mat);
            return poly;
        }
    }
}