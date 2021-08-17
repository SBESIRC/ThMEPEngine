using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem;
using ThMEPElectrical.SecurityPlaneSystem.GuardTourSystem.LayoutService;
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThGuardToourSystemCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { ThMEPCommon.FRAME_LAYER_NAME });
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as BlockReference);
                }

                List<Polyline> frames = new List<Polyline>();
                foreach (var frameBlock in frameLst)
                {
                    var frame = CommonService.GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                var pt = frames.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frames = frames.Select(x =>
                {
                    //originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x);
                }).ToList();
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                foreach (var outFrame in frames)
                {
                    //获取构建信息
                    var rooms = new List<ThIfcRoom>();
                    using (var ov = new ThCADCoreNTSArcTessellationLength(3000))
                    {
                        rooms = getPrimitivesService.GetRoomInfo(outFrame);
                    }
                    var doors = getPrimitivesService.GetDoorInfo(outFrame);
                    getPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);

                    //获取车道线
                    var lanes = getPrimitivesService.GetLanes(outFrame, out List<List<Line>> otherLanes);
                    lanes.AddRange(otherLanes);

                    //获取楼层信息
                    var floor = getPrimitivesService.GetFloorInfo(outFrame);

                    //布置
                    LayoutGuardTourService layoutService = new LayoutGuardTourService();
                    var layoutInfo = layoutService.Layout(rooms, doors, columns, walls, lanes, floor);

                    //插入图块
                    InsertBlock(layoutInfo, originTransformer);

                    //using (AcadDatabase db = AcadDatabase.Active())
                    //{
                    //    foreach (var item in layoutInfo)
                    //    {
                    //        Line line = new Line(item.Item1, item.Item1 + 1000 * item.Item2);
                    //        //originTransformer.Reset(line);
                    //        db.ModelSpace.Add(line);
                    //    }
                    //}
                }
            }
        }

        /// <summary>
        /// 插入图块
        /// </summary>
        /// <param name="layoutModels"></param>
        /// <param name="originTransformer"></param>
        private void InsertBlock(List<(Point3d, Vector3d)> layoutModels, ThMEPOriginTransformer originTransformer)
        {
            foreach (var model in layoutModels)
            {
                var pt = model.Item1;
                //originTransformer.Reset(ref pt);

                var dir = model.Item2;
                if (dir.Y < 0)
                {
                    dir = new Vector3d(dir.X, -dir.Y, 0);
                }
                double rotateAngle = Vector3d.YAxis.GetAngleTo(dir, Vector3d.ZAxis);
                InsertBlockService.InsertBlock(ThMEPCommon.GT_LAYER_NAME, ThMEPCommon.TIMERECORDER_BLOCK_NAME, pt, rotateAngle, ThElectricalUIService.Instance.Parameter.scale);
            }
        }
    }
}
