using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor.Service;



namespace ThPlatform3D.WallConstruction.Data
{
    internal class ThWallConstructionDataProcessService
    {
        public ThMEPOriginTransformer Transformer { get; set; }
        public List<Curve> Wall { get; set; } = new List<Curve>();
        public List<Curve> Door { get; set; } = new List<Curve>();
        public List<Curve> Axis { get; set; } = new List<Curve>();
        public List<Curve> FloorLevel { get; set; } = new List<Curve>();
        public List<Curve> Moldings { get; set; } = new List<Curve>();
        public List<Polyline> BreakLine { get; set; } = new List<Polyline>();
        public List<Entity> FloorNum { get; set; } = new List<Entity>();
        public ThWallConstructionDataProcessService()
        {

        }


        public void Print()
        {
            Wall.ForEach(x => DrawUtils.ShowGeometry(x, "l0wall", 30));
            Door.ForEach(x => DrawUtils.ShowGeometry(x, "l0door", 3));
            Axis.ForEach(x => DrawUtils.ShowGeometry(x, "l0axis", 1));
            FloorLevel.ForEach(x => DrawUtils.ShowGeometry(x, "l0floor", 1));
            Moldings.ForEach(x => DrawUtils.ShowGeometry(x, "l0moldings", 11));
            BreakLine.ForEach(x => DrawUtils.ShowGeometry(x, "l0breakLine", 6));
            FloorNum.ForEach(x => DrawUtils.ShowGeometry(x, "l0floorNum", 3));
        }
    }
}
