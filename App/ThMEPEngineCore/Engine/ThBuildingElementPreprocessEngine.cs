using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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

        protected List<ThIfcLineBeam> MergeBeams(List<ThIfcLineBeam> beams)
        {
            var results = new List<ThIfcLineBeam>();
            beams.Select(o => o.Outline)
                .ToCollection()
                .LooseBoundaries()
                .Cast<Polyline>()
                .ForEachDbObject(o =>
                {
                    var rectangle = o.GetMinimumRectangle();
                    results.Add(ThIfcLineBeam.Create(rectangle, beams.First().Height));
                });
            return results;
        }
    }
}
