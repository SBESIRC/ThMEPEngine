using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

using ThCADExtension;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSBlockReferenceData
    {
        public Point3d Position { get; set; }
        public Scale3d ScaleFactors { get; set; }
        public string BlockLayer { get; set; }
        public string EffectiveName { get; set; }
        public Matrix3d OwnerSpace2WCS { get; set; }
        public Database Database { get; set; }
        public ObjectId ObjId { get; set; }
        public SortedDictionary<string, string> Attributes { get; set; }
        public DynamicBlockReferencePropertyCollection CustomProperties { get; set; }

        /// <summary>
        /// 一级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_1 Cat_1 { get; set; } = ThPDSLoadTypeCat_1.None;

        /// <summary>
        /// 二级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_2 Cat_2 { get; set; } = ThPDSLoadTypeCat_2.None;

        /// <summary>
        /// 默认回路类型
        /// </summary>
        public ThPDSCircuitType DefaultCircuitType { get; set; } = ThPDSCircuitType.None;

        public ThPDSBlockReferenceData(ObjectId blockRef)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            Attributes = blockRef.GetAttributesInBlockReference();
        }

        public ThPDSBlockReferenceData(ObjectId blockRef, Matrix3d transfrom)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            CustomProperties = blockRef.GetDynProperties();
            Attributes = blockRef.GetAttributesInBlockReference();
            OwnerSpace2WCS = transfrom;
        }
    }
}
