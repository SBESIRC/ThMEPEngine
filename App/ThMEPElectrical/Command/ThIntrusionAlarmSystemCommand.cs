using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.SecurityPlaneSystem.IntrusionAlarmSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem;
using ThMEPElectrical.Service;
using ThMEPEngineCore.Command;

namespace ThMEPElectrical.Command
{
    public class ThIntrusionAlarmSystemCommand : ThMEPBaseCommand, IDisposable
    {
        public ThIntrusionAlarmSystemCommand()
        {
            this.ActionName="安防平面-入侵报警系统布置";
            this.CommandName="THIASYSTEM";
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
                    var outFrame = frameBlockDic.Key;
                    var frameBlockId = frameBlockDic.Value;
                    originTransformer.Transform(outFrame);
                    outFrame = ThMEPFrameService.Normalize(outFrame);

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
                    var IAEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.IA_BLOCK_NAMES, ThMEPCommon.IA_PIPE_LAYER_NAME);

                    //布置
                    LayoutFactoryService layoutService = new LayoutFactoryService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, floor);

                    //删除旧图块
                    DeleteBlock(IAEntitys);

                    //插入图块
                    InsertBlock(layoutInfo, originTransformer);

                    //using (AcadDatabase db = AcadDatabase.Active())
                    //{
                    //    foreach (var item in layoutInfo)
                    //    {
                    //        var endPt = item.LayoutPoint + 500 * item.LayoutDir;
                    //        Line line = new Line(item.LayoutPoint, endPt);
                    //        Circle circle = new Circle(endPt, Vector3d.ZAxis, 100);
                    //        //originTransformer.Reset(line);
                    //        db.ModelSpace.Add(line);
                    //        db.ModelSpace.Add(circle);
                    //    }
                    //}
                }
            }
        }

        private void DeleteBlock(List<Entity> vmEntitys)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                vmEntitys.ForEach(vmEntity =>
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
        private void InsertBlock(List<LayoutModel> layoutModels, ThMEPOriginTransformer originTransformer)
        {
            double scale = ThElectricalUIService.Instance.Parameter.scale;
            foreach (var model in layoutModels)
            {
                var pt = model.LayoutPoint;
                originTransformer.Reset(ref pt);

                double rotateAngle = (-Vector3d.XAxis).GetAngleTo(model.LayoutDir, Vector3d.ZAxis);
                if (model is ControllerModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.CONTROLLER_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is InfraredWallDetectorModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.INFRAREDWALLDETECTOR_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "IR" } });
                }
                else if (model is DoubleWallDetectorModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.DOUBLEDETECTOR_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "IR/M" } });
                }
                else if (model is InfraredHositingDetectorModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.INFRAREDHOSITINGDETECTOR_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "IR" } });
                }
                else if (model is DoubleHositingDetectorModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.DOUBLEDETECTOR_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "IR/M" } });
                }
                else if (model is SoundLightAlarm)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.SOUNDLIGHTALARM_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is DisabledAlarmButtun)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.DISABLEDALARM_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is EmergencyAlarmButton)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.IA_LAYER_NAME, ThMEPCommon.EMERGENCYALARM_BLOCK_NAME, pt, rotateAngle, scale);
                }
            }
        }
    }
}
