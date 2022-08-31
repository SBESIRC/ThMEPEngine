namespace ThMEPWSS.Model
{
    public class ThTCHPipeInfo
    {
        /// <summary>
        /// 是否包含天正水管
        /// </summary>
        public bool HasTCHPipe { get; set; }

        /// <summary>
        /// 水管系统
        /// </summary>
        public string System { get; set; }

        public ThTCHPipeInfo()
        {
            HasTCHPipe = false;
            System = "消防";
        }
    }
}
