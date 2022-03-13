﻿using System;
using System.Linq;
using ThMEPHVAC.IO;

namespace ThMEPHVAC.CAD
{
    public enum PortRecommendType
    {
        PORT,
        VERTICAL_PIPE
    }
    public class ThPortParameter
    {
        public PortSizeParameter DuctSizeInfor { get; set; }
        public ThPortParameter(double airVolume, double verticalPipeWidth, PortRecommendType type)
        {
            if (type == PortRecommendType.PORT)
                DuctSizeInfor = GetCandidatePortSize(airVolume, 2.0);
            if (type == PortRecommendType.VERTICAL_PIPE)
                DuctSizeInfor = GetCandidateVerticalPipeSize(airVolume, verticalPipeWidth, 3.0);
        }
        private PortSizeParameter GetCandidateVerticalPipeSize(double airVolume, double verticalPipeWidth, double speed)
        {
            double area = Round2float(airVolume / 3600.0 / speed);
            var jsonReader = new ThPortParameterJsonReader();
            var sizeFloor = jsonReader.Parameters.Where(d => d.SectionArea >= area).OrderBy(d => d.SectionArea);
            var filterSize = sizeFloor.Where(e => Math.Abs(e.DuctWidth - verticalPipeWidth) < 1e-3);
            if (filterSize.Count() == 0)
                return new PortSizeParameter() { DuctWidth = 500, DuctHeight = 200, SectionArea = 0.1, AspectRatio = 2.5 };
            return filterSize.First(d => d.DuctHeight == filterSize.Min(f => f.DuctHeight));
        }
        private PortSizeParameter GetCandidatePortSize(double airVolume, double speed)
        {
            double area = Round2float(airVolume / 3600.0 / speed);
            var jsonReader = new ThPortParameterJsonReader();
            var sizeFloor = jsonReader.Parameters.Where(d => d.SectionArea >= area).OrderBy(d => d.SectionArea);
            var filterSize = sizeFloor.Where(e => e.AspectRatio <= 4);
            if (filterSize.Count() == 0)
                return new PortSizeParameter() { DuctWidth = 500, DuctHeight = 200, SectionArea = 0.1, AspectRatio = 2.5};
            return filterSize.First(d => d.DuctHeight == filterSize.Min(f => f.DuctHeight));
        }
        private double Round2float(double f)
        {
            string s = f.ToString("#0.00");
            return Double.Parse(s);
        }
    }
}
