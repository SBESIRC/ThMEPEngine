using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.MPartitionLayout
{
    public class MViewModel { }
    //public class Ramps { }
    public class Lane
    {
        public Lane(LineSegment line, Vector2D vec, bool canBeMoved = true)
        {
            Line = line;
            Vec = vec;
            CanBeMoved = canBeMoved;
        }
        public LineSegment Line;
        public bool CanBeMoved;
        public Vector2D Vec;
        public bool GStartAdjLine = false;
        public bool GEndAdjLine = false;
        public bool CanExtend = true;
    }
    public class CarBoxPlus
    {
        public CarBoxPlus() { }
        public CarBoxPlus(Polygon box, bool isSingleForParallelExist = false)
        {
            Box = box;
            IsSingleForParallelExist = isSingleForParallelExist;
        }
        public Polygon Box;
        public bool IsSingleForParallelExist = false;
    }
    public class CarModule
    {
        public CarModule() { }
        public CarModule(Polygon box, LineSegment line, Vector2D vec)
        {
            Box = box;
            Line = line;
            Vec = vec;
        }
        public bool GenerateCars = true;
        public Polygon Box;
        public LineSegment Line;
        public Vector2D Vec;
        public bool IsInBackBackModule = false;
        public bool IsInVertUnsureModule = false;
    }
    public class GenerateLaneParas
    {
        public int SetNotBeMoved = -1;
        public int SetGStartAdjLane = -1;
        public int SetGEndAdjLane = -1;
        public List<Lane> LanesToAdd = new List<Lane>();
        public List<Polygon> CarBoxesToAdd = new List<Polygon>();
        public List<CarModule> CarModulesToAdd = new List<CarModule>();
        public List<CarBoxPlus> CarBoxPlusToAdd = new List<CarBoxPlus>();
    }
    public class PerpModlues
    {
        public List<LineSegment> Lanes;
        public int Mminindex;
        public int Count;
        public Vector2D Vec;
        public List<Polygon> Bounds;
        public bool IsInVertUnsureModule = true;
    }
    public class InfoCar
    {
        public InfoCar(Polygon car, Coordinate point, Vector2D vector)
        {
            Polyline = car;
            Point = point;
            Vector = vector;
        }
        public int CarLayoutMode = 0;
        public Vector2D Vector;
        public Coordinate Point;
        public Polygon Polyline;
    }
    public enum CarLayoutMode : int
    {
        VERT = 0,
        PARALLEL = 1,
    }
}
