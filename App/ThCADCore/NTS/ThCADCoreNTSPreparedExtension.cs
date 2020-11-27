﻿using System;
using DotNetARX;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSPreparedPolygon
    {

        private IPreparedGeometry PreparedPolygon { get; set; }

        public ThCADCoreNTSPreparedPolygon(AcPolygon polygon)
        {
            PreparedPolygon = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(polygon.ToNTSPolygon());
        }

        public bool Intersects(Curve curve)
        {
            return PreparedPolygon.Intersects(curve.ToNTSGeometry());
        }
        public bool Intersects(MPolygon mPolygon)
        {
            return PreparedPolygon.Intersects(mPolygon.ToNTSPolygon());
        }
    }
}
