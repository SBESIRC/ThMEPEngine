using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public static class ThTextureMaterialManager
    {
        public readonly static string THReinforcedConcrete = "TH-钢筋混凝土";
        public readonly static string THAeratedconcrete = "TH-加气混凝土";
        public readonly static string THConcrete = "TH-素混凝土";
        public readonly static string THStone = "TH-石材";
        public readonly static string ThMenChuangKaiqiShikuai = "门窗开启体块";
        public readonly static string THNoColourFnsh = "SECTION-ERROR-MATERIAL-无颜色线脚";
        public readonly static string THQDCommonBrick = "QD_COMMONBRICK";
        public readonly static string THInsulationLayer = "TH-NET";
        public readonly static string THRailing = "TH-栏杆"; 

        static ThTextureMaterialManager()
        {
            AllMaterials = new List<string>() {
                    THReinforcedConcrete, THAeratedconcrete, THConcrete , THStone ,
                    ThMenChuangKaiqiShikuai ,THNoColourFnsh,THQDCommonBrick,THInsulationLayer,THRailing};
        }
        public static List<string> AllMaterials { get; } 
    }
}
