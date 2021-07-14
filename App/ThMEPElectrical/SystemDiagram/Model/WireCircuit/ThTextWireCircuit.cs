using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    /// <summary>
    /// 专门用来绘画块数量和文字提示
    /// </summary>
    public class ThTextWireCircuit : ThWireCircuit
    {
        //Draw
        public override List<Entity> Draw()
        {
            List<Entity> Result = new List<Entity>();
            int CurrentIndex = this.StartIndexBlock;
            while (CurrentIndex <= EndIndexBlock)
            {
                int Quantity = 0;
                ThBlockConfigModel.BlockConfig.Where(o => o.Index == CurrentIndex).ToList().ForEach(x =>
                {
                    if (x.ShowQuantity && ((Quantity = this.fireDistrict.Data.BlockData.BlockStatistics[x.UniqueName]) > 0 || !x.CanHidden))
                    {
                        //V2.0 新增业务需求 【火灾声光警报器】显示的数量与【手动火灾报警按钮(带消防电话插座)】一致，但是内部计数还需要保留
                        if (x.UniqueName == "火灾声光警报器")
                        {
                            Quantity = this.fireDistrict.Data.BlockData.BlockStatistics["手动火灾报警按钮(带消防电话插座)"];
                        }
                        DBText QuantityText = new DBText() { Height = 250, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                        QuantityText.TextString = Quantity.ToString();
                        QuantityText.Position = x.QuantityPosition.Add(new Vector3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1), 0));
                        QuantityText.AlignmentPoint = QuantityText.Position;
                        Result.Add(QuantityText);
                    }
                    if (x.ShowText && (this.fireDistrict.Data.BlockData.BlockStatistics[x.UniqueName] > 0 || !x.CanHidden))
                    {
                        DBText Text = new DBText() { Height = 350, WidthFactor = 0.5, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3") };
                        Text.TextString = x.BlockNameRemark;
                        Text.Position = x.TextPosition.Add(new Vector3d(OuterFrameLength * (CurrentIndex - 1), OuterFrameLength * (FloorIndex - 1), 0));
                        Text.AlignmentPoint = Text.Position;
                        Result.Add(Text);
                    }
                });
                CurrentIndex++;
            }
            //设置线型
            Result.ForEach(o =>
            {
                o.Linetype = this.CircuitLinetype;
                o.Layer = this.CircuitLayer;
                o.ColorIndex = this.CircuitColorIndex;
            });

            return Result;
        }

        public override void InitCircuitConnection()
        {
            this.CircuitColorIndex = 6;
            this.CircuitLayer = "E-UNIV-NOTE";
            this.CircuitLinetype = "ByLayer";
            this.CircuitLayerLinetype = "CONTINUOUS";
            this.StartIndexBlock = 1;
            this.EndIndexBlock = 21;
        }
    }
}
