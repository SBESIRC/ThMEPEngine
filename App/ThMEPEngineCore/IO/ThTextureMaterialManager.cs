using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public static class ThTextureMaterialManager
    {
        public readonly static string THSteelConcrete = "TH-钢筋混凝土";
        public readonly static string THGasConcrete = "TH-加气混凝土";
        public readonly static string THSuConcrete = "TH-素混凝土";
        public readonly static string THShiCai = "TH-石材";
        public readonly static string ThMenChuangKaiqiShikuai = "门窗开启体块";
        public readonly static string THNoColourFnsh = "SECTION-ERROR-MATERIAL-无颜色线脚";
        public readonly static string THQD_COMMONBRICK = "QD_COMMONBRICK";
        public readonly static string THBaoWenCeng = "TH-NET";
        public readonly static string THLanGan = "TH-栏杆"; 

        static ThTextureMaterialManager()
        {
            AllMaterials = new List<string>() {
                    THSteelConcrete, THGasConcrete, THSuConcrete , THShiCai ,
                    ThMenChuangKaiqiShikuai ,THNoColourFnsh,THQD_COMMONBRICK,THBaoWenCeng,THLanGan};
        }
        public static List<string> AllMaterials { get; } 
    }
}
