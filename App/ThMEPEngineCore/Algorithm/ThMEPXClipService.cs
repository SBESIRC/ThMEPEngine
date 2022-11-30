using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
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
                            try
                            {
                                if (fil != null && fil.Definition.Enabled)
                                {
                                    xClipInfo.Inverted = fil.Inverted;
                                    xClipInfo.Polygon = XClipPolygon(fil);
                                }
                            }
                            catch
                            {
                                // 在获取SpatialFilter.Definition时会抛出eNullObjectPointer异常
                                // 由于具体的原因未知，我们只能暂时捕捉这个异常，不做任何处理
                                // 由于无法正确获取SpatialFilter的信息，我们将忽略掉这个XClip
                            }
                        }
                    }
                }
                return xClipInfo;
            }
        }

        private static Polyline XClipPolygon(SpatialFilter filter)
        {
            var vertices = filter.Definition.GetPoints();
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
            poly.TransformBy(filter.ClipSpaceToWorldCoordinateSystemTransform);
            poly.TransformBy(filter.OriginalInverseBlockTransform);
            // Fix TAPD ID1000809：
            //  XClip剪裁区域可能会和区域内的对象贴合，导致对象被裁剪掉了
            //  解决方案：把XClip裁剪区域稍微扩大一点（100mm）
            return poly.Buffer(100).OfType<Polyline>().OrderByDescending(x => x.Area).First();
        }
    }
}