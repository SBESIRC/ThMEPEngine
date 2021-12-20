namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class SecondaryBeamLayoutConfig
    {
        public static string MainBeamLayerName = "TH_AI_BEAM";
        public static string SecondaryBeamLayerName = "TH_AICL_S_BEAM";
        public static short SecondaryBeamLayerColorIndex = 6;

        public static string MainBeamTextLayerName = "TH_AIZL_S_BEAM_TEXT";
        public static short MainBeamTextLayerColorIndex = 7;
        public static string SecondaryBeamTextLayerName = "TH_AICL_S_BEAM_TEXT";
        public static short SecondaryBeamTextLayerColorIndex = 7;

        public static double Da = 5500;//mm
        public static double Db = 6300;//mm
        public static double Dc = 3300;//mm
        public static double Er = 2.0;

        public static double AngleTolerance = 60; //容差：30°
    }
}
