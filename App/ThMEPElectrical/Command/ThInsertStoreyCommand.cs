using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using GeometryExtensions;

namespace ThMEPElectrical.Command
{

    // https://spiderinnet1.typepad.com/blog/2012/02/autocad-net-entityjig-honor-ucs-when-inserting-block-insertblockreference.html
    public class ThStoreyBlockJig : EntityJig
    {
        private Point3d mPosition;

        public ThStoreyBlockJig(BlockReference br) : base(br)
        {
            br.TransformBy(Active.Editor.UCS2WCS());
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions opts = new JigPromptPointOptions()
            {
                Message = "\n请指定插入点",
                UserInputControls =
                UserInputControls.Accept3dCoordinates |
                UserInputControls.GovernedByUCSDetect |
                UserInputControls.UseBasePointElevation,
            };
            PromptPointResult prResult = prompts.AcquirePoint(opts);
            if (prResult.Status == PromptStatus.Cancel)
            {
                return SamplerStatus.Cancel;
            }
            if (prResult.Value.Equals(mPosition))
            {
                return SamplerStatus.NoChange;
            }
            else
            {
                mPosition = prResult.Value;
                return SamplerStatus.OK;
            }
        }

        protected override bool Update()
        {
            var br = (BlockReference)Entity;
            br.Position = mPosition;
            return true;
        }
    }

    public class ThInsertStoreyCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                var block = currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-楼层框定E"), false);
                var br = new BlockReference(Point3d.Origin, block.Item.ObjectId);
                var jig = new ThStoreyBlockJig(br);
                var pr = Active.Editor.Drag(jig);
                if (pr.Status == PromptStatus.OK)
                {
                    currentDb.ModelSpace.Add(br);
                    br.SetDatabaseDefaults();
                }
            }
        }

        private string BlockDwgPath()
        {
            return ThCADCommon.StoreyFrameDwgPath();
        }
    }
}
