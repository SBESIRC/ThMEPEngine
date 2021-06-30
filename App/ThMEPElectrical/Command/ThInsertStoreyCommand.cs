using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Command
{
    public class ThStoreyBlockJig : EntityJig
    {
        Point3d mCenterPt, mActualPoint;

        public ThStoreyBlockJig(BlockReference br) : base(br)
        {
            mCenterPt = br.Position;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jigOpts = new JigPromptPointOptions()
            {
                Message = "\n请指定插入点",
                UserInputControls =
                UserInputControls.Accept3dCoordinates |
                UserInputControls.NoZeroResponseAccepted |
                UserInputControls.NoNegativeResponseAccepted,
            };
            PromptPointResult dres = prompts.AcquirePoint(jigOpts);
            if (mActualPoint == dres.Value)
            {
                return SamplerStatus.NoChange;
            }
            else
            {
                mActualPoint = dres.Value;
            }

            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            mCenterPt = mActualPoint;
            try
            {
                ((BlockReference)Entity).Position = mCenterPt;
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }

        public Entity GetEntity()
        {
            return Entity;
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
                    var blkref = jig.GetEntity();
                    currentDb.ModelSpace.Add(blkref);
                    blkref.SetDatabaseDefaults();
                }
            }
        }

        private string BlockDwgPath()
        {
            return ThCADCommon.StoreyFrameDwgPath();
        }
    }
}
