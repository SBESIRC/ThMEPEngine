using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.Command
{
    public class ThFirstFloorDrainageCmd : IAcadCommand, IDisposable
    {
        Dictionary<string, List<string>> config;
        public ThFirstFloorDrainageCmd(Dictionary<string, List<string>> dic)
        {
            config = dic;
        }

        public void Execute()
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var frameDic = CalStructrueService.GetFrame(acad);
                var pt = frameDic.First().Key.StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                foreach (var dic in frameDic)
                {
                    var frame = dic.Key;
                    var thRooms = frame.GetRoomInfo(acad, originTransformer);
                    var userOutFrame = frame.GetUserFrame(acad);
                    frame.GetStructureInfo(acad, out List<Polyline> columns, out List<Polyline> walls);
                    var roomWalls = CalStructrueService.GetRoomWall(thRooms.Select(x => x.Boundary as Polyline).ToList(), userOutFrame);
                    var holeWalls = CutWallByUserOutFrame(userOutFrame, walls, roomWalls);
                    var verticalPipe = frame.RecognizeVerticalPipe(acad);
                    var drainingEquipment = frame.RecognizeSanitaryWarePipe(config, walls);
                    var sewagePipes = frame.GetSewageDrainageMainPipe(acad);
                    var rainPipes = frame.GetRainDrainageMainPipe(acad);

                    CreateDrainagePipeRoute createDrainageRoute = new CreateDrainagePipeRoute(frame,sewagePipes, verticalPipe, drainingEquipment, holeWalls);
                    var routes = createDrainageRoute.Routing();
                }
            }
            
        }

        /// <summary>
        /// 用出户框线切割墙
        /// </summary>
        /// <param name="userOutFrame"></param>
        /// <param name="walls"></param>
        /// <param name="roomWalls"></param>
        /// <returns></returns>
        private List<Polyline> CutWallByUserOutFrame(List<Polyline> userOutFrame, List<Polyline> walls, List<MPolygon> roomWalls)
        {
            var wallCurves = new List<Entity>(walls);
            wallCurves.AddRange(roomWalls);
            var dbObjs = wallCurves.ToCollection().UnionPolygons(true);
            var allWalls = new List<Entity>();
            foreach (Entity obj in dbObjs)
            {
                var interOutFrames = userOutFrame.Where(x => x.Intersects(obj)).ToList();
                if (interOutFrames.Count > 0)
                {
                    var bufferFrameDic = interOutFrames.ToDictionary(x => x, y => y.ExtendByLengthLine(100));
                    if (obj is MPolygon mPolygon)
                    {
                        allWalls.AddRange(mPolygon.Difference(bufferFrameDic.Values.ToCollection()).Cast<Entity>().ToList());
                    }
                    else if (obj is Polyline polyline)
                    {
                        allWalls.AddRange(polyline.Difference(bufferFrameDic.Values.ToCollection()).OfType<Entity>().ToList());
                    }
                }
            }
            return StructGeoService.GetWallPolylines(allWalls);
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
