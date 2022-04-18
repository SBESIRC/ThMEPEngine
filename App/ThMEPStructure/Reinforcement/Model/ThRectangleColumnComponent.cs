namespace ThMEPStructure.Reinforcement.Model
{
    public class ThRectangleColumnComponent : ThColumnComponent
    {
        public int Bw { get; set; }
        public int Hc { get; set; }

        /// <summary>
        /// 拉筋
        /// </summary>
        public string Link2 { get; set; } = "";
        /// <summary>
        /// 拉筋
        /// </summary>
        public string Link3 { get; set; } = "";
    }
}
