using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm
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
