using Linq2Acad;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDbUtils
    {
        public static bool UpdateLayerSettings(string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ltr = acadDatabase.Layers.ElementOrDefault(name, true);
                if (ltr == null)
                {
                    return false;
                }

                // 如果当前图层等于插入图层，暂不处理
                if (acadDatabase.Database.Clayer.Equals(ltr.ObjectId))
                {
                    return false;
                }

                // 设置图层状态
                ltr.IsOff = false;
                ltr.IsFrozen = false;
                ltr.IsLocked = false;
                ltr.IsPlottable = true;

                return true;
            }
        }
    }
}
