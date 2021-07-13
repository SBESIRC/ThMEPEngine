﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.HydrantConnectPipe.Command;
using ThMEPWSS.HydrantConnectPipe.Engine;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public static class ThHydrantDataManager
    {
        public static List<ThHydrant> GetFireHydrants(Point3dCollection selectArea)
        {
            ThHydrantService fireHydrantService = new ThHydrantService();
            return fireHydrantService.GetFireHydrant(selectArea);
        }
        public static List<ThHydrantPipe> GetFireHydrantPipes(Point3dCollection selectArea)
        {
            var pipeService = new ThHydrantPipeService();
            return pipeService.GetFireHydrantPipe(selectArea);
        }
        public static void GetHydrantLoopAndBranchLines(ref List<Line> loopLines, ref List<Line> branchLines, Point3dCollection selectArea)
        {
            var hydrantMainLineService = new ThHydrantPipeLineService();
            hydrantMainLineService.GetHydrantLoopAndBranchLines(ref loopLines, ref branchLines, selectArea);
        }
        public static List<ThCivilAirWall> GetCivilAirWalls(Point3dCollection selectArea)
        {
            var civilAirWallService = new ThCivilAirWallService();
            return civilAirWallService.GetCivilAirWall(selectArea);
        }
        public static List<ThElectricWell> GetElectricWells(Point3dCollection selectArea)
        {
            var electricWellService = new ThElectricWellService();
            return electricWellService.GetElectricWell(selectArea);
        }
        public static List<ThFireShutter> GetFireShutters(Point3dCollection selectArea)
        {
            var fireShutters = new List<ThFireShutter>();
            return fireShutters;
        }
        public static List<ThShearWall> GetShearWalls(Point3dCollection selectArea)
        {
            ThShearWallService shearWallService = new ThShearWallService();
            return shearWallService.GetWallEdges(selectArea);
        }
        public static List<ThStairsRoom> GetStairsRooms(Point3dCollection selectArea)
        {
            var stairsRoomService = new ThStairsRoomService();
            return stairsRoomService.GetStairsRoom(selectArea);
        }
        public static List<ThStructureCol> GetStructuralCols(Point3dCollection selectArea)
        {
            var structureColService = new ThStructureColService();
            return structureColService.GetStructureCols(selectArea);
        }
        public static List<ThStructureWall> GetStructureWalls(Point3dCollection selectArea)
        {
            var structureWallService = new ThStructureWallService();
            return structureWallService.GetStructureWalls(selectArea);
        }
        public static List<ThWindWell> GetWindWells(Point3dCollection selectArea)
        {
            var windWellService = new ThWindWellService();
            return windWellService.GetWindWell(selectArea);
        }
    }
}
