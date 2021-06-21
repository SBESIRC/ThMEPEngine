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
            List<ThHydrant> fireHydrants = fireHydrantService.GetFireHydrant(selectArea);
            return fireHydrants;
        }
        public static List<ThHydrantPipe> GetFireHydrantPipes(Point3dCollection selectArea)
        {
            var pipeService = new ThHydrantPipeService();
            return pipeService.GetFireHydrantPipe(selectArea);
        }
        public static List<ThHydrantMainLine> GetHydrantMainLines(Point3dCollection selectArea)
        {
            List<ThHydrantMainLine> mainLines = new List<ThHydrantMainLine>();
            return mainLines;
        }
        public static List<ThHydrantBranchLine> GetHydrantBranchLines(Point3dCollection selectArea)
        {
            List<ThHydrantBranchLine> branchLines = new List<ThHydrantBranchLine>();
            return branchLines;
        }
        public static List<ThCivilAirWall> GetCivilAirWalls(Point3dCollection selectArea)
        {
            var airWalls = new List<ThCivilAirWall>();
            return airWalls;
        }
        public static List<ThElectricWell> GetElectricWells(Point3dCollection selectArea)
        {
            var eleWalls = new List<ThElectricWell>();
            return eleWalls;
        }
        public static List<ThFireShutter> GetFireShutters(Point3dCollection selectArea)
        {
            var fireShutters = new List<ThFireShutter>();
            return fireShutters;
        }
        public static List<ThShearWall> GetShearWalls(Point3dCollection selectArea)
        {
            var shaerWalls = new List<ThShearWall>();
            return shaerWalls;
        }
        public static List<ThStairsRoom> GetStairsRooms(Point3dCollection selectArea)
        {
            var stairsRooms = new List<ThStairsRoom>();
            return stairsRooms;
        }
        public static List<ThStructureCol> GetStructuralCols(Point3dCollection selectArea)
        {
            var structuralCols = new List<ThStructureCol>();
            return structuralCols;
        }
        public static List<ThStructureWall> GetStructureWalls(Point3dCollection selectArea)
        {
            var structureWalls = new List<ThStructureWall>();
            return structureWalls;
        }
        public static List<ThWindWell> GetWindWells(Point3dCollection selectArea)
        {
            var windWells = new List<ThWindWell>();
            return windWells;
        }
    }
}
