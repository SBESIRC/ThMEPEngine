using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels.ThSmokeProofMappingModel;

namespace ThMEPHVAC
{
    public class ThMEPHVACStaticService
    {

        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPHVACStaticService instance = new ThMEPHVACStaticService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPHVACStaticService() { }
        internal ThMEPHVACStaticService() { }
        public static ThMEPHVACStaticService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// 正压送风参数
        /// </summary>
        public SmokeCalculateViewModel smokeCalculateViewModel { get; set; }

        /// <summary>
        /// 消防电梯前室
        /// </summary>
        public FireElevatorFrontRoomViewModel fireElevatorFrontRoomViewModel { get; set; }

        /// <summary>
        /// 独立或合用前室（楼梯间自然）
        /// </summary>
        public SeparateOrSharedNaturalViewModel separateOrSharedNaturalViewModel { get; set; }

        /// <summary>
        /// 独立或合用前室（楼梯间送风）
        /// </summary>
        public SeparateOrSharedWindViewModel separateOrSharedWindViewModel { get; set; }

        /// <summary>
        /// 楼梯间（前室不送风）
        /// </summary>
        public StaircaseNoWindViewModel staircaseNoWindViewModel { get; set; }

        /// <summary>
        /// 楼梯间（前室送风）
        /// </summary>
        public StaircaseWindViewModel staircaseWindViewModel { get; set; }

        /// <summary>
        /// 封闭避难层（间）、避难走道
        /// </summary>
        public EvacuationWalkViewModel evacuationWalkViewModel { get; set; }

        /// <summary>
        /// 避难走道前室
        /// </summary>
        public EvacuationFrontViewModel evacuationFrontViewModel { get; set; }

        /// <summary>
        /// 主面板映射model
        /// </summary>
        public SmokeCalculateMappingModel smokeCalculateMappingModel { get; set; }

        /// <summary>
        /// 当前选中块
        /// </summary>
        public ObjectId BlockId { get; set; }
    }
}
