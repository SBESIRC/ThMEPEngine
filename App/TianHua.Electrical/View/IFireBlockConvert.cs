﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public interface IFireBlockConvert
    {
        List<ViewFireBlockConvert> m_ListStrongBlockConvert { get; set; }


        List<ViewFireBlockConvert> m_ListWeakBlockConvert { get; set; }


        List<ViewGdvEidtData> m_ListLayingRatio { get; set; }
    }
}
