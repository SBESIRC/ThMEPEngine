using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection
{
    public static class PubVar
    {
        public static List<FanPrefixDictDataModel> g_ListFanPrefixDict = new List<FanPrefixDictDataModel>()
        {
            new FanPrefixDictDataModel(){ No = 8, FanUse = "平时排风", Prefix ="EF", Explain = "包含燃烧和散热" },
            new FanPrefixDictDataModel(){ No = 9, FanUse = "平时送风", Prefix ="SF", Explain = "包含燃烧和散热" },
            new FanPrefixDictDataModel(){ No = 12, FanUse = "厨房排油烟", Prefix ="EKF", Explain = "" },

            new FanPrefixDictDataModel(){ No = 13, FanUse = "厨房排油烟补风", Prefix ="SF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 1, FanUse = "消防排烟", Prefix ="ESF", Explain = "" },
            new FanPrefixDictDataModel(){ No = 2, FanUse = "消防补风", Prefix ="SSF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 3, FanUse = "消防加压送风", Prefix ="SPF", Explain = "" },

            new FanPrefixDictDataModel(){ No = 10, FanUse = "事故排风", Prefix ="EF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 11, FanUse = "事故补风", Prefix ="SF", Explain = "平时风机,自动备注" },

            new FanPrefixDictDataModel(){ No = 4, FanUse = "消防排烟兼平时排风", Prefix ="E(S)F", Explain = "包含燃烧和散热" },
            new FanPrefixDictDataModel(){ No = 5, FanUse = "消防补风兼平时送风", Prefix ="S(S)F", Explain = "包含燃烧和散热" },

            new FanPrefixDictDataModel(){ No = 6, FanUse = "平时排风兼事故排风", Prefix ="EF", Explain = "平时风机,自动备注" },
            new FanPrefixDictDataModel(){ No = 7, FanUse = "平时送风兼事故补风", Prefix ="SF", Explain = "平时风机,自动备注" }
        };


        public static List<SceneResistaCalcModel> g_ListSceneResistaCalc = new List<SceneResistaCalcModel>()
        {
            new SceneResistaCalcModel(){ No = 1, Scene = "消防排烟", Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 2, Scene = "消防补风", Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 3, Scene = "消防加压送风", Friction = 3,  LocRes = 1.5 ,  Damper =0, DynPress = 60 },

            new SceneResistaCalcModel(){ No = 4, Scene = "厨房排油烟", Friction = 2,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 5, Scene = "厨房排油烟补风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 6, Scene = "平时送风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },
            new SceneResistaCalcModel(){ No = 7, Scene = "平时排风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },

            new SceneResistaCalcModel(){ No = 8, Scene = "消防排烟兼平时排风", Friction = 3,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 9, Scene = "消防补风兼平时送风", Friction = 3,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },

            new SceneResistaCalcModel(){ No = 10, Scene = "事故排风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 11, Scene = "事故补风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  },

            new SceneResistaCalcModel(){ No = 12, Scene = "平时送风兼事故补风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60 },
            new SceneResistaCalcModel(){ No = 13, Scene = "平时排风兼事故排风", Friction = 1,  LocRes = 1.5 ,  Damper =80, DynPress = 60  }
        };


        public static List<MotorEfficiency> g_ListMotorEfficiency = new List<MotorEfficiency>()
        {
            new MotorEfficiency(){ Key ="直连",Value =0.99 },

            new MotorEfficiency(){ Key ="皮带",Value =0.85 }
        };

    }
}
