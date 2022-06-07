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
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThGuardToourSystemCommand : ThMEPBaseCommand, IDisposable
    {
        public ThGuardToourSystemCommand()
        {
            this.ActionName="安防平面-电子巡更系统布置";
            this.CommandName="THGTSYSTEM";
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public override void SubExecute()
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
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Dictionary<Polyline, ObjectIdCollection> frameLst = new Dictionary<Polyline, ObjectIdCollection>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    var boundary = ThElectricalCommonService.GetFrameBlkPolyline(frame);
                    frameLst.Add(boundary, new ObjectIdCollection() { obj });
                }

                var pt = frameLst.First().Key.StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                foreach (var frameBlockDic in frameLst)
                {
                    var outFrame = ThMEPFrameService.Normalize(frameBlockDic.Key);
                    var frameBlockId = frameBlockDic.Value;
                    originTransformer.Transform(outFrame);

                    //获取楼层信息
                    var floor = getPrimitivesService.GetFloorInfo(frameBlockId);
                    if (floor.IsNull())
                    {
                        continue;
                    }

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
                    var GTEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.GT_BLOCK_NAMES, ThMEPCommon.GT_PIPE_LAYER_NAME);

                    //布置
                    LayoutGuardTourService layoutService = new LayoutGuardTourService();
                    var layoutInfo = layoutService.Layout(rooms, doors, columns, walls, lanes, floor);

                    //删除旧图块
                    DeleteBlock(GTEntitys);

                    //插入图块
                    InsertBlock(layoutInfo, originTransformer);
                }
            }
        }

        private void DeleteBlock(List<Entity> gTEntitys)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                gTEntitys.ForEach(vmEntity =>
                {
                    vmEntity.UpgradeOpen();
                    vmEntity.Erase();
                });
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
                originTransformer.Reset(ref pt);

                var dir = model.Item2;
                if (dir.Y < 0)
                {
                    dir = dir.Negate();
                }
                double rotateAngle = Vector3d.YAxis.GetAngleTo(dir, Vector3d.ZAxis);
                InsertBlockService.InsertBlock(ThMEPCommon.GT_LAYER_NAME, ThMEPCommon.TIMERECORDER_BLOCK_NAME, pt, rotateAngle, ThElectricalUIService.Instance.Parameter.scale);
            }
        }
    }
}
