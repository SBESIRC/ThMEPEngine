using NetTopologySuite.Algorithm.Match;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSMatch
    {
        public static double SimilarityMeasure(this Polyline first, Polyline second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first.ToNTSPolygon(), second.ToNTSPolygon());
        }

        public static double SimilarityMeasure(this Polyline first, MPolygon second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first.ToNTSPolygon(), second.ToNTSPolygon());
        }

        public static double SimilarityMeasure(this MPolygon first, MPolygon second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first.ToNTSPolygon(), second.ToNTSPolygon());
        }

        public static double SimilarityMeasure(this MPolygon first, Polyline second)
        {
            var measure = new HausdorffSimilarityMeasure();
            return measure.Measure(first.ToNTSPolygon(), second.ToNTSPolygon());
        }

        public static bool IsSimilar(this Polyline first, Polyline second, double degree)
        {
            return first.SimilarityMeasure(second) >= degree;
        }

        public static bool IsSimilar(this Polyline first, MPolygon second, double degree)
        {
            return first.SimilarityMeasure(second) >= degree;
        }

        public static bool IsSimilar(this MPolygon first, MPolygon second, double degree)
        {
            return first.SimilarityMeasure(second) >= degree;
        }

        public static bool IsSimilar(this MPolygon first, Polyline second, double degree)
        {
            return first.SimilarityMeasure(second) >= degree;
        }        
    }
}
