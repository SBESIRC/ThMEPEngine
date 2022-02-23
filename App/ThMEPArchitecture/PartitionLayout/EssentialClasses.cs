using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilities;

namespace ThMEPArchitecture.PartitionLayout
{
    public class PartitionBoundary : IEquatable<PartitionBoundary>
    {
        public List<Point3d> BoundaryVertices = new List<Point3d>();
        public PartitionBoundary(Point3dCollection pts)
        {
            BoundaryVertices = pts.Cast<Point3d>().ToList();
        }

        public bool Equals(PartitionBoundary other)
        {
            if (this.BoundaryVertices.Count != other.BoundaryVertices.Count) return false;
            var thisVertices = this.BoundaryVertices;
            var otherVertices = other.BoundaryVertices;
            for (int i = 0; i < this.BoundaryVertices.Count; i++)
            {
                if (!thisVertices[i].IsEqualTo(otherVertices[i])) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashcode = BoundaryVertices.Count;
            var thisVertices = this.BoundaryVertices;
            foreach (var vertex in thisVertices)
            {
                hashcode ^= vertex.GetHashCode();
            }
            return hashcode;
        }
    }

    public enum LayoutDirection : int
    {
        LENGTH = 0,
        HORIZONTAL = 1,
        VERTICAL = 2
    }

    public class GenerateLaneParas
    {
        public int SetNotBeMoved = -1;
        public int SetGStartAdjLane = -1;
        public int SetGEndAdjLane = -1;
        public List<Lane> LanesToAdd = new List<Lane>();
        public List<Polyline> CarBoxesToAdd = new List<Polyline>();
        public List<CarModule> CarModulesToAdd = new List<CarModule>();
        public List<CarBoxPlus> CarBoxPlusToAdd = new List<CarBoxPlus>();
        public void Dispose()
        {
            LanesToAdd.ForEach(e => e.Line.Dispose());
            CarBoxesToAdd.ForEach(e => e.Dispose());
            CarModulesToAdd.ForEach(e => e.Line.Dispose());
            CarModulesToAdd.ForEach(e => e.Box.Dispose());
        }
    }

    public class CarBoxPlus
    {
        public CarBoxPlus() { }
        public CarBoxPlus(Polyline box, bool isSingleForParallelExist = false)
        {
            Box = box;
            IsSingleForParallelExist = isSingleForParallelExist;
        }
        public Polyline Box;
        public bool IsSingleForParallelExist = false;
    }

    public class Lane
    {
        public Lane(Line line, Vector3d vec, bool canBeMoved = true)
        {
            Line = line;
            Vec = vec;
            CanBeMoved = canBeMoved;
        }
        public Line Line;
        public bool CanBeMoved;
        public Vector3d Vec;
        public bool GStartAdjLine = false;
        public bool GEndAdjLine = false;
        public bool CanExtend = true;
    }

    public class PerpModlues
    {
        public List<Line> Lanes;
        public int Mminindex;
        public int Count;
        public Vector3d Vec;
        public List<Polyline> Bounds;
    }

    public class CarModule
    {
        public CarModule() { }
        public CarModule(Polyline box, Line line, Vector3d vec)
        {
            Box = box;
            Line = line;
            Vec = vec;
        }
        public bool GenerateCars = true;
        public Polyline Box;
        public Line Line;
        public Vector3d Vec;
        public bool IsInBackBackModule = false;
    }

}