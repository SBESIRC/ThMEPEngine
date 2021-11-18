using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
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
        public static List<Polyline> GetAIHole(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            {
                var retHoles = new List<Polyline>();
                var tmpHoles = database.ModelSpace.OfType<Entity>().Where(o=>o.Layer.Contains("AI-洞口")).ToList();

                if(tmpHoles.Count != 0)
                {

                    var spatialIndex = new ThCADCoreNTSSpatialIndex(tmpHoles.ToCollection());

                    var filterObjs = spatialIndex.SelectCrossingPolygon(selectArea);

                    foreach (var hole in filterObjs)
                    {
                        if (hole is Polyline)
                        {
                            retHoles.Add(hole as Polyline);
                        }
                    }
                }
                return retHoles;
            }
        }

        public static Point3d GetPipeStartPt(Point3dCollection selectArea)
        {
            using (var database = AcadDatabase.Active())
            {
                Point3d retPt = new Point3d();
                var blks = database.ModelSpace.OfType<BlockReference>().Where(o => o.GetEffectiveName() == "AI-水管起点").ToList();
                if(blks.Count != 0)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(blks.ToCollection());
                    var filterObjs = spatialIndex.SelectCrossingPolygon(selectArea);

                    if(filterObjs.Count != 0)
                    {
                        var blk = filterObjs[0] as BlockReference;
                        retPt = blk.Position;
                    }
                }
                return retPt;
            }
        }
    }
}
