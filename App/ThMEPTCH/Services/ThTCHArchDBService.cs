using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPTCH.Model;
using ThMEPTCH.TCHArchDataConvert;

namespace ThMEPTCH.Services
{
    public class ThTCHArchDBService
    {
        private TCHArchDBData archDBData;
        public ThTCHArchDBService(string dbPath) 
        {
            archDBData = new TCHArchDBData(dbPath);
        }
        public ThTCHProject TCHDBDataToProject() 
        {
            var thPrj = new ThTCHProject();
            thPrj.ProjectName = "测试项目";
            var thSite = new ThTCHSite();
            var thBuilding = new ThTCHBuilding();

            var buildingStorey = new ThTCHBuildingStorey();
            buildingStorey.Number = "1";
            buildingStorey.Height = 3000;
            buildingStorey.Elevation = 0.0;
            buildingStorey.Origin = new Point3d(0, 0,0);

            var entityConvert = new TCHDBEntityConvert();

            var dbWalls = archDBData.GetDBWallDatas();
            var dbDoors = archDBData.GetDBDoorDatas();
            var dbWindows = archDBData.GetDBWindowDatas();
            var walls = entityConvert.WallDoorWindowRelation(dbWalls, dbDoors, dbWindows);
            buildingStorey.Walls.AddRange(walls);
            thBuilding.Storeys.Add(buildingStorey);
            thSite.Building = thBuilding;
            thPrj.Site = thSite;
            return thPrj;
        }
    }
}
