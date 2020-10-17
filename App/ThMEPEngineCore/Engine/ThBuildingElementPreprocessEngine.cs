using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementPreprocessEngine
    {
        protected bool IsCollinear(ThIfcLineBeam beam, ThIfcLineBeam other)
        {
            return (beam.Uuid == other.Uuid) ||
                ThMEPNTSExtension.IsLooseCollinear(
                beam.StartPoint,
                beam.EndPoint,
                other.StartPoint,
                other.EndPoint);
        }

        protected bool IsOverlap(ThIfcLineBeam beam, ThIfcLineBeam other)
        {
            return (beam.Uuid == other.Uuid) ||
                    ThMEPNTSExtension.IsLooseOverlap(
                    beam.StartPoint,
                    beam.EndPoint,
                    other.StartPoint,
                    other.EndPoint);
        }
    }
}
