using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.FastAStarAlgorithm
{
    public enum CompassDirections
    {
        NotSet = 0,
        UP = 1,     //UP
        //NorthEast = 2, //UP Right
        Down = 2,   //Down
        //SouthEast = 4,
        Left = 3,   //Left
        //SouthWest = 6,
        Right = 4,  //Right
        //NorthWest = 8
    }
}
