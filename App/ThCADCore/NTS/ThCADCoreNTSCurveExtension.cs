using ThCADExtension;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSCurveExtension
    {
        /// <summary>
        /// 按弦长细化
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Geometry TessellateWithChord(this Arc arc, double chord)
        {
            return arc.TessellateArcWithChord(chord).ToNTSLineString();
        }

        /// <summary>
        /// 按弧长细化
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Geometry TessellateWithArc(this Arc arc, double length)
        {
            return arc.TessellateArcWithArc(length).ToNTSLineString();
        }

        /// <summary>
        /// 按弦长细化
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="chord"></param>
        /// <returns></returns>
        public static Geometry TessellateWithChord(this Polyline polyline, double chord)
        {
            return polyline.TessellatePolylineWithChord(chord).ToNTSLineString();
        }

        /// <summary>
        /// 按弧长细化
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Geometry TessellateWithArc(this Polyline polyline, double length)
        {
            return polyline.TessellatePolylineWithArc(length).ToNTSLineString();
        }

        public static bool Overlaps(this Curve curve, Curve other)
        {
            return curve.ToNTSLineString().Overlaps(other.ToNTSLineString());
        }
    }
}
