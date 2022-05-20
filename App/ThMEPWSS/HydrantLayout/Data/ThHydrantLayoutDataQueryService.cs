using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.CAD;

using ThMEPWSS.Sprinkler.Data;

using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Model;

namespace ThMEPWSS.HydrantLayout.Data
{
    internal class ThHydrantLayoutDataQueryService
    {
        //----input
        public List<ThExtractorBase> InputExtractors { get; set; }
        public List<ThIfcVirticalPipe> VerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();

        public List<ThIfcDistributionFlowElement> Hydrant { private get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> Room { get; set; } = new List<Entity>(); //mpolygon //polyline
        public List<Entity> Wall { get; set; } = new List<Entity>(); //mpolygon //polyline
        public List<Polyline> Column { get; set; } = new List<Polyline>();
        public List<Polyline> Door { get; set; } = new List<Polyline>();
        public List<Polyline> FireProof { get; set; } = new List<Polyline>();

        //----output
        public List<Polyline> Car { get; set; } = new List<Polyline>();
        public List<Polyline> Well { get; set; } = new List<Polyline>();

        public List<ThHydrantModel> HydrantModel { get; set; } = new List<ThHydrantModel>();

        public ThHydrantLayoutDataQueryService()
        {
        }

        public void ProcessArchitechData()
        {
            var architectureWallExtractor = InputExtractors.Where(o => o is ThSprinklerArchitectureWallExtractor).First() as ThSprinklerArchitectureWallExtractor;
            var shearWallExtractor = InputExtractors.Where(o => o is ThSprinklerShearWallExtractor).First() as ThSprinklerShearWallExtractor;
            var columnExtractor = InputExtractors.Where(o => o is ThSprinklerColumnExtractor).First() as ThSprinklerColumnExtractor;
            var doorExtractor = InputExtractors.Where(o => o is ThHydrantDoorExtractor).First() as ThHydrantDoorExtractor;
            var fireproofshutterExtractor = InputExtractors.Where(o => o is ThSprinklerFireproofshutterExtractor).First() as ThSprinklerFireproofshutterExtractor;
            var roomExtractor = InputExtractors.Where(o => o is ThSprinklerRoomExtractor).First() as ThSprinklerRoomExtractor;

            Wall.AddRange(architectureWallExtractor.Walls);
            Wall.AddRange(shearWallExtractor.Walls);
            Column.AddRange(columnExtractor.Columns);
            doorExtractor.Doors.ForEach(x => Door.Add(x.Outline as Polyline));
            FireProof.AddRange(fireproofshutterExtractor.FireproofShutter);
            roomExtractor.Rooms.ForEach(x => Room.Add(x.Boundary as Entity));

        }

        public void ProcessHydrant()
        {
            foreach (var h in Hydrant)
            {
                var model = ThHydrantModelService.CreateHydrantMode(h);
                if (model != null)
                {
                    HydrantModel.Add(model);
                }
            }
        }

        public void Transform(ThMEPOriginTransformer transformer)
        {
            VerticalPipe.ForEach(x => transformer.Transform(x.Outline));
            HydrantModel.ForEach(x => x.Transform(transformer));

            Wall.ForEach(x => transformer.Transform(x));
            Column.ForEach(x => transformer.Transform(x));
            Door.ForEach(x => transformer.Transform(x));
            FireProof.ForEach(x => transformer.Transform(x));
            Room.ForEach(x => transformer.Transform(x));

            Car.ForEach(x => transformer.Transform(x));
            Well.ForEach(x => transformer.Transform(x));
        }
        public void ProjectOntoXYPlane()
        {
            VerticalPipe.ForEach(x => x.Outline.ProjectOntoXYPlane());
            HydrantModel.ForEach(x => x.ProjectOntoXYPlane());
            Wall.ForEach(x => x.ProjectOntoXYPlane());
            Column.ForEach(x => x.ProjectOntoXYPlane());
            Door.ForEach(x => x.ProjectOntoXYPlane());
            FireProof.ForEach(x => x.ProjectOntoXYPlane());
            Room.ForEach(x => x.ProjectOntoXYPlane());

            Car.ForEach(x => x.ProjectOntoXYPlane());
            Well.ForEach(x => x.ProjectOntoXYPlane());
        }
        public void Reset(ThMEPOriginTransformer transformer)
        {
            VerticalPipe.ForEach(x => transformer.Reset(x.Outline));
            HydrantModel.ForEach(x => x.Reset(transformer));

            Wall.ForEach(x => transformer.Reset(x));
            Column.ForEach(x => transformer.Reset(x));
            Door.ForEach(x => transformer.Reset(x));
            FireProof.ForEach(x => transformer.Reset(x));
            Room.ForEach(x => transformer.Reset(x));

            Car.ForEach(x => transformer.Reset(x));
            Well.ForEach(x => transformer.Reset(x));

        }

        public void Print()
        {
            VerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0VerticalPipe", 140));
            HydrantModel.ForEach(x => DrawUtils.ShowGeometry(x.Center, "l0Hydrant", 140));
            HydrantModel.ForEach(x => DrawUtils.ShowGeometry(x.Outline, "l0Hydrant", 140));

            Wall.ForEach(x => DrawUtils.ShowGeometry(x, "l0wall", 1));
            Column.ForEach(x => DrawUtils.ShowGeometry(x, "l0column", 3));
            Door.ForEach(x => DrawUtils.ShowGeometry(x, "l0door", 6));
            FireProof.ForEach(x => DrawUtils.ShowGeometry(x, "l0fireProof", 6));
            Room.ForEach(x => DrawUtils.ShowGeometry(x, "l0room", 30));

            Car.ForEach(x => DrawUtils.ShowGeometry(x, "l0car", 173));
            Well.ForEach(x => DrawUtils.ShowGeometry(x, "l0well", 6));
        }
    }
}
