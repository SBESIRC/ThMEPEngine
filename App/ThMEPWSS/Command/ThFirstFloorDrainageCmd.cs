using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
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
using ThMEPEngineCore.Model;
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
                var frameDic = CalStructrueService.GetFrameByCrosing(acad);
                if (frameDic.Count <= 0)
                {
                    return;
                }
                var pt = frameDic.First().Key.StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                foreach (var dic in frameDic)
                {
                    var frame = dic.Key.Clone() as Polyline;
                    originTransformer.Transform(frame);
                    var thRooms = frame.GetRoomInfo(acad, originTransformer);
                    var userOutFrame = frame.GetUserFrame(acad, originTransformer);
                    frame.GetStructureInfo(acad, out List<Polyline> columns, out List<Polyline> walls, originTransformer);
                    var roomWalls = CalStructrueService.GetRoomWall(CalAllRoomPolylines(thRooms), userOutFrame);
                    var holeWalls = CutWallByUserOutFrame(userOutFrame, walls, roomWalls);
                    holeWalls.AddRange(columns);
                    var verticalPipe = frame.RecognizeVerticalPipe(acad, originTransformer);
                    var drainingEquipment = dic.Key.RecognizeSanitaryWarePipe(config, walls, originTransformer);
                    verticalPipe.AddRange(drainingEquipment);
                    var sewagePipes = frame.GetSewageDrainageMainPipe(acad, originTransformer);
                    var rainPipes = frame.GetRainDrainageMainPipe(acad, originTransformer);
                    var gridLines = frame.GetAxis(acad, originTransformer);

                    CreateDrainagePipeRoute createDrainageRoute = new CreateDrainagePipeRoute(frame, sewagePipes, rainPipes, verticalPipe, holeWalls, gridLines, userOutFrame);
                    var routes = createDrainageRoute.Routing();

                    using (acad.Database.GetDocument().LockDocument())
                    {
                        //foreach (var item in holeWalls)
                        //{
                        //    originTransformer.Reset(item);
                        //    acad.ModelSpace.Add(item);
                        //}
                        foreach (var item in routes)
                        {
                            var line = item.route;
                            originTransformer.Reset(line);
                            acad.ModelSpace.Add(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取房间所有的polyline
        /// </summary>
        /// <param name="thRooms"></param>
        /// <returns></returns>
        private List<Polyline> CalAllRoomPolylines(List<ThIfcRoom> thRooms)
        {
            var roomPolys = new List<Polyline>();
            foreach (var room in thRooms)
            {
                if (room.Boundary is Polyline polyline)
                {
                    roomPolys.Add(polyline);
                }
                else if (room.Boundary is MPolygon mPolygon)
                {
                    roomPolys.Add(mPolygon.Shell());
                    roomPolys.AddRange(mPolygon.Holes());
                }
            }

            return roomPolys;
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
                    var bufferFrameDic = interOutFrames.ToDictionary(x => x, y => {
                        var tempPoly = y.ExtendByLengthLine(150);
                        tempPoly = tempPoly.ExtendByLengthLine(-100, false);
                        return tempPoly;
                    });
                    if (obj is MPolygon mPolygon)
                    {
                        allWalls.AddRange(mPolygon.Difference(bufferFrameDic.Values.ToCollection()).Cast<Entity>().ToList());
                    }
                    else if (obj is Polyline polyline)
                    {
                        allWalls.AddRange(polyline.Difference(bufferFrameDic.Values.ToCollection()).OfType<Entity>().ToList());
                    }
                }
                else
                {
                    if (obj is MPolygon mPolygon)
                    {
                        allWalls.Add(mPolygon.Shell());
                    }
                    else if (obj is Polyline polyline)
                    {
                        allWalls.Add(polyline);
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
