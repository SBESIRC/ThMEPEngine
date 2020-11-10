using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFSBlockReference
    {
        public double Rotation { get; set; }
        public Point3d Position { get; set; }
        public List<string> Texts { get; set; }
        public string EffectiveName { get; set; }
        public Database HostDatabase { get; set; }
        public Matrix3d BlockTransform { get; set; }
        public SortedDictionary<string, string> Attributes { get; set; }
        public DynamicBlockReferencePropertyCollection CustomProperties { get; set; }
        public ThFSBlockReference(ObjectId blockRef)
        {
            HostDatabase = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            Rotation = blockRef.GetBlockRotation();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            BlockTransform = blockRef.GetBlockTransform();
            Attributes = blockRef.GetAttributesInBlockReference();
        }
    }
}
