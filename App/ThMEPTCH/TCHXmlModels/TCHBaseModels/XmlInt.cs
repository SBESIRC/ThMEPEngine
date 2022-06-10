namespace ThMEPTCH.TCHXmlModels.TCHBaseModels
{
    public class XmlInt:XmlString
    {
        public int GetIntValue() 
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            int.TryParse(value, out int intValue);
            return intValue;
        }
    }
}
