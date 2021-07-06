using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public class ThBlockReferenceData
    {
        public double Rotation { get; set; }
        public Point3d Position { get; set; }
        public Extents3d Extents { get; set; }
        public string BlockLayer { get; set; }
        public string EffectiveName { get; set; }
        public Database Database { get; set; }
        public Matrix3d MCS2WCS { get; set; }
        public Matrix3d BlockTransform { get; set; }
        public SortedDictionary<string, string> Attributes { get; set; }
        public DynamicBlockReferencePropertyCollection CustomProperties { get; set; }
        public ThBlockReferenceData(ObjectId blockRef)
        {
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            Rotation = blockRef.GetBlockRotation();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            BlockTransform = blockRef.GetBlockTransform();
            Attributes = blockRef.GetAttributesInBlockReference();
        }
        public ThBlockReferenceData(ObjectId blockRef, Matrix3d transfrom)
        {
            MCS2WCS = transfrom;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            Rotation = blockRef.GetBlockRotation();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            BlockTransform = blockRef.GetBlockTransform();
            Attributes = blockRef.GetAttributesInBlockReference();
        }
    }
}