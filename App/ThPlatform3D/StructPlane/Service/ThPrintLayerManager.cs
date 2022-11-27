using System.Collections.Generic;
using System.Linq;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThPrintLayerManager
    {
        public static string BeamLayerName = "S_BEAM";
        public static string ColumnLayerName = "S_COLU";
        public static string ConstructColumnLayerName = "S_CONS_构造柱";
        public static string ColumnHatchLayerName = "S_COLU_HACH";
        public static string ConstructColumnHatchLayerName = "S_CONS_构造柱填充";
        public static string BelowColumnLayerName = "S_COLU_BELW_DASH";
        public static string BelowColumnHatchLayerName = "S_COLU_BELW_DASH_HACH";
        public static string HoleLayerName = "S_HOLE";
        public static string HoleHatchLayerName = "S_HOLE_HACH";
        public static string ShearWallLayerName = "S_WALL";
        public static string ShearWallHatchLayerName = "S_WALL_HACH";
        public static string BelowShearWallLayerName = "S_WALL_BELW_DASH";
        public static string BelowShearWallHatchLayerName = "S_WALL_BELW_DASH_HACH";
        public static string PassHeightWallLayerName = "S_CONS_通高墙";
        public static string PassHeightWallHatchLayerName = "S_CONS_通高墙填充";
        public static string WindowWallLayerName = "S_CONS_窗台墙";
        public static string WindowWallHatchLayerName = "S_CONS_窗台墙填充";
        public static string PCWallLayer = "PC_YZ_WALL";
        public static string PCWallHatchLayer = "PC_YZ_WALL_HACH";
        public static string SlabLayerName = "S_PLAN_HACH";
        public static string SlabHatchLayerName = "S_PLAN_HACH";
        public static string BeamTextLayerName = "S_BEAM_TEXT_VERT";
        public static string SlabTextLayerName = "S_FLOR_THIK";
        public static string ElevationTableLineLayerName = "S_TABL";
        public static string ElevationTableTextLayerName = "S_TABL_NUMB";
        public static string SlabPatternTableTextLayerName = "S_PLAN_TEXT";
        public static string HeadTextLayerName = "S_PLAN_TOPC";
        public static string HeadTextDownLineLayerName = "S_PLAN_TOPC";
        public static string StairSlabCornerLineLayerName = "S_STAR_TEXT";
        public static string StairSlabCornerTextLayerName = "S_STAR_TEXT";
        public static string DefpointsLayerName = "Defpoints";
        public static List<string> AllLayers
        {
            get
            {
                var layers = new List<string>() { BeamLayerName , 
                    ColumnLayerName , ColumnHatchLayerName ,                    
                    BelowColumnLayerName,BelowColumnHatchLayerName,
                    ConstructColumnLayerName,ConstructColumnHatchLayerName,                    
                    ShearWallLayerName,ShearWallHatchLayerName,
                    BelowShearWallLayerName,BelowShearWallHatchLayerName,
                    PassHeightWallLayerName,PassHeightWallHatchLayerName,
                    WindowWallLayerName,WindowWallHatchLayerName,
                    PCWallLayer,PCWallHatchLayer,
                    HoleLayerName,HoleHatchLayerName,
                    SlabLayerName,SlabHatchLayerName,BeamTextLayerName,
                    SlabTextLayerName,ElevationTableLineLayerName,ElevationTableTextLayerName,
                    SlabPatternTableTextLayerName,HeadTextLayerName,HeadTextDownLineLayerName,
                StairSlabCornerLineLayerName,StairSlabCornerTextLayerName,DefpointsLayerName};
                return layers.Distinct().ToList();
            }
        }
        /// <summary>
        /// 从高到低
        /// </summary>
        public static List<string> PriorityLayers
        {
            get
            {
                // 其它的图层都高于这些
                return new List<string> {
                ShearWallHatchLayerName,
                ColumnHatchLayerName,
                BelowShearWallHatchLayerName,
                BelowColumnHatchLayerName};
            }
        }
    }
    internal class ThPrintStyleManager
    {
        public static string BeamTextStyleName = "TH-STYLE3";
        public static string SlabTextStyleName = "TH-STYLE1";
        public static string ElevationTableTextStyleName = "TH-STYLE3";
        public static string THSTYLE3 = "TH-STYLE3";
        public static string THSTYLE1 = "TH-STYLE1";
        public static string THSTYLE2 = "TH-STYLE2";
        public static List<string> AllTextStyles
        {
            get
            {
                var styles = new List<string> { BeamTextStyleName, SlabTextStyleName ,
                    ElevationTableTextStyleName,THSTYLE1,THSTYLE3 ,THSTYLE2};
                return styles.Distinct().ToList();
            }
        }
    }
    internal class ThPrintBlockManager
    {
        public static string BasePointBlkName = "BASEPOINT";
        public static string BthBlkName = "B-th";
        public static string SDemoH2BlkName = "S-demo-H2";
        public static List<string> AllBlockNames
        {
            get
            {
                return new List<string> { BthBlkName, SDemoH2BlkName , BasePointBlkName };
            }
        }
    }
}
