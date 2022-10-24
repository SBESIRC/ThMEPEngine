using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Model
{
    public enum LayoutSpace
    {
        WalkayRampSpace,    // 走道车道布置
        OtherSpace,         // 其他区域布置 
    }

    public enum HazardLevel
    {
        FirstLevel,         //轻危险等级
        SecondLevel,        //中危险等级一级
        ThirdLevel,         //中危险等级二级
        SeriousLevel,       //严重危险等级
    }

    public enum LayoutRange 
    {
        StandardRange,      //标准覆盖
        ExpandRange,        //扩大覆盖
    }


    public enum LayoutType
    {
        UpSpray,            //上喷
        DownSpray           //下喷
    }

    public enum BlindAreaType
    {
        //Rectangle = 0,          //矩形
        SmallCircle = 1000,        //圆形-低
        MedianCircle = 500,       //圆形-中
        BigCircle = 200,          //圆形-高
    }

    public class ThWSSParameter
    {
        //应用区域
        public LayoutSpace applicationSite = LayoutSpace.OtherSpace;
        //危险等级
        public HazardLevel hazardLevel = HazardLevel.ThirdLevel;
        //喷头覆盖范围
        public LayoutRange layoutRange = LayoutRange.StandardRange;
        //喷头类型（上喷，下喷）
        public LayoutType layoutType = LayoutType.UpSpray;
        //是否考虑梁
        public bool ConsiderBeam = true;
        //是否考虑使用天正喷头布置
        public bool ConsiderTCH = true;
        //盲区表达方式
        public BlindAreaType blindAreaType = BlindAreaType.MedianCircle;
        //喷头间距
        public double distance = 3400;

        /// <summary>
        /// 保护半径
        /// </summary>
        public double protectRange
        {
            get
            {
                double value = 3400;
                if (layoutRange == LayoutRange.StandardRange)
                {
                    switch (hazardLevel)
                    {
                        case HazardLevel.FirstLevel:
                            value = 4400;
                            break;
                        case HazardLevel.SecondLevel:
                            value = 3600;
                            break;
                        case HazardLevel.ThirdLevel:
                            value = 3400;
                            break;
                        case HazardLevel.SeriousLevel:
                            value = 3000;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (hazardLevel)
                    {
                        case HazardLevel.FirstLevel:
                            value = 5400;
                            break;
                        case HazardLevel.SecondLevel:
                            value = 4800;
                            break;
                        case HazardLevel.ThirdLevel:
                            value = 4200;
                            break;
                        case HazardLevel.SeriousLevel:
                            value = 3600;
                            break;
                        default:
                            break;
                    }
                }

                return value;
            }
        }

        public ThWSSParameter(LayoutSpace space = LayoutSpace.OtherSpace, HazardLevel level = HazardLevel.ThirdLevel, LayoutRange range = LayoutRange.StandardRange,
            LayoutType type = LayoutType.UpSpray, bool hasBeam = true, bool hasTCH = true, BlindAreaType blindArea = BlindAreaType.MedianCircle)
        {
            applicationSite = space;
            hazardLevel = level;
            layoutRange = range;
            layoutType = type;
            ConsiderBeam = hasBeam;
            ConsiderTCH = hasTCH;
            blindAreaType = blindArea;
        }
    }
}
