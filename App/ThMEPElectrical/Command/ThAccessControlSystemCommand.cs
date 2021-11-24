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
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThAccessControlSystemCommand : ThMEPBaseCommand, IDisposable
    {
        public ThAccessControlSystemCommand()
        {
            this.ActionName="安防平面-出入口控制系统布置";
            this.CommandName="THACSYSTEM";
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

                    //布置
                    LayoutAccessControlService layoutService = new LayoutAccessControlService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, floor);

                    //插入图块
                    InsertBlock(layoutInfo, originTransformer);
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
            double scale = ThElectricalUIService.Instance.Parameter.scale;
            foreach (var model in layoutModels)
            {
                var pt = model.layoutPt;
                originTransformer.Reset(ref pt);

                if (model.layoutDir.Y < 0)
                {
                    //model.layoutDir = new Vector3d(model.layoutDir.X, -model.layoutDir.Y, 0);
                    model.layoutDir = -model.layoutDir;
                }
                double rotateAngle = Vector3d.XAxis.GetAngleTo(model.layoutDir, Vector3d.ZAxis);
                if (model is Buttun)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.BUTTON_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "E" } });
                }
                else if (model is CardReader)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.CARDREADER_BLOCK_NAME, pt, rotateAngle, scale);
                }
                else if (model is ElectricLock)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.ELECTRICLOCK_BLOCK_NAME, pt, rotateAngle, scale, new Dictionary<string, string>() { { "F", "EL" } });
                }
                else if (model is Intercom)
                {
                    InsertBlockService.InsertBlock(ThMEPCommon.AC_LAYER_NAME, ThMEPCommon.INTERCOM_BLOCK_NAME, pt, rotateAngle, scale);
                }
            }
        }
    }
}
