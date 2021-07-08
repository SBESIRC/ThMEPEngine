using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Command
{
    // https://spiderinnet1.typepad.com/blog/2012/02/autocad-net-entityjig-jig-attributes-of-block-insertblockreference.html
    // https://spiderinnet1.typepad.com/blog/2012/02/autocad-net-entityjig-honor-ucs-when-inserting-block-insertblockreference.html
    public class ThStoreyBlockJig : EntityJig
    {
        private const double DblTol = 0.0001;

        private Point3d mPosition;

        private Dictionary<AttributeReference, AttributeDefinition> mRef2DefMap;

        public ThStoreyBlockJig(BlockReference br, Dictionary<AttributeReference, AttributeDefinition> dict) 
            : base(br)
        {
            mRef2DefMap = dict;
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
            if (prResult.Value.IsEqualTo(mPosition, new Tolerance(DblTol, DblTol)))
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

            foreach (var ar2ad in mRef2DefMap)
            {
                ar2ad.Key.SetAttributeFromBlock(ar2ad.Value, br.BlockTransform);
            }

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
                currentDb.ModelSpace.Add(br);

                var dict = new Dictionary<AttributeReference, AttributeDefinition>();
                if (block.Item.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in block.Item)
                    {
                        DBObject obj = currentDb.Element<DBObject>(id);
                        if (obj is AttributeDefinition)
                        {
                            AttributeDefinition ad = obj as AttributeDefinition;
                            AttributeReference ar = new AttributeReference();
                            ar.SetAttributeFromBlock(ad, br.BlockTransform);

                            br.AttributeCollection.AppendAttribute(ar);
                            currentDb.AddNewlyCreatedDBObject(ar);

                            dict.Add(ar, ad);
                        }
                    }
                }

                var jig = new ThStoreyBlockJig(br, dict);
                var pr = Active.Editor.Drag(jig);
                if (pr.Status != PromptStatus.OK)
                {
                    currentDb.DiscardChanges();
                }
            }
        }

        private string BlockDwgPath()
        {
            return ThCADCommon.StoreyFrameDwgPath();
        }
    }
}
