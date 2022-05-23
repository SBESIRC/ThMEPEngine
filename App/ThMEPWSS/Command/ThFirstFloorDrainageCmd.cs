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
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Dimension;
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
        FirstFloorPlaneViewModel firstFloorPlane = null;
        public ThFirstFloorDrainageCmd(Dictionary<string, List<string>> dic, ParamSettingViewModel _paramSetting, FirstFloorPlaneViewModel _firstFloorPlane)
        {
            config = dic;
            paramSetting = _paramSetting;
            firstFloorPlane = _firstFloorPlane;
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
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(new Autodesk.AutoCAD.Geometry.Point3d());
                foreach (var dic in frameDic)
                {
                    var frame = dic.Key.Clone() as Polyline;
                    originTransformer.Transform(frame);
                    CalStructrueService.GetStructureInfo(acad, out List<Polyline> columns, out List<Polyline> walls, originTransformer);
                    var verticalPipe = frame.RecognizeVerticalPipe(acad, originTransformer);
                    if (paramSetting.SingleRowSetting != SingleRowSettingEnum.NotConsidered)        //不考虑一层出户不需要读取洁具立管
                    {
                        var drainingEquipment = dic.Key.RecognizeSanitaryWarePipe(config, walls, originTransformer);
                        verticalPipe.AddRange(drainingEquipment);
                    }
                    verticalPipe = FilterNeedRoutePipes(verticalPipe);
                    var thRooms = CalStructrueService.GetRoomInfo(acad, originTransformer);
                    var rooms = CalAllRoomPolylines(thRooms);
                    var userOutFrame = CalStructrueService.GetUserFrame(acad, originTransformer);
                    var deepRooms = HandleStructService.GetNeedStruct(rooms, userOutFrame, verticalPipe);       //拿到需要的房间（房间内有点位的房间）
                    var roomWalls = CalStructrueService.GetRoomWall(rooms.Keys.ToList());
                    var holeWalls = CutWallByUserOutFrame(userOutFrame, walls, roomWalls);
                    holeWalls.AddRange(columns);
                    var sewagePipes = CalStructrueService.GetSewageDrainageMainPipe(acad, originTransformer);
                    var rainPipes = CalStructrueService.GetRainDrainageMainPipe(acad, originTransformer);
                    var gridLines = CalStructrueService.GetAxis(acad, originTransformer);

                    CreateDrainagePipeRoute createDrainageRoute = new CreateDrainagePipeRoute(sewagePipes, rainPipes, verticalPipe, holeWalls, gridLines, userOutFrame, deepRooms, paramSetting, originTransformer);
                    var routes = createDrainageRoute.Routing();

                    using (acad.Database.GetDocument().LockDocument())
                    {
                        //套管标注
                        DrivepipeDimensionService drivepipeDimensionService = new DrivepipeDimensionService(routes, userOutFrame, firstFloorPlane, originTransformer);
                        drivepipeDimensionService.scale = Convert.ToDouble(firstFloorPlane.BlockScale.Tag);
                        drivepipeDimensionService.CreateDim();

                        //进行路由倒角
                        ChamferService chamferService = new ChamferService(routes);
                        routes = chamferService.Chamfer();

                        //放置管井
                        CreateTubeWellService createTubeWellService = new CreateTubeWellService(routes, originTransformer);
                        createTubeWellService.scale = Convert.ToDouble(firstFloorPlane.BlockScale.Tag);
                        routes = createTubeWellService.Layout();

                        //处理冷凝水管
                        HandlePipes(routes, originTransformer);

                        //标注管径
                        PipeDiameterMarkingService pipeDiameterMarkingService = new PipeDiameterMarkingService(routes, originTransformer);
                        pipeDiameterMarkingService.scale = Convert.ToDouble(firstFloorPlane.BlockScale.Tag);
                        pipeDiameterMarkingService.CreateDim();

                        var otherPipes = routes.Where(x => x.verticalPipeType != VerticalPipeType.CondensatePipe).ToList();
                        PrintPipes.Print(otherPipes, Convert.ToDouble(firstFloorPlane.BlockScale.Tag), originTransformer);
                    }
                }
            }
        }

        /// <summary>
        /// 冷凝水管间接排水
        /// </summary>
        /// <param name="routes"></param>
        private void HandlePipes(List<RouteModel> routes, ThMEPOriginTransformer originTransformer)
        {
            var condensatePipes = routes.Where(x => x.verticalPipeType == VerticalPipeType.CondensatePipe && !x.IsBranchPipe).ToList();
            DraningSettingService drainningSettingService = null;
            switch (paramSetting.IndirectDrainageSetting)
            {
                case DrainageSettingEnum.Tagging:
                    drainningSettingService = new DrainningSettingTaggingService(condensatePipes, originTransformer);
                    break;
                case DrainageSettingEnum.RainwaterInlet13:
                    drainningSettingService = new DrainningSettingRainwaterInlet(condensatePipes, originTransformer);
                    break;
                case DrainageSettingEnum.OutdoorWell:
                    drainningSettingService = new DrainningSettingSealedWellService(condensatePipes, originTransformer);
                    break;
                case DrainageSettingEnum.NotConsidered:
                default:
                    break;
            }
            if (drainningSettingService != null)
            {
                drainningSettingService.scale = Convert.ToDouble(firstFloorPlane.BlockScale.Tag);
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
        private List<Polyline> CutWallByUserOutFrame(List<Polyline> userOutFrame, List<Polyline> walls, List<Polyline> roomWalls)
        {
            var wallCurves = new List<Polyline>(walls);
            wallCurves.AddRange(roomWalls);
            var allWalls = new List<Entity>();
            foreach (var curve in wallCurves)
            {
                var interOutFrames = userOutFrame.Where(x => x.IsIntersects(curve)).ToList();
                if (interOutFrames.Count > 0)
                {
                    var bufferFrameDic = interOutFrames.ToDictionary(x => x, y =>
                    {
                        var tempPoly = y.ExtendByLengthLine(100);
                        //tempPoly = tempPoly.ExtendByLengthLine(-100, false);
                        return tempPoly;
                    });
                    allWalls.AddRange(curve.Difference(bufferFrameDic.Values.ToCollection()).OfType<Entity>().ToList());
                }
                else
                {
                    allWalls.Add(curve);
                }
            }
            var dbObjs = allWalls.ToCollection().UnionPolygons(false).Cast<Entity>().ToList();
            return StructGeoService.GetWallPolylines(dbObjs);
        }

        /// <summary>
        /// 回退transform，打印图纸到世界坐标系
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="originTransformer"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> FilterNeedRoutePipes(List<VerticalPipeModel> pipes)
        {
            var verPipes = new List<VerticalPipeModel>();
            foreach (var pipe in pipes)
            {
                if (pipe.PipeType == VerticalPipeType.ConfluencePipe || pipe.PipeType == VerticalPipeType.SewagePipe || pipe.PipeType == VerticalPipeType.WasteWaterPipe)
                {
                    if (paramSetting.WasteWaterChecked.Value)
                    {
                        verPipes.Add(pipe);
                    }
                    else
                    {
                        pipe.PipeType = VerticalPipeType.holePipe;
                        verPipes.Add(pipe);
                    }
                }
                else if (pipe.PipeType == VerticalPipeType.CondensatePipe)
                {
                    if (paramSetting.CondensateChecked.Value)
                    {
                        verPipes.Add(pipe);
                    }
                    else
                    {
                        pipe.PipeType = VerticalPipeType.holePipe;
                        verPipes.Add(pipe);
                    }
                }
                else if (pipe.PipeType == VerticalPipeType.rainPipe)
                {
                    if (paramSetting.RainWaterChecked.Value)
                    {
                        verPipes.Add(pipe);
                    }
                    else
                    {
                        pipe.PipeType = VerticalPipeType.holePipe;
                        verPipes.Add(pipe);
                    }
                }
                else
                {
                    pipe.PipeType = VerticalPipeType.holePipe;
                    verPipes.Add(pipe);
                }
            }
            return verPipes;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}