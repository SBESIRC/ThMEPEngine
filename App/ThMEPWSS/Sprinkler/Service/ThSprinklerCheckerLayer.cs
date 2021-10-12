namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerCheckerLayer
    {
        public const string Blind_Zone_LayerName = "AI-喷头校核-盲区检测";
        public const string From_Boundary_So_Far_LayerName = "AI-喷头校核-喷头距边是否过大";
        public const string Room_Checker_LayerName = "AI-喷头校核-房间是否布置喷头";
        public const string Sprinkler_Distance_LayerName = "AI-喷头校核-喷头间距是否过小";
        public const string From_Boundary_So_Close_LayerName = "AI-喷头校核-喷头距边是否过小";
        public const string Distance_Form_Beam_LayerName = "AI-喷头校核-喷头距梁是否过小";
        public const string Layout_Area_LayerName = "AI-喷头校核-可布置区域";
        public const string Beam_Checker_LayerName = "AI-喷头校核-较高的梁";
        public const string Pipe_Checker_LayerName = "AI-喷头校核-喷头是否连管";
        public const string Duct_Checker_LayerName = "AI-喷头校核-宽度大于1200的风管";
        public const string Duct_Blind_Zone_LayerName = "AI-喷头校核-风管下喷盲区";
        public const string Sprinkler_So_Dense_LayerName = "AI-喷头校核-区域喷头过密";
    }
}
