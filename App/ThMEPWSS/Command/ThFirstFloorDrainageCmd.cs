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
using ThMEPWSS.FirstFloorDrainagePlaneSystem.DrainingSetting;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Print;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.Command
{
    public class ThFirstFloorDrainageCmd : IAcadCommand, IDisposable
    {
        Dictionary<string, List<string>> config;
        ParamSettingViewModel paramSetting = null;

        public ThFirstFloorDrainageCmd(Dictionary<string, List<string>> dic, ParamSettingViewModel _paramSetting)
        {
            config = dic;
            paramSetting = _paramSetting;
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
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(new Autodesk.AutoCAD.Geometry.Point3d(0, 0, 0));
                foreach (var dic in frameDic)
                {
                    var frame = dic.Key.Clone() as Polyline;
                    originTransformer.Transform(frame);
                    var thRooms = frame.GetRoomInfo(acad, originTransformer);
                    var userOutFrame = frame.GetUserFrame(acad, originTransformer);
                    frame.GetStructureInfo(acad, out List<Polyline> columns, out List<Polyline> walls, originTransformer);
                    var rooms = CalAllRoomPolylines(thRooms);
                    var roomWalls = CalStructrueService.GetRoomWall(rooms.Keys.ToList(), userOutFrame);
                    var holeWalls = CutWallByUserOutFrame(userOutFrame, walls, roomWalls);
                    holeWalls.AddRange(columns);
                    var verticalPipe = frame.RecognizeVerticalPipe(acad, originTransformer);
                    if (paramSetting.SingleRowSetting != SingleRowSettingEnum.NotConsidered)        //不考虑一层出户不需要读取洁具立管
                    {
                        var drainingEquipment = dic.Key.RecognizeSanitaryWarePipe(config, walls, originTransformer);
                        verticalPipe.AddRange(drainingEquipment);
                    }
                    var sewagePipes = frame.GetSewageDrainageMainPipe(acad, originTransformer);
                    var rainPipes = frame.GetRainDrainageMainPipe(acad, originTransformer);
                    var gridLines = frame.GetAxis(acad, originTransformer);

                    CreateDrainagePipeRoute createDrainageRoute = new CreateDrainagePipeRoute(frame, sewagePipes, rainPipes, verticalPipe, holeWalls, gridLines, userOutFrame, rooms, paramSetting);
                    var routes = createDrainageRoute.Routing();
                    //处理冷凝水管

                    using (acad.Database.GetDocument().LockDocument())
                    {
                        HandlePipes(routes);
                        //foreach (var item in holeWalls)
                        //{
                        //    originTransformer.Reset(item);
                        //    acad.ModelSpace.Add(item);
                        //}
                        var otherPipes = routes.Where(x => x.verticalPipeType != VerticalPipeType.CondensatePipe).ToList();
                        PrintPipes.Print(otherPipes);
                    }
                }
            }
        }

        /// <summary>
        /// 冷凝水管间接排水
        /// </summary>
        /// <param name="routes"></param>
        private void HandlePipes(List<RouteModel> routes)
        {
            var condensatePipes = routes.Where(x => x.verticalPipeType == VerticalPipeType.CondensatePipe).ToList();
            DraningSettingService drainningSettingService = null;
            switch (paramSetting.IndirectDrainageSetting)
            {
                case DrainageSettingEnum.Tagging:
                    drainningSettingService = new DrainningSettingTaggingService(condensatePipes);
                    break;
                case DrainageSettingEnum.RainwaterInlet13:
                    drainningSettingService = new DrainningSettingRainwaterInlet(condensatePipes);
                    break;
                case DrainageSettingEnum.OutdoorWell:
                    drainningSettingService = new DrainningSettingSealedWellService(condensatePipes);
                    break;
                case DrainageSettingEnum.NotConsidered:
                default:
                    break;
            }
            if (drainningSettingService != null)
            {
                drainningSettingService.CreateDraningSetting();
            }
        }

        /// <summary>
        /// 获取房间所有的polyline
        /// </summary>
        /// <param name="thRooms"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<string>> CalAllRoomPolylines(List<ThIfcRoom> thRooms)
        {
            var polyDic = new Dictionary<Polyline, List<string>>();
            foreach (var room in thRooms)
            {
                if (room.Boundary is Polyline polyline)
                {
                    polyDic.Add(polyline, room.Tags);
                }
                else if (room.Boundary is MPolygon mPolygon)
                {
                    polyDic.Add(mPolygon.Shell(), room.Tags);
                    //roomPolys.AddRange(mPolygon.Holes());
                }
            }

            return polyDic;
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
                        var tempPoly = y.ExtendByLengthLine(100);
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