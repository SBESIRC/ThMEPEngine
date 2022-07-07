namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertBlockNameService
    {
        public static bool BlockNameEquals(string sourceStr, string targetStr)
        {
            // 对电动机及负载标注、负载标注单独处理
            if (targetStr.Contains(ThBConvertCommon.BLOCK_LOAD_DIMENSION))
            {
                return KeepChinese(sourceStr).Equals(KeepChinese(targetStr));
            }
            else
            {
                return sourceStr.Equals(targetStr);
            }
        }

        public static string KeepChinese(this string str)
        {
            //声明存储结果的字符串
            var chineseString = "";

            //将传入参数中的中文字符添加到结果字符串中
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] >= 0x4E00 && str[i] <= 0x9FA5) //汉字
                {
                    chineseString += str[i];
                }
            }

            //返回保留中文的处理结果
            return chineseString;
        }
    }
}
