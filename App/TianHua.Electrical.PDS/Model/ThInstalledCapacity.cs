namespace TianHua.Electrical.PDS.Model
{
    public class ThInstalledCapacity
    {
        public ThInstalledCapacity()
        {
            IsDualPower = false;
            LowPower = 0;
            HighPower = 0;
        }

        /// <summary>
        /// 是否是双功率
        /// </summary>
        public bool IsDualPower { get; set; }

        /// <summary>
        /// 低功率
        /// </summary>
        public double LowPower { get; set; }

        /// <summary>
        /// 高功率
        /// </summary>
        public double HighPower { get; set; }

        public bool EqualsTo(ThInstalledCapacity other)
        {
            return this.IsDualPower == other.IsDualPower
                && this.LowPower == other.LowPower
                && this.HighPower == other.HighPower;
        }
    }
}
