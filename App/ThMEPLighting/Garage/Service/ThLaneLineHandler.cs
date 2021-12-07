using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    internal abstract class ThLaneLineHandler
    {
        public abstract DBObjectCollection Handle(DBObjectCollection laneLines);
    }
    internal class ThTTypeLaneLineHandler : ThLaneLineHandler
    {
        public override DBObjectCollection Handle(DBObjectCollection laneLines)
        {
            throw new System.NotImplementedException();
        }
    }
}
