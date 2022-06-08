using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHWallAttributes
{
    public class Height : TCHXmlBaseModel
    {
        public XmlDouble Net_height { get; set; }
        public XmlDouble Bottom_height { get; set; }
        public double WallHeight()
        {
            return WallMaxZ() - WallMinZ();
        }
        public double WallMaxZ()
        {
            if (null == Net_height)
                return 0.0;
            return Net_height.GetDoubleValue();
        }
        public double WallMinZ() 
        {

            if (null == Bottom_height)
                return 0.0;
            return Bottom_height.GetDoubleValue();
        }
    }
}
