namespace ThMEPTCH.TCHXmlModels.TCHBaseModels
{
    public class XmlDouble : XmlString
    {
        public double GetDoubleValue()
        {
            if (string.IsNullOrEmpty(value))
                return 0.0;
            double.TryParse(value, out double dValue);
            return dValue;
        }
    }
}
