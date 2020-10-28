﻿using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries.Prepared;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPXClipInfo
    {
        public Polyline Polygon { get; set; }
        public bool Inverted { get; set; }

        public bool IsValid
        {
            get
            {
                return Polygon != null;
            }
        }

        public bool Contains(Point3d point)
        {
            var dbpoint = new DBPoint(point);
            return PreparedPolygon.Contains(dbpoint.ToNTSPoint());
        }

        public bool Contains(Curve curve)
        {
            return PreparedPolygon.Contains(curve.ToNTSGeometry());
        }

        private IPreparedGeometry preparedPolygon;
        private IPreparedGeometry PreparedPolygon
        {
            get
            {
                if (preparedPolygon == null)
                {
                    preparedPolygon = PreparedGeometryFactory.Prepare(Polygon.ToNTSPolygon());
                }
                return preparedPolygon;
            }
        }

        public void TransformBy(Matrix3d mat)
        {
            if (IsValid)
            {
                Polygon.TransformBy(mat);
            }
        }
    }
}
