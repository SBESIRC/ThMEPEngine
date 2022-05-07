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
        public Scale3d ScaleFactors { get; set; }
        public string BlockLayer { get; set; }
        public string EffectiveName { get; set; }
        public Database Database { get; set; }
        public ObjectId ObjId { get; set; }
        public Vector3d Normal { get; set; }
        public Matrix3d OwnerSpace2WCS { get; set; }
        public Matrix3d BlockTransform { get; set; }
        public SortedDictionary<string, string> Attributes { get; set; }
        public DynamicBlockReferencePropertyCollection CustomProperties { get; set; }
        public ThBlockReferenceData(ObjectId blockRef)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            Rotation = blockRef.GetBlockRotation();
            Normal = blockRef.GetBlockNormal();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            try
            {
                CustomProperties = blockRef.GetDynProperties();
            }
            catch
            {
            }
            BlockTransform = blockRef.GetBlockTransform();
            Attributes = blockRef.GetAttributesInBlockReference();
            OwnerSpace2WCS = Matrix3d.Identity;
        }
        public ThBlockReferenceData(ObjectId blockRef, Matrix3d transfrom)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            Rotation = blockRef.GetBlockRotation();
            Normal = blockRef.GetBlockNormal();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            BlockTransform = blockRef.GetBlockTransform();
            Attributes = blockRef.GetAttributesInBlockReference();
            OwnerSpace2WCS = transfrom;
        }
    }
}