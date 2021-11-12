using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThBuildElementExtractServiece
    {
        /// <summary>
        /// 提取剪力墙
        /// </summary>
        /// <param name="selectArea"></param>
        /// <returns></returns>
        public static List<ThFanShearWallModel> GetShearWalls(Point3dCollection selectArea)
        {
            var results = new List<ThFanShearWallModel>();
            using (var database = AcadDatabase.Active())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                shearWallEngine.Recognize(database.Database, selectArea);
                List<ThIfcWall> shearWalls = shearWallEngine.Elements.Cast<ThIfcWall>().ToList();
                foreach (var shearWall in shearWalls)
                {
                    results.Add(ThFanShearWallModel.Create(shearWall.Outline));
                }
            }
            return results;
        }
        /// <summary>
        /// 提取结构柱
        /// </summary>
        /// <param name="selectArea"></param>
        /// <returns></returns>
        public static List<ThFanColumnModel> GetColumns(Point3dCollection selectArea)
        {
            var results = new List<ThFanColumnModel>();
            using (var database = AcadDatabase.Active())
            using (var columnEngine = new ThColumnRecognitionEngine())
            {
                columnEngine.Recognize(database.Database, selectArea);
                List<ThIfcColumn> structureCols = columnEngine.Elements.Cast<ThIfcColumn>().ToList();
                foreach (var structureCol in structureCols)
                {
                    results.Add(ThFanColumnModel.Create(structureCol.Outline));
                }
            }
            return results;
        }
        /// <summary>
        /// 提取房间框线
        /// </summary>
        /// <param name="selectArea"></param>
        /// <returns></returns>
        public static List<ThFanRoomModel> GetBuildRooms(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            using (var roomEngine = new ThRoomBuilderEngine())
            {
                var rooms = roomEngine.BuildFromMS(database.Database, selectArea);
                List<ThFanRoomModel> buildRooms = new List<ThFanRoomModel>();
                foreach (var room in rooms)
                {
                    buildRooms.Add(ThFanRoomModel.Create(room.Boundary));
                }
                return buildRooms;
            }
        }
    }
}
