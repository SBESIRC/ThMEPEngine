using Linq2Acad;
using DotNetARX;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using ThCADExtension;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.CAD
{
    public static partial class ThTCHDbExtension
    {
        public static ThRawIfcFlowSegmentData LoadCableCarrierSegmentFromDb(this Database database, ObjectId tch, Matrix3d matrix, double arctesslateLength = 10.0)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curve = GetCurve(tch);
                return new ThRawIfcFlowSegmentData()
                {
                    Geometry = GetCableCarrierSegmentOutline(curve, matrix, arctesslateLength),
                };
            }
        }

        public static ThRawIfcFlowFittingData LoadTCHCableCarrierFitting(this Database database, ObjectId tch, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curve = GetCurve(tch);
                return new ThRawIfcFlowFittingData()
                {
                    Geometry = GetCableCarrierFittingOutline(curve, matrix),
                };
            }
        }

        public static Curve GetTCHCableCarrierSegmentCenterLine(this Curve curve, Matrix3d matrix)
        {
            return GetTCHCableCarrierSegmentCenterLine(curve, matrix);
        }

        public static Curve GetTCHCableCarrierSegmentOutLine(this Curve curve, Matrix3d matrix,double arctesslateLength = 10.0)
        {
            return GetCableCarrierSegmentOutline(curve, matrix, arctesslateLength);
        }        

        public static Curve GetTCHCableCarrierFittingOutline(this Curve curve, Matrix3d matrix)
        {
            return GetCableCarrierFittingOutline(curve, matrix);            
        }

        private static Curve GetCableCarrierSegmentCenterLine(Curve curve, Matrix3d matrix)
        {
            var line = new Line(curve.StartPoint, curve.EndPoint);
            return line.GetTransformedCopy(matrix) as Curve;
        }

        private static Curve GetCableCarrierFittingOutline(Curve curve, Matrix3d matrix)
        {
            var vertices = new Point3dCollection();
            for (double parameter = curve.StartParam; parameter < curve.EndParam; parameter += 1.0)
            {
                vertices.Add(curve.GetPointAtParameter(parameter));
            }
            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(vertices);
            return pline.GetTransformedCopy(matrix) as Curve;
        }
        private static Curve GetCableCarrierSegmentOutline(Curve curve, Matrix3d matrix,double arctesslateLength=10.0)
        {
            var curves = new DBObjectCollection();
            curve.Explode(curves);
            var handleCurves = new DBObjectCollection();
            curves.OfType<Curve>().ForEach(c =>
            {
                if(c is Line line)
                {
                    handleCurves.Add(line.ExtendLine(1.0));
                }
                else if(c is Arc arc)
                {
                    handleCurves.Add(arc.TessellateArcWithArc(arctesslateLength));
                }
            });
            var transformer = new ThMEPOriginTransformer(handleCurves);
            transformer.Transform(handleCurves);
            var polygons  = handleCurves.PolygonsEx();
            transformer.Reset(polygons);
            transformer.Reset(handleCurves);
            var polylines = new List<Polyline>();
            polygons.OfType<Entity>().ForEach(e =>
            {
                if(e is Polyline poly)
                {
                    polylines.Add(poly);
                }
                else if(e is MPolygon polygon)
                {
                    polylines.Add(polygon.Shell());
                    polylines.AddRange(polygon.Holes());
                }
            });
            if (polylines.Count>0)
            {
                return polylines.OrderByDescending(p=>p.Area).First();
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }
    }
}
