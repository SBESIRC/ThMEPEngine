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
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPElectrical.VideoMonitoringSystem;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThVideoMonitoringSystemCommand : ThMEPBaseCommand, IDisposable
    {
        public ThVideoMonitoringSystemCommand()
        {
            this.ActionName="安防平面-视频监控系统布置";
            this.CommandName="THVMSYSTEM";
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
                    var blk = frame.Clone() as BlockReference;
                    var boundary = CommonService.GetBlockInfo(blk).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    ObjectIdCollection dBObject = new ObjectIdCollection();
                    dBObject.Add(obj);
                    frameLst.Add(boundary, dBObject);
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
                    if(floor.IsNull())
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

                    var vmEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.VM_BLOCK_NAMES, ThMEPCommon.VM_PIPE_LAYER_NAME);

                    //布置
                    LayoutService layoutService = new LayoutService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, lanes.SelectMany(x => x).ToList(), floor);

                    //删除旧图块
                    DeleteBlock(vmEntitys);

                    //插入图块
                    InsertBlock(layoutInfo, originTransformer);

                    //using (AcadDatabase db = AcadDatabase.Active())
                    //{
                    //    foreach (var item in layoutInfo)
                    //    {
                    //        var endPt = item.layoutPt + 500 * item.layoutDir;
                    //        Line line = new Line(item.layoutPt, endPt);
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
            using(AcadDatabase acad = AcadDatabase.Active())
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
                var pt = model.layoutPt;
                originTransformer.Reset(ref pt);

                double rotateAngle = (-Vector3d.XAxis).GetAngleTo(model.layoutDir, Vector3d.ZAxis);
                if (model is GunCameraModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is PanTiltCameraModel)
                {
                    var block = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.PANTILTCAMERA_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is DomeCameraModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, scale);
                    InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.DOMECAMERA_SHILED_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is GunCameraWithShieldModel)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, scale);
                    InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_SHIELD_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is FaceRecognitionCameraModel)
                {
                    var attribute = new Dictionary<string, string>() { { "人脸", "人脸" } };
                    var faceBlockId = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.FACERECOGNITIONCAMERA_BLOCK_NAME, pt, rotateAngle, scale, attribute);
                    TransFaceBlockText(faceBlockId, rotateAngle);
                }
            }
        }

        private void TransFaceBlockText(ObjectId blockId, double angle)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var block = db.Element<BlockReference>(blockId);
                foreach (ObjectId id in block.AttributeCollection)
                {
                    var attributeBlock = db.Element<AttributeReference>(id, true);
                    if (angle > (Math.PI / 2) && angle < (Math.PI * 3 / 2))
                    {
                        attributeBlock.Rotation = angle + Math.PI;
                    }
                }
            }
        }
    }
}
