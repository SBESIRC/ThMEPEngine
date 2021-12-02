using System.Collections.Generic;

namespace ThMEPEngineCore.Engine
{
    public class ThSpatialElementVisitorManager
    {
        public List<ThSpatialElementExtractionVisitor> Visitors { get; protected set; }

        public void Add(ThSpatialElementExtractionVisitor visitor)
        {
            Visitors.Add(visitor);
        }

        public ThSpatialElementVisitorManager()
        {
            Visitors = new List<ThSpatialElementExtractionVisitor>();
        }
    }
}
