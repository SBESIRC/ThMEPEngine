using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using AcHelper;
using System.Collections.Generic;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.UCSDivisionService;

namespace ThMEPElectrical
{
    public class ThGroundGridCmds
    {
        [CommandMethod("TIANHUACAD", "THGGD", CommandFlags.Modal)]
        public void THGGD()
        {
            using (var cmd = new ThGroundGridCommand())
            {
                cmd.Execute();
            }
        }

        /// <summary>
        /// ucs分区
        /// </summary>
        [CommandMethod("TIANHUACAD", "THUCSDIV", CommandFlags.Modal)]
        public void ThUcsDisivision()
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
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                foreach (var frame in frameLst)
                {
                    GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                    getPrimitivesService.GetStructureInfo(frame, out List<Polyline> columns, out List<Polyline> walls);

                    //区域分割
                    UCSService uCSService = new UCSService();
                    var ucsInfo = uCSService.UcsDivision(columns, frame);
                    foreach (var item in ucsInfo)
                    {
                        acadDatabase.ModelSpace.Add(item.Key);
                    }
                }
            }
        }
    }
}
