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
using ThMEPWSS.Sprinkler.Data;

namespace ThMEPWSS.HydrantLayout.Data
{
    internal class ThHydrantLayoutDataQueryService
    {
        //----input
        public List<ThExtractorBase> InputExtractors { get; set; }

        //----output
        public List<ThIfcVirticalPipe> THCVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> BlkVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcVirticalPipe> CVerticalPipe { get; set; } = new List<ThIfcVirticalPipe>();
        public List<ThIfcDistributionFlowElement> Hydrant { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Entity> Room { get; set; } = new List<Entity>(); //mpolygon //polyline
        public List<Entity> Wall { get; set; } = new List<Entity>(); //mpolygon //polyline
        public List<Polyline> Column { get; set; } = new List<Polyline>();
        public List<Polyline> Door { get; set; } = new List<Polyline>();
        public List<Polyline> FireProof { get; set; } = new List<Polyline>();
        public List<Entity> Car { get; set; } = new List<Entity>();
        public ThHydrantLayoutDataQueryService()
        {

        }

        public void ExtractData()
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

        public void Print()
        {
            THCVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0THCVerticalPipe", 140));
            BlkVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0blkVerticalPipe", 140));
            CVerticalPipe.ForEach(x => DrawUtils.ShowGeometry((x.Outline as DBPoint).Position, "l0cVerticalPipe", 140));
            Hydrant.ForEach(x => DrawUtils.ShowGeometry((x.Outline as BlockReference).Position, "l0Hydrant", 140));

            Wall.ForEach(x => DrawUtils.ShowGeometry(x, "l0wall", 1));
            Column.ForEach(x => DrawUtils.ShowGeometry(x, "l0column", 3));
            Door.ForEach(x => DrawUtils.ShowGeometry(x, "l0door", 6));
            FireProof.ForEach(x => DrawUtils.ShowGeometry(x, "l0fireProof", 6));
            Room.ForEach(x => DrawUtils.ShowGeometry(x, "l0room", 30));

            Car.ForEach(x => DrawUtils.ShowGeometry(x, "l0car", 173));
        }

        public void Clean()
        {
            THCVerticalPipe.ForEach(x => CleanEntity(x.Data));
            BlkVerticalPipe.ForEach(x => CleanEntity(x.Data));
            CVerticalPipe.ForEach(x => CleanEntity(x.Data));
            Hydrant.ForEach(x => CleanEntity(x.Outline));

        }

        /// <summary>
        /// 无法删除块中的entity
        /// </summary>
        /// <param name="e"></param>
        private void CleanEntity(Entity e)
        {
            var dbTrans = new DBTransaction();
            var objId = e.ObjectId;
            var obj = dbTrans.GetObject(objId, OpenMode.ForWrite, false);
            obj.UpgradeOpen();
            obj.Erase();
            obj.DowngradeOpen();
            dbTrans.Commit();
            // Data.Remove(x);
        }

    }
}
