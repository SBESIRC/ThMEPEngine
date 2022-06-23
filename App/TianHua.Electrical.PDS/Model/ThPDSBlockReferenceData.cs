using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

using ThCADExtension;
using TianHua.Electrical.PDS.Project.Module;

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
        public ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }
        
        /// <summary>
        /// 是否消防
        /// </summary>
        public ThPDSFireLoad FireLoad{ get; set; }

        /// <summary>
        /// 默认负载描述
        /// </summary>
        public string DefaultDescription { get; set; }

        /// <summary>
        /// Cable laying method 1
        /// </summary>
        public LayingSite CableLayingMethod1 { get; set; }

        /// <summary>
        /// Cable laying method 2
        /// </summary>
        public LayingSite CableLayingMethod2 { get; set; }

        public ThPDSBlockReferenceData(ObjectId blockRef)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            Attributes = blockRef.GetAttributesInBlockReference();
            Cat_1 =  ThPDSLoadTypeCat_1.LumpedLoad;
            Cat_2 = ThPDSLoadTypeCat_2.None;
            Phase = ThPDSPhase.三相;
            DemandFactor = 1.0;
            PowerFactor = 0.85;
            FireLoad = ThPDSFireLoad.Unknown;
            DefaultDescription = "";
            CableLayingMethod1 = LayingSite.CC;
            CableLayingMethod2 = LayingSite.None;
        }

        public ThPDSBlockReferenceData(ObjectId blockRef, Matrix3d transfrom)
        {
            ObjId = blockRef;
            Database = blockRef.Database;
            Position = blockRef.GetBlockPosition();
            ScaleFactors = blockRef.GetScaleFactors();
            BlockLayer = blockRef.GetBlockLayer();
            EffectiveName = blockRef.GetBlockName();
            Attributes = blockRef.GetAttributesInBlockReference();
            Cat_1 = ThPDSLoadTypeCat_1.LumpedLoad;
            Cat_2 = ThPDSLoadTypeCat_2.None;
            Phase = ThPDSPhase.三相;
            DemandFactor = 1.0;
            PowerFactor = 0.85;
            OwnerSpace2WCS = transfrom;
            FireLoad = ThPDSFireLoad.Unknown;
            DefaultDescription = "";
            CableLayingMethod1 = LayingSite.CC;
            CableLayingMethod2 = LayingSite.None;
        }
    }
}
