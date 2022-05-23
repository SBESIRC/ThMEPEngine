using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.ConnectPipe;
using ThMEPElectrical.Service;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThSecurityPlaneSystemPipeCommand : ThMEPBaseCommand, IDisposable
    {
        public ThSecurityPlaneSystemPipeCommand()
        {
            this.ActionName="安防平面-连线";
            this.CommandName="THSPPIPE";
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

                // 获取线槽图层
                PromptSelectionOptions trunkingOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择线槽类型",
                    RejectObjectsOnLockedLayers = true,
                    SinglePickInSpace = true,
                };
                var trunkingDxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                    "TCH_CABLETRY",//天正桥架
                };
                var trunkingFilter = ThSelectionFilterTool.Build(trunkingDxfNames);
                var trunkingResult = Active.Editor.GetSelection(trunkingOptions, trunkingFilter);
                if (trunkingResult.Status != PromptStatus.OK)
                {
                    return;
                }
                string trunkingLayer = acadDatabase.Element<Curve>(trunkingResult.Value.GetObjectIds().First()).Layer;

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
                    

                    //获取连线图块
                    var blocks = GetBlocks(outFrame, originTransformer);

                    //获取线槽
                    var trunkings = getPrimitivesService.GetTrunkings(outFrame, trunkingLayer);

                    var ACEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.AC_BLOCK_NAMES, ThMEPCommon.AC_PIPE_LAYER_NAME, true);
                    var GTEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.GT_BLOCK_NAMES, ThMEPCommon.GT_PIPE_LAYER_NAME, true);
                    var IAEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.IA_BLOCK_NAMES, ThMEPCommon.IA_PIPE_LAYER_NAME, true);
                    var vmEntitys = getPrimitivesService.GetOldLayout(outFrame, ThMEPCommon.VM_BLOCK_NAMES, ThMEPCommon.VM_PIPE_LAYER_NAME, true);

                    //删除旧图块
                    DeleteBlock(ACEntitys);
                    DeleteBlock(GTEntitys);
                    DeleteBlock(IAEntitys);
                    DeleteBlock(vmEntitys);

                    //连线
                    ConnectPipeService connectPipeService = new ConnectPipeService();
                    var Lines=connectPipeService.ConnectPipe(outFrame, blocks, rooms, doors, columns, trunkings, new List<Polyline>(), floor);

                    using (AcadDatabase db = AcadDatabase.Active())
                    {
                        foreach (var polys in Lines)
                        {
                            originTransformer.Reset(polys);
                            db.ModelSpace.Add(polys);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查找框线内所有布置图块
        /// </summary>
        /// <param name="polyline"></param>
        public List<BlockReference> GetBlocks(Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //获取喷淋
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var layerNames = new string[]
                {
                    ThMEPCommon.VM_LAYER_NAME,
                    ThMEPCommon.AC_LAYER_NAME,
                    ThMEPCommon.IA_LAYER_NAME,
                    ThMEPCommon.GT_LAYER_NAME,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == string.Join(",", layerNames) &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var blocks = new List<Entity>();
                var status = Active.Editor.SelectAll(filterlist);
                if (status.Status == PromptStatus.OK)
                {
                    foreach (ObjectId obj in status.Value.GetObjectIds())
                    {
                        blocks.Add(acadDatabase.Element<Entity>(obj));
                    }
                }
                var resBlocks = new List<BlockReference>();
                blocks.Where(o => {
                    var pts = o.GeometricExtents;
                    var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                    position = originTransformer.Transform(position);
                    return polyline.Contains(position);
                })
                .Cast<BlockReference>()
                .ForEachDbObject(o =>
                {
                    var blk = o.Clone() as BlockReference;
                    originTransformer.Transform(blk);
                    resBlocks.Add(blk);
                });

                return resBlocks;
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
    }
}
