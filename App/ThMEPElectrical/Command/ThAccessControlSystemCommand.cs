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
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThAccessControlSystemCommand : IAcadCommand, IDisposable
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
                    using (AcadDatabase db = AcadDatabase.Active())
                    {
                        //foreach (var item in walls)
                        //{
                        //    db.ModelSpace.Add(item);
                        //}
                        //foreach (var item in columns)
                        //{
                        //    db.ModelSpace.Add(item);
                        //}
                    }
                    var floor = getPrimitivesService.GetFloorInfo(outFrame);

                    //布置
                    LayoutAccessControlService layoutService = new LayoutAccessControlService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, floor);

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
        private void InsertBlock(List<AccessControlModel> layoutModels, ThMEPOriginTransformer originTransformer)
        {
            foreach (var model in layoutModels)
            {
                var pt = model.layoutPt;
                //originTransformer.Reset(ref pt);

                if (model.layoutDir.Y < 0)
                {
                    //model.layoutDir = new Vector3d(model.layoutDir.X, -model.layoutDir.Y, 0);
                    model.layoutDir = -model.layoutDir;
                }
                double rotateAngle = Vector3d.XAxis.GetAngleTo(model.layoutDir, Vector3d.ZAxis);
                if (model is Buttun)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.BUTTON_BLOCK_NAME, pt, rotateAngle, 100, new Dictionary<string, string>() { { "F", "E" } });
                }
                else if (model is CardReader)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.CARDREADER_BLOCK_NAME, pt, rotateAngle, 100);
                }
                else if (model is ElectricLock)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.ELECTRICLOCK_BLOCK_NAME, pt, rotateAngle, 100, new Dictionary<string, string>() { { "F", "EL" } });
                }
                else if (model is Intercom)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.INTERCOM_BLOCK_NAME, pt, rotateAngle, 100);
                }
            }
        }
    }
}
