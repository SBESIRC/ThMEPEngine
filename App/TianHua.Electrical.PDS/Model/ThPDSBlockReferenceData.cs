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
        public ThPDSLoadTypeCat_1 Cat_1 { get; set; }

        /// <summary>
        /// 二级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_2 Cat_2 { get; set; }

        /// <summary>
        /// 默认回路类型
        /// </summary>
        public ThPDSCircuitType DefaultCircuitType { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public int Phase { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }

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
            Cat_1 =  ThPDSLoadTypeCat_1.LumpedLoad;
            Cat_2 = ThPDSLoadTypeCat_2.None;
            Phase = 1;
            DemandFactor = 1.0;
            PowerFactor = 1.0;
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
