using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPWSS.Diagram.ViewModel;

namespace ThMEPWSS.ExerciseProj
{
    public class HorizontalPipe
    {
        public HorizontalPipe(Entity entity)
        {
            Entity=entity;
        }
        public Entity Entity { get; set; }
    }
    public class BlkRain
    {
        public BlkRain(Entity entity)
        {
            Entity = entity;
        }
        public Entity Entity { get; set; }

    }
    

    public class VerticlePipe
    {
        public VerticlePipe(Circle circle)
        {
            Circle=circle;
        }
        public Circle Circle { get; set; }
    }
    public class Pump
    {
        public Pump(BlockReference block)
        {
            Block = block;
            Bound = GetBound();
            Position = Block.Position;
        }
        public BlockReference Block { get; set; }
        public Polyline Bound { get; set; }
        public Point3d Position { get; set; }
        private Polyline GetBound()
        {
            Polyline newline = new Polyline();
            var ext = Block.Bounds;
            if (ext != null)
                return ((Extents3d)ext).ToRectangle();
            else
                return newline;
        }
    }
}
