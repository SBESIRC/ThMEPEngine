using System.Collections.Generic;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.PublicMethod
{
    public static class CompassDirectionsHelper
    {
        private static List<CompassDirections> AllCompassDirections = new List<CompassDirections>();

        #region Static Ctor
        static CompassDirectionsHelper()
        {
            CompassDirectionsHelper.AllCompassDirections.Add(CompassDirections.Left);
            CompassDirectionsHelper.AllCompassDirections.Add(CompassDirections.Right);
            CompassDirectionsHelper.AllCompassDirections.Add(CompassDirections.Down);
            CompassDirectionsHelper.AllCompassDirections.Add(CompassDirections.UP);
        }
        #endregion

        #region GetAllCompassDirections
        public static List<CompassDirections> GetAllCompassDirections()
        {
            return CompassDirectionsHelper.AllCompassDirections;
        }
        #endregion
    }
}
