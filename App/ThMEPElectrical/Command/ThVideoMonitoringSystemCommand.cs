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
using ThMEPElectrical.StructureHandleService;
using ThMEPElectrical.VideoMonitoringSystem;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThVideoMonitoringSystemCommand : IAcadCommand, IDisposable
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
                var filter = ThSelectionFilterTool.Build(dxfNames);
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
                    LayoutService layoutService = new LayoutService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, lanes.SelectMany(x => x).ToList(), floor);

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

        /// <summary>
        /// 插入图块
        /// </summary>
        /// <param name="layoutModels"></param>
        /// <param name="originTransformer"></param>
        private void InsertBlock(List<LayoutModel> layoutModels, ThMEPOriginTransformer originTransformer)
        {
            foreach (var model in layoutModels)
            {
                var pt = model.layoutPt;
                var layoutDir = -model.layoutDir;
                //originTransformer.Reset(ref pt);

                double rotateAngle = (-Vector3d.XAxis).GetAngleTo(layoutDir, Vector3d.ZAxis);
                if (model is GunCameraModel)
                {
                    var block = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, 100);
                    SetBlockTrans(block);
                }
                else if (model is PanTiltCameraModel)
                {
                    var block = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.PANTILTCAMERA_BLOCK_NAME, pt, rotateAngle, 100);
                    SetBlockTrans(block);
                }
                else if (model is DomeCameraModel)
                {
                    var cameraBlock = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, 100);
                    var shiledBlock = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.DOMECAMERA_SHILED_BLOCK_NAME, pt, rotateAngle, 100);
                    SetBlockTrans(cameraBlock);
                    SetBlockTrans(shiledBlock);
                }
                else if (model is GunCameraWithShieldModel)
                {
                    var cameraBlock = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_BLOCK_NAME, pt, rotateAngle, 100);
                    var shiledBlock = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.GUNCAMERA_SHIELD_BLOCK_NAME, pt, rotateAngle, 100);
                    SetBlockTrans(cameraBlock);
                    SetBlockTrans(shiledBlock);
                }
                else if (model is FaceRecognitionCameraModel)
                {
                    var block = InsertBlockService.InsertBlock(ThMEPCommon.VM_LAYER_NAME, ThMEPCommon.FACERECOGNITIONCAMERA_BLOCK_NAME, pt, rotateAngle, 100);
                    SetBlockTrans(block);
                }
            }
        }

        /// <summary>
        /// 翻转块，让其正锅来
        /// </summary>
        private void SetBlockTrans(BlockReference block)
        {
            var coord = block.BlockTransform.CoordinateSystem3d;
            Matrix3d matrix = new Matrix3d(new double[] {
                -coord.Xaxis.X, -coord.Xaxis.Y, -coord.Xaxis.Z, coord.Origin.X,
                coord.Yaxis.X, coord.Yaxis.Y, coord.Yaxis.Z, coord.Origin.Y,
                coord.Zaxis.X, coord.Zaxis.Y, coord.Zaxis.Z, coord.Origin.Z,
                0.0, 0.0, 0.0, 1.0
            });

            block.BlockTransform = matrix;
        }
    }
}
