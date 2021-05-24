using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 块配置白名单
    /// 所有的块都在此配置，如新增块等操作
    /// </summary>
    public static class ThBlockConfigModel
    {
        public static List<ThBlockModel> BlockConfig;
        public static void Init()
        {
            if (!BlockConfig.IsNull())
                return;
            BlockConfig = new List<ThBlockModel>();
            #region #1
            #endregion
            #region #2
            #endregion
            #region #3
            #endregion
            #region #4
            #endregion
            #region #5
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS540",
                BlockAliasName = "E-BFAS540",
                BlockNameRemark = "短路隔离器",
                Index = 5,
                CanHidden = false,
                Position = new Point3d(700, 1500, 0),
                ShowQuantity=true,
                QuantityPosition=new Point3d(1050,1150,0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "SI" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS520",
                BlockAliasName = "E-BFAS520_消防广播火栓强制启动模块",
                BlockNameRemark = "消防广播火栓强制启动模块",
                Index = 5,
                CanHidden = false,
                Position = new Point3d(2300, 1150, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2650, 1150, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
            });
            #endregion
            #region #6
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS030",
                BlockAliasName = "E-BFAS030",
                BlockNameRemark = "区域显示器/火灾显示盘",
                Index = 6,
                CanHidden = true,
                Position = new Point3d(1500, 450, 0),
                ShowQuantity=true,
                QuantityPosition=new Point3d(1850,450,0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "D" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS031",
                BlockAliasName = "E-BFAS031",
                BlockNameRemark = "楼层或回路重复显示屏",
                Index = 6,
                CanHidden = true,
                Position = new Point3d(1500, 450, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0),
                ShowAtt = true,
                attNameValues = new Dictionary<string, string>() { { "F", "FI" } }
            });
            #endregion
            #region #7
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS212",
                BlockAliasName = "E-BFAS212",
                BlockNameRemark = "手动火灾报警按钮(带消防电话插座)",
                Index = 7,
                CanHidden = true,
                Position = new Point3d(1500, 1500, 0),
                ShowQuantity=true,
                QuantityPosition=new Point3d(1850,1150,0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS220",
                BlockAliasName = "E-BFAS220",
                BlockNameRemark = "火灾报警电话",
                Index = 7,
                CanHidden = true,
                Position = new Point3d(2250, 800, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS330",
                BlockAliasName = "E-BFAS330",
                BlockNameRemark = "火灾声光警报器",
                Index = 7,
                CanHidden = true,
                Position = new Point3d(1500, 800, 0),
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0)
            });
            #endregion
            #region #8
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS410-2",
                BlockAliasName = "E-BFAS410-2",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(1500, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "C" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS410-3",
                BlockAliasName = "E-BFAS410-3",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(750, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "R" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS410-4",
                BlockAliasName = "E-BFAS410-4",
                BlockNameRemark = "火灾应急广播扬声器",
                Index = 8,
                Position = new Point3d(2250, 800, 0),
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "W" } }
            });
            #endregion
            #region #9
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS110",
                BlockAliasName = "E-BFAS110",
                BlockNameRemark = "感烟火灾探测器",
                Index = 9,
                CanHidden = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                Position = new Point3d(750, 1500, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS120",
                BlockAliasName = "E-BFAS120",
                BlockNameRemark = "感温火灾探测器",
                Index = 9,
                CanHidden = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                Position = new Point3d(2250, 1500, 0)
            });
            #endregion
            #region #10
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS112",
                BlockAliasName = "E-BFAS112",
                BlockNameRemark = "红外光束感烟火灾探测器发射器",
                Index = 10,
                CanHidden = true,
                Position = new Point3d(1000, 1500, 0)
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS113",
                BlockAliasName = "E-BFAS113",
                BlockNameRemark = "红外光束感烟火灾探测器接收器",
                Index = 10,
                CanHidden = true,
                Position = new Point3d(2000, 1500, 0)
            });
            #endregion
            #region #11
            #endregion
            #region #12
            #endregion
            #region #13
            #endregion
            #region #14
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS711",
                BlockAliasName = "E-BFAS711",
                BlockNameRemark = "70℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(750, 1500, 0),
                CanHidden=true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1100, 1150, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "70℃" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS713",
                BlockAliasName = "E-BFAS712",
                BlockNameRemark = "150℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(1500, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 1150, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "150℃" } }
            });
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS712",
                BlockAliasName = "E-BFAS713",
                BlockNameRemark = "280℃防火阀+输入模块",
                Index = 14,
                Position = new Point3d(2250, 1500, 0),
                CanHidden = true,
                ShowAtt = true,
                ShowQuantity = true,
                QuantityPosition = new Point3d(2600, 1150, 0),
                ShowText=true,
                TextPosition = new Point3d(2600, 450, 0),
                attNameValues = new Dictionary<string, string>() { { "F", "280℃" } }
            });
            #endregion
            #region #15
            #endregion
            #region #16
            BlockConfig.Add(new ThBlockModel()
            {
                BlockName = "E-BFAS522",
                BlockAliasName = "E-BFAS522_16",
                BlockNameRemark = "防排抽烟机",
                Index = 16,
                Position = new Point3d(1100, 1200, 0),
                CanHidden = true,
                ShowAtt = false,
                ShowQuantity = true,
                QuantityPosition = new Point3d(1850, 850, 0),
                ShowText = true,
                TextPosition = new Point3d(1500, 450, 0),
                HasMultipleBlocks = true,
                AssociatedBlocks = new List<ThBlockModel>()
                {
                    new ThBlockModel()
                    {
                        BlockName = "E-BDB004",
                        BlockAliasName = "E-BDB004",
                        BlockNameRemark = "防排抽烟机_1",
                        Index = 16,
                        Position = new Point3d(1500, 1200, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "BOX", "APE" } }
                    },
                    new ThBlockModel()
                    {
                        BlockName = "E-BFAS011",
                        BlockAliasName = "E-BFAS011",
                        BlockNameRemark = "防排抽烟机_2",
                        Index = 16,
                        Position = new Point3d(1500, 1500, 0),
                        ShowAtt=true,
                        attNameValues = new Dictionary<string, string>() { { "F", "I/O" } }
                    }
                }
            });
            #endregion
            #region #17

            #endregion
            #region #18
            #endregion
            #region #19
            #endregion
            #region #20
            #endregion
            #region #21
            #endregion
        }
    }
}
