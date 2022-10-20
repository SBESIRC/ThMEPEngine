﻿using System;

namespace ThPlatform3D.Common
{
    [Serializable]
    public class PlaneJsonConfig
    {
        public ObjConfig ObjConfig { get; set; }
        public BoxConfig BoxConfig { get; set; }
        public GlConfig GlConfig { get; set; }
        public ClipConfig ClipConfig { get; set; }
        public MergeConfig MergeConfig { get; set; }
        public SvgConfig SvgConfig { get; set; }
        public GlobalConfig GlobalConfig { get; set; }
        public DebugConfig DebugConfig { get; set; }
        public PlaneJsonConfig()
        {
            ObjConfig = new ObjConfig();
            BoxConfig = new BoxConfig();
            GlConfig = new GlConfig();
            ClipConfig = new ClipConfig();
            MergeConfig = new MergeConfig();
            SvgConfig = new SvgConfig();
            GlobalConfig= new GlobalConfig();
            DebugConfig = new DebugConfig();    
        }
    }
    [Serializable]
    public class ObjConfig
    {
        public string path { get; set; }
        public string current_floor { get; set; }
        public string high_floor { get; set; }
    }
    [Serializable]
    public class BoxConfig
    {
        public string x_min {get;set;}
        public string x_max { get; set; }
        public string y_min { get; set; }
        public string y_max { get; set; }
        public string z_min { get; set; }
        public string z_max { get; set; }
        public string angle { get; set; }
    }
    [Serializable]
    public class GlConfig
    {
        public int gl_size { get; set; }
        public bool use_cuda { get; set; }       
    }
    [Serializable]
    public class ClipConfig
    {
        //
    }
    [Serializable]
    public class MergeConfig
    {
        public bool apporx_merge_mode { get; set; }
        public string merge_mode { get; set; }
    }
    [Serializable]
    public class SvgConfig
    {
        public string image_size { get; set; }
        public string save_path { get; set; }
    }
    [Serializable]
    public class GlobalConfig
    {
        public string image_type { get; set; }
        public int? cut_position { get; set; }
        public int? relative_cut_position { get; set; }
        public Direction eye_dir { get; set; }
        public Direction up { get; set; }
        public int scale_size { get; set; }
    }
    [Serializable]
    public class DebugConfig
    {
        public bool print_time { get; set; } = true;
        public string log_path { get; set; }
    }
    [Serializable]
    public class Direction
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        public Direction()
        {
        }
        public Direction(int x,int y,int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
