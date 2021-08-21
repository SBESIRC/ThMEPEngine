using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Data;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
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
        public static List<ThBuildRoom> GetBuildRoom(Point3dCollection selectArea)
        {
            var buildRoomService = new ThBuildRoomService();
            return buildRoomService.GetBuildRoom(selectArea);
        }
        public static List<ThBuildRoom> GetBuildRoom(Point3dCollection selectArea,string strKey)
        {
            string path = "D:\\DATA\\Git\\ThMEPEngine\\AutoLoader\\Contents\\Support\\上海地区住宅-安防配置表.xlsx";
            var buildRoomService = new ThBuildRoomService();
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(path, true);
            var tableTree = GetRoomTableTree(dataSet);
            var rooList = tableTree.CalRoomLst(strKey);
            return buildRoomService.GetBuildRoom(selectArea, rooList);
        }
        public static List<Line> ConnectLine(Point3dCollection selectArea)
        {
            var hydrantMainLineService = new ThHydrantPipeLineService();
            var lines = hydrantMainLineService.GetHydrantMainLine(selectArea);
            return lines;
        }
        public static void RemoveBranchLines(List<Line> branchLines, List<Line> loopLines, Point3dCollection selectArea)
        {
            var hydrantMainLineService = new ThHydrantPipeLineService();
            hydrantMainLineService.RemoveBranchLines(branchLines, loopLines, selectArea);
        }

        private static List<RoomTableTree> GetRoomTableTree(DataSet dataSet)
        {
            foreach (System.Data.DataTable table in dataSet.Tables)
            {
                if (table.TableName.Contains("房间名称处理"))
                {
                    return RoomConfigTreeService.CreateRoomTree(table);
                }
            }
            return null;
        }
    }
}
