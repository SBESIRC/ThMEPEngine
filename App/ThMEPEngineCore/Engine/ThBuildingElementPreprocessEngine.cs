using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementPreprocessEngine
    {
        protected bool IsParallel(ThIfcLineBeam beam, ThIfcLineBeam other)
        {
            return ThGeometryTool.IsLooseParallel(
                beam.StartPoint,
                beam.EndPoint,
                other.StartPoint,
                other.EndPoint);
        }

        protected bool IsCollinear(ThIfcLineBeam beam, ThIfcLineBeam other)
        {
            return ThGeometryTool.IsLooseCollinear(
                beam.StartPoint,
                beam.EndPoint,
                other.StartPoint,
                other.EndPoint);
        }

        protected bool IsOverlap(ThIfcLineBeam beam, ThIfcLineBeam other)
        {
            return ThGeometryTool.IsLooseOverlap(
                beam.StartPoint,
                beam.EndPoint,
                other.StartPoint,
                other.EndPoint);
        }
    }
}
