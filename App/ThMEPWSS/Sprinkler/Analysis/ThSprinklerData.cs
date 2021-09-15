using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerData
    {
        public string Type { get; set; }
        public string HorizontalMirror { get; set; }
        public string VerticalMirror { get; set; }
        public double Rotation { get; set; }
        public Point3d Position { get; set; }

        public ThSprinklerData(Dictionary<string, object> data, string name)
        {
            if(name == "$TwtSys$00000131")
            {
                Type = "侧喷";
            }
            else if(name == "$TwtSys00000125")
            {
                if(data["遮挡管线"] as string == "是")
                {
                    Type = "上喷";
                }
                else if(data["遮挡管线"] as string == "否")
                {
                    Type = "下喷";
                }
            }
            HorizontalMirror = data["横向镜像"] as string;
            VerticalMirror = data["纵向镜像"] as string;
            Rotation = Convert.ToDouble(data["旋转角度"] as string);
        }
    }
}
