using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService;
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
                    var floor = getPrimitivesService.GetFloorInfo(outFrame);

                    //布置
                    LayoutAccessControlService layoutService = new LayoutAccessControlService();
                    var layoutInfo = layoutService.LayoutFactory(rooms, doors, columns, walls, floor);

                    using (AcadDatabase db = AcadDatabase.Active())
                    {
                        foreach (var item in layoutInfo)
                        {
                            Line line = new Line(item.layoutPt, item.layoutPt + 1000 * item.layoutDir);
                            //originTransformer.Reset(line);
                            db.ModelSpace.Add(line);
                        }
                    }
                }
            }
        }
    }
}
