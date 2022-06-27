using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThLightingLinkInfo
    {
        public Line FirstEdge { get; private set; }
        public Line SecondEdge { get; private set; }
        public string Number { get; private set; }

        public ThLightingLinkInfo(Line firstEdge, Line secondEdge, string number)
        {
            FirstEdge = firstEdge;
            SecondEdge = secondEdge;
            Number = number;
        }

        public bool Equals(ThLightingLinkInfo other)
        {
            return Number.Equals(other.Number)
                && (FirstEdge.Equals(other.FirstEdge) && SecondEdge.Equals(other.SecondEdge)
                || SecondEdge.Equals(other.FirstEdge) && FirstEdge.Equals(other.SecondEdge));
        }
    }
}
