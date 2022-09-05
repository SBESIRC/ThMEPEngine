namespace ThPlatform3D.ArchitecturePlane
{
    /// <summary>
    /// 建筑平面图通用参数
    /// </summary>
    internal class ThArchitecturePlaneCommon
    {
        private static readonly ThArchitecturePlaneCommon instance = new ThArchitecturePlaneCommon() { };
        static ThArchitecturePlaneCommon()
        {
        }
        internal ThArchitecturePlaneCommon()
        {
            PointTolerance = 1.0;
            WallWindowThickRatio = 1.5;
            WallArcTessellateLength = 100.0;
            DoorMarkBoundaryDistanceToWall = 50.0;
            WindowMarkBoundaryDistanceToWall = 50.0;
        }
        public static ThArchitecturePlaneCommon Instance { get { return instance; } }
        /// <summary>
        /// 梁标注边界距离墙
        /// </summary>
        public double DoorMarkBoundaryDistanceToWall { get; private set;}
        public double WindowMarkBoundaryDistanceToWall { get; private set;}
        public double PointTolerance { get; private set;}
        public double WallArcTessellateLength { get; private set; }   
        public double WallWindowThickRatio { get; private set; }
    }
}
