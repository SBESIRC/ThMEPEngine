using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    internal abstract class ThEdgeComponentStirrupEnhanceService
    {
        protected readonly double DoubleTolerance = 1e-3;
        /// <summary>
        /// 从YJK计算书中提取的配筋率
        /// </summary>
        protected double StirrupRatio { get; set; } 
        public bool IsSuccess { get; protected set; }
        protected ThEdgeComponent EdgeComponent { get; set; }
        public ThEdgeComponentStirrupEnhanceService(
            ThEdgeComponent edgeComponent, double stirrupRatio)
        {
            StirrupRatio = stirrupRatio;
            EdgeComponent = edgeComponent;
        }
        public abstract void Enhance();
        protected string ToStirrupSpec(int diameter,int spacing)
        {
            //C8@120
            return "C" + diameter + "@" + spacing;
        }
        protected string ToLinkSpec(int count,int diameter,int spacing)
        {
            //1C8@120
            return count+"C"+ diameter + "@" + spacing;
        }
        protected bool IsBiggerThanStirrupRatio(double pvcal)
        {
            //表中pvcal = 0.915，yjk提取值=0.92,即满足yjk提取值 <= 实配值
            return ThReinforcementUtils.IsBiggerThan(pvcal, StirrupRatio, 2);
        }
        protected string EnlargeStirrupDiameter(string stirrup)
        {
            // 将箍筋直径增大一级
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(stirrup);
            if (stirrupDatas.Count == 2)
            {
                var enhanceDia = ThSteelDataManager.Instance.FindEnhancedDiameter(stirrupDatas[0]);
                if (enhanceDia.HasValue)
                {
                    return ToStirrupSpec((int)enhanceDia.Value, stirrupDatas[1]);
                }
            }
            return "";
        }
    }
    internal class ThRectEdgeComponentStirrupEnhanceService :
        ThEdgeComponentStirrupEnhanceService
    {
        private ThRectangleEdgeComponent RectEdgeComponent { get; set; }
        public ThRectEdgeComponentStirrupEnhanceService
            (ThRectangleEdgeComponent edgeComponent, double stirrupRatio) 
            :base(edgeComponent, stirrupRatio)
        {
            RectEdgeComponent = edgeComponent;
        }
        public override void Enhance()
        {
            // Step1: 增大拉筋2、3的直径与箍筋1的直径相同（若2、3直径就与1相同，则直接进入迭代2）
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if(IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step2: 在迭代1的基础上，加大箍筋1的直径一级
            var newStirrup1 = EnlargeStirrupDiameter(RectEdgeComponent.Stirrup);
            if(!string.IsNullOrEmpty(newStirrup1))
            {
                RectEdgeComponent.Stirrup = newStirrup1;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step3: 在迭代2的基础上，增大拉筋2、3的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step4: 在迭代3的基础上，继续加大箍筋1的直径1级
            var newStirrup2 = EnlargeStirrupDiameter(RectEdgeComponent.Stirrup);
            if (!string.IsNullOrEmpty(newStirrup2))
            {
                RectEdgeComponent.Stirrup = newStirrup2;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step5: 在迭代4的基础上，增大拉筋2、3的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step6: 在迭代5的基础上，减小箍筋/拉筋间距，每次减小间距5，直至满足要求；（最小间距>=80）
            // 若仍不满足要求，输出最后迭代步的箍筋值
            AdjustSpacing();
        }
        private bool SetDiameterIdentical()
        {
            bool isSet = false; // 是否进行了设置
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(RectEdgeComponent.Stirrup);
            if(stirrupDatas.Count!=2)
            {
                return isSet;
            }
            var link2Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link2);
            var link3Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link3);
            if (link2Datas.Count == 3)
            {
                if(link2Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    RectEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        stirrupDatas[0], link2Datas[2]);
                }
            }
            if (link3Datas.Count==3)
            {
                if (link3Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    RectEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        stirrupDatas[0], link3Datas[2]);
                }
            }
            return isSet;
        }
        private bool SetSpacingIdentical()
        {
            bool isSet = false; // 是否进行了设置
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(RectEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return isSet;
            }
            var link2Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link2);
            var link3Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link3);
            if (link2Datas.Count == 3)
            {
                if (link2Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    RectEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        link2Datas[1], stirrupDatas[1]);
                }
            }
            if (link3Datas.Count == 3)
            {
                if (link3Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    RectEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        link3Datas[1], stirrupDatas[1]);
                }
            }
            return isSet;
        }
        private void AdjustSpacing()
        {
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(RectEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return;
            }
            int spacing = stirrupDatas[1] -5;
            while (spacing >= 80)
            {
                RectEdgeComponent.Stirrup = ToStirrupSpec(stirrupDatas[0], spacing);
                bool isSet = SetSpacingIdentical();
                if (isSet == false)
                {
                    break;
                }
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    break;
                }
                spacing -= 5;
            }
        }

        private double CalculatePvcal()
        {
            /*
             *             2Asv1(bw-2*a)+ 2Asv1(hc-2*a)+n2 *Asv2*(bw-2a) + n3*Asv3*(hc-2*a)
             *     Pvcal = -----------------------------------------------------------------
             *                         (bw - 2*a - d1) * (hc - 2*a - d1) * s
             */
            // init members            
            double Pvcal = 0.0;// 箍筋体积配箍率计算值
            int bw = RectEdgeComponent.Bw; // 轮廓宽度
            int hc = RectEdgeComponent.Hc; // 轮廓高度
            double c = RectEdgeComponent.C; // 保护层厚度
            // 直径，间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(RectEdgeComponent.Stirrup);
            if(stirrupDatas.Count!=2)
            {
                return Pvcal;
            }            
            int d1 = stirrupDatas[0]; // 箍筋直径
            int s = stirrupDatas[1]; //箍筋间距
            // 箍筋面积
            double asv1 = ThSteelDataManager.Instance.GetSteelArea(d1, DoubleTolerance);           
            double a = c + d1 / 2.0;
            // 拉筋2的数据： 根数，直径，间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link2);            
            int n2 = 0; // 拉筋2肢数
            double asv2 = 0.0; // 拉筋2直径对应的截面面积
            if (link2Datas.Count==3)
            {
                n2 = link2Datas[0];
                asv2 = ThSteelDataManager.Instance.GetSteelArea(link2Datas[1], DoubleTolerance);
            }
            // 拉筋3的数据： 根数，直径，间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(RectEdgeComponent.Link3);
            int n3 = 0; // 拉筋3肢数
            double asv3 = 0.0; // 拉筋3直径对应的截面面积
            if (link3Datas.Count == 3)
            {
                n3 = link3Datas[0];
                asv3 = ThSteelDataManager.Instance.GetSteelArea(link3Datas[1], DoubleTolerance);
            }

            // Calculate
            var formula1 = 2 * asv1 * (bw - 2 * a);
            var formula2 = 2 * asv1 * (hc - 2 * a);
            var formula3 = n2 * asv2 * (bw - 2 * a);
            var formula4 = n3 * asv3 * (hc - 2 * a);
            var formula5 = bw - 2 * a - d1;
            var formula6 = hc - 2 * a - d1;
            var molecule = formula1 + formula2 + formula3 + formula4; // 分子
            var denominator = formula5 * formula6 * s; // 分母
            if (denominator != 0.0)
            {
                Pvcal = molecule / denominator;
            }
            return Pvcal;
        }
    }
    internal class ThLTypeEdgeComponentStirrupEnhanceService :
        ThEdgeComponentStirrupEnhanceService
    {
        private ThLTypeEdgeComponent LTypeEdgeComponent { get; set; }
        public ThLTypeEdgeComponentStirrupEnhanceService
            (ThLTypeEdgeComponent edgeComponent, double stirrupRatio)
            :base (edgeComponent, stirrupRatio)
        {
            LTypeEdgeComponent = edgeComponent;
        }
        public override void Enhance()
        {
            // Step1: 增大拉筋2、3、4的直径与箍筋1的直径相同（若2、3、4直径就与1相同，则直接进入迭代2）
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step2: 在迭代1的基础上，加大箍筋1的直径一级
            var newStirrup1 = EnlargeStirrupDiameter(LTypeEdgeComponent.Stirrup);
            if (!string.IsNullOrEmpty(newStirrup1))
            {
                LTypeEdgeComponent.Stirrup = newStirrup1;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step3: 在迭代2的基础上，增大拉筋2、3、4的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step4: 在迭代3的基础上，继续加大箍筋1的直径1级
            var newStirrup2 = EnlargeStirrupDiameter(LTypeEdgeComponent.Stirrup);
            if (!string.IsNullOrEmpty(newStirrup2))
            {
                LTypeEdgeComponent.Stirrup = newStirrup2;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step5: 在迭代4的基础上，增大拉筋2、3、4的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step6: 在迭代5的基础上，减小箍筋/拉筋间距，每次减小间距5，直至满足要求；（最小间距>=80）
            // 若仍不满足要求，输出最后迭代步的箍筋值
            AdjustSpacing();
        }
        private bool SetDiameterIdentical()
        {
            bool isSet = false; // 是否进行了设置
            // 直径、间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(LTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return isSet;
            }
            // 肢数、直径、间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link2);
            if (link2Datas.Count == 3)
            {
                if (link2Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        stirrupDatas[0], link2Datas[2]);
                }
            }
            // 肢数、直径、间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link3);
            if (link3Datas.Count == 3)
            {
                if (link3Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        stirrupDatas[0], link3Datas[2]);
                }
            }
            // 肢数、直径、间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link4);
            if (link4Datas.Count == 3)
            {
                if (link4Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link4 = ToLinkSpec(link4Datas[0],
                        stirrupDatas[0], link4Datas[2]);
                }
            }
            return isSet;
        }
        private bool SetSpacingIdentical()
        {
            bool isSet = false; // 是否进行了设置
            // 直径、间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(LTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return isSet;
            }
            // 肢数、直径、间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link2);           
            if (link2Datas.Count == 3)
            {
                if (link2Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        link2Datas[1], stirrupDatas[1]);
                }
            }
            // 肢数、直径、间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link3);
            if (link3Datas.Count == 3)
            {
                if (link3Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        link3Datas[1], stirrupDatas[1]);
                }
            }
            // 肢数、直径、间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link4);
            if (link4Datas.Count == 3)
            {
                if (link4Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    LTypeEdgeComponent.Link4 = ToLinkSpec(link4Datas[0],
                        link4Datas[1], stirrupDatas[1]);
                }
            }
            return isSet;
        }
        private void AdjustSpacing()
        {
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(LTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return;
            }
            int spacing = stirrupDatas[1] - 5;
            while (spacing >= 80)
            {
                LTypeEdgeComponent.Stirrup = ToStirrupSpec(stirrupDatas[0], spacing);
                bool isSet = SetSpacingIdentical();
                if (isSet == false)
                {
                    break;
                }
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    break;
                }
                spacing -= 5;
            }
        }
        private double CalculatePvcal()
        {
            /*
             *            Asv1(3*bw+3*bf+2*hc1+2*hc2-12*a) + n2*Asv2*(bf-2*a) + n3*Asv3*(bw-2*a) + Asv4*(bw+hc1+bf+hc2-4*a)
             *    Pvcal = -------------------------------------------------------------------------------------------------
             *                    [(bw+hc1-2*a-d1)*(bf-2*a-d1) + (bw-2*a-d1)*hc2]*s
             */
            // init members
            double Pvcal = 0.0;
            int bw = LTypeEdgeComponent.Bw;
            int bf = LTypeEdgeComponent.Bf;
            int hc1 = LTypeEdgeComponent.Hc1;
            int hc2 = LTypeEdgeComponent.Hc2;
            double c = LTypeEdgeComponent.C; // 保护层厚度
            // 直径，间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(LTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return Pvcal;
            }
            int d1 = stirrupDatas[0]; // 箍筋直径
            int s = stirrupDatas[1]; //箍筋间距
            // 箍筋面积
            double asv1 = ThSteelDataManager.Instance.GetSteelArea(d1, DoubleTolerance);
            double a = c + d1 / 2.0;

            // 拉筋2的数据： 根数，直径，间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link2);
            int n2 = 0; // 拉筋2肢数
            double asv2 = 0.0; // 拉筋2直径对应的截面面积
            if (link2Datas.Count == 3)
            {
                n2 = link2Datas[0];
                asv2 = ThSteelDataManager.Instance.GetSteelArea(link2Datas[1], DoubleTolerance);
            }

            // 拉筋3的数据： 根数，直径，间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link3);
            int n3 = 0; // 拉筋3肢数
            double asv3 = 0.0; // 拉筋3直径对应的截面面积
            if (link3Datas.Count == 3)
            {
                n3 = link3Datas[0];
                asv3 = ThSteelDataManager.Instance.GetSteelArea(link3Datas[1], DoubleTolerance);
            }

            // 拉筋4的数据： 根数，直径，间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(LTypeEdgeComponent.Link4);
            int n4 = 0; // 拉筋4肢数
            double asv4 = 0.0; // 拉筋4直径对应的截面面积
            if (link4Datas.Count == 3)
            {
                n4 = link4Datas[0];
                asv4 = ThSteelDataManager.Instance.GetSteelArea(link4Datas[1], DoubleTolerance);
            }

            // Calculate
            var formula1 = asv1*(3*bw+3*bf+2*hc1+2*hc2-12*a);
            var formula2 = n2 * asv2 * (bf - 2 * a);
            var formula3 = n3 * asv3 * (bw - 2 * a);
            var formula4 = asv4 * (bw + hc1 + bf + hc2 - 4 * a);
            var formula5 = bw + hc1 - 2 * a - d1;
            var formula6 = bf - 2 * a - d1;
            var formula7 = (bw - 2 * a - d1) * hc2;
            var molecule = formula1+ formula2+ formula3+ formula4; // 分子
            var denominator = (formula5* formula6+ formula7)*s;    // 分母
            if(denominator!=0.0)
            {
                Pvcal = molecule / denominator;
            }
            return Pvcal;
        }
    }
    internal class ThTTypeEdgeComponentStirrupEnhanceService :
        ThEdgeComponentStirrupEnhanceService
    {
        private ThTTypeEdgeComponent TTypeEdgeComponent { get; set; }
        public ThTTypeEdgeComponentStirrupEnhanceService
            (ThTTypeEdgeComponent edgeComponent, double stirrupRatio)
            : base(edgeComponent, stirrupRatio)
        {
            TTypeEdgeComponent = edgeComponent; 
        }
        public override void Enhance()
        {
            // Step1: 增大拉筋2、3、4的直径与箍筋1的直径相同（若2、3、4直径就与1相同，则直接进入迭代2）
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step2: 在迭代1的基础上，加大箍筋1的直径一级
            var newStirrup1 = EnlargeStirrupDiameter(TTypeEdgeComponent.Stirrup);
            if (!string.IsNullOrEmpty(newStirrup1))
            {
                TTypeEdgeComponent.Stirrup = newStirrup1;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step3: 在迭代2的基础上，增大拉筋2、3、4的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step4: 在迭代3的基础上，继续加大箍筋1的直径1级
            var newStirrup2 = EnlargeStirrupDiameter(TTypeEdgeComponent.Stirrup);
            if (!string.IsNullOrEmpty(newStirrup2))
            {
                TTypeEdgeComponent.Stirrup = newStirrup2;
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step5: 在迭代4的基础上，增大拉筋2、3、4的直径与箍筋1的直径相同
            if (SetDiameterIdentical())
            {
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    return;
                }
            }

            // Step6: 在迭代5的基础上，减小箍筋/拉筋间距，每次减小间距5，直至满足要求；（最小间距>=80）
            // 若仍不满足要求，输出最后迭代步的箍筋值
            AdjustSpacing();
        }
        private bool SetDiameterIdentical()
        {
            bool isSet = false; // 是否进行了设置
            // 直径、间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(TTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return isSet;
            }
            // 肢数、直径、间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link2);
            if (link2Datas.Count == 3)
            {
                if (link2Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        stirrupDatas[0], link2Datas[2]);
                }
            }
            // 肢数、直径、间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link3);
            if (link3Datas.Count == 3)
            {
                if (link3Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        stirrupDatas[0], link3Datas[2]);
                }
            }
            // 肢数、直径、间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link4);
            if (link4Datas.Count == 3)
            {
                if (link4Datas[1] != stirrupDatas[0])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link4 = ToLinkSpec(link4Datas[0],
                        stirrupDatas[0], link4Datas[2]);
                }
            }
            return isSet;
        }
        private bool SetSpacingIdentical()
        {
            bool isSet = false; // 是否进行了设置
            // 直径、间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(TTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return isSet;
            }
            // 肢数、直径、间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link2);
            if (link2Datas.Count == 3)
            {
                if (link2Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link2 = ToLinkSpec(link2Datas[0],
                        link2Datas[1], stirrupDatas[1]);
                }
            }
            // 肢数、直径、间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link3);
            if (link3Datas.Count == 3)
            {
                if (link3Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link3 = ToLinkSpec(link3Datas[0],
                        link3Datas[1], stirrupDatas[1]);
                }
            }
            // 肢数、直径、间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link4);
            if (link4Datas.Count == 3)
            {
                if (link4Datas[2] != stirrupDatas[1])
                {
                    isSet = true;
                    TTypeEdgeComponent.Link4 = ToLinkSpec(link4Datas[0],
                        link4Datas[1], stirrupDatas[1]);
                }
            }
            return isSet;
        }
        private void AdjustSpacing()
        {
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(TTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return;
            }
            int spacing = stirrupDatas[1] - 5;
            while (spacing >= 80)
            {
                TTypeEdgeComponent.Stirrup = ToStirrupSpec(stirrupDatas[0], spacing);
                bool isSet = SetSpacingIdentical();
                if (isSet == false)
                {
                    break;
                }
                var pvcal = CalculatePvcal();
                if (IsBiggerThanStirrupRatio(pvcal))
                {
                    IsSuccess = true;
                    break;
                }
                spacing -= 5;
            }
        }
        private double CalculatePvcal()
        {
            /*
             *            Asv1(4*bw+bf+2*hc1+2*hc2-14*a) + n2*Asv2*(bf-2*a) + n3*Asv3*(bw-2*a) + Asv4*(bw+hc1+hc2-4*a)
             *    Pvcal = -----------------------------------------------------------------------------------------------
             *                    [(bw-2*a-d1)*(hc2-2*a-d1) + (bf-2*a-d1)*hc1]*s
             */
            // init members
            double Pvcal = 0.0;
            int bw = TTypeEdgeComponent.Bw;
            int bf = TTypeEdgeComponent.Bf;
            int hc1 = TTypeEdgeComponent.Hc1;
            int hc2 = TTypeEdgeComponent.Hc2s+ TTypeEdgeComponent.Hc2l+ TTypeEdgeComponent.Bf;
            double c = TTypeEdgeComponent.C; // 保护层厚度
            // 直径，间距
            var stirrupDatas = ThReinforcementUtils.GetStirrupDatas(TTypeEdgeComponent.Stirrup);
            if (stirrupDatas.Count != 2)
            {
                return Pvcal;
            }
            int d1 = stirrupDatas[0]; // 箍筋直径
            int s = stirrupDatas[1]; //箍筋间距
            // 箍筋面积
            double asv1 = ThSteelDataManager.Instance.GetSteelArea(d1, DoubleTolerance);
            double a = c + d1 / 2.0;

            // 拉筋2的数据： 根数，直径，间距
            var link2Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link2);
            int n2 = 0; // 拉筋2肢数
            double asv2 = 0.0; // 拉筋2直径对应的截面面积
            if (link2Datas.Count == 3)
            {
                n2 = link2Datas[0];
                asv2 = ThSteelDataManager.Instance.GetSteelArea(link2Datas[1], DoubleTolerance);
            }

            // 拉筋3的数据： 根数，直径，间距
            var link3Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link3);
            int n3 = 0; // 拉筋3肢数
            double asv3 = 0.0; // 拉筋3直径对应的截面面积
            if (link3Datas.Count == 3)
            {
                n3 = link3Datas[0];
                asv3 = ThSteelDataManager.Instance.GetSteelArea(link3Datas[1], DoubleTolerance);
            }

            // 拉筋4的数据： 根数，直径，间距
            var link4Datas = ThReinforcementUtils.GetLinkDatas(TTypeEdgeComponent.Link4);
            int n4 = 0; // 拉筋4肢数
            double asv4 = 0.0; // 拉筋4直径对应的截面面积
            if (link4Datas.Count == 3)
            {
                n4 = link4Datas[0];
                asv4 = ThSteelDataManager.Instance.GetSteelArea(link4Datas[1], DoubleTolerance);
            }

            // Calculate
            var formula1 = asv1 * (4 * bw + bf + 2 * hc1 + 2 * hc2 - 14 * a);
            var formula2 = n2 * asv2 * (bf - 2 * a);
            var formula3 = n3 * asv3 * (bw - 2 * a);
            var formula4 = asv4 * (bw + hc1 + hc2 - 4 * a);
            var formula5 = bw - 2 * a - d1;
            var formula6 = hc2 - 2 * a - d1;
            var formula7 = (bf - 2 * a - d1) * hc1;
            var molecule = formula1 + formula2 + formula3 + formula4; // 分子
            var denominator = (formula5 * formula6 + formula7) * s;    // 分母
            if (denominator != 0.0)
            {
                Pvcal = molecule / denominator;
            }
            return Pvcal;
        }
    }
}
