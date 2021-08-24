﻿using AcHelper;
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
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThSecurityPlaneSystemPipeCommand : IAcadCommand, IDisposable
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
                };
                var trunkingFilter = ThSelectionFilterTool.Build(trunkingDxfNames);
                var trunkingResult = Active.Editor.GetSelection(trunkingOptions, trunkingFilter);
                if (trunkingResult.Status != PromptStatus.OK)
                {
                    return;
                }

                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as BlockReference);
                }
                string trunkingLayer = acadDatabase.Element<Curve>(trunkingResult.Value.GetObjectIds().First()).Layer;
                foreach (var frameBlock in frameLst)
                {
                    var frame = CommonService.GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame == null)
                    {
                        continue;
                    }

                    var pt = frame.StartPoint;
                    ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                    //originTransformer.Transform(frame);
                    var outFrame = ThMEPFrameService.Normalize(frame);

                    GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                    //获取构建信息
                    var rooms = new List<ThIfcRoom>();
                    using (var ov = new ThCADCoreNTSArcTessellationLength(3000))
                    {
                        rooms = getPrimitivesService.GetRoomInfo(outFrame);
                    }
                    var doors = getPrimitivesService.GetDoorInfo(outFrame);
                    getPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);
                    var floor = getPrimitivesService.GetFloorInfo(outFrame);

                    //获取连线图块
                    var blocks = GetBlocks(frame);

                    //获取线槽
                    var trunkings = getPrimitivesService.GetTrunkings(frame, trunkingLayer);

                    //连线
                    ConnectPipeService connectPipeService = new ConnectPipeService();
                    connectPipeService.ConnectPipe(frame, blocks, rooms, doors, columns, trunkings, new List<Polyline>(), floor);
                }
            }
        }

        /// <summary>
        /// 查找框线内所有布置图块
        /// </summary>
        /// <param name="polyline"></param>
        public List<BlockReference> GetBlocks(Polyline polyline)
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
                    return polyline.Contains(position);
                })
                .Cast<BlockReference>()
                .ForEachDbObject(o => resBlocks.Add(o));

                return resBlocks;
            }
        }
    }
}
