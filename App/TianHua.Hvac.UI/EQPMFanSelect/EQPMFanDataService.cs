using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThMEPEngineCore.IO.JSON;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    class EQPMFanDataService
    {
        public static EQPMFanDataService Instance = new EQPMFanDataService();
        EQPMFanDataService()
        {
            InitFanData();
        }
        private string MOTOR_POWER = "电机功率.json";
        private string MOTOR_POWER_Double = "电机功率-双速.json";
        private string HTFC_Selection = "离心风机选型.json";
        private string HTFC_Parameters = "离心-前倾-单速.json";
        private string HTFC_Parameters_Double = "离心-前倾-双速.json";
        private string HTFC_Parameters_Single = "离心-后倾-单速.json";
        private string AXIAL_Selection = "轴流风机选型.json";
        private string AXIAL_Parameters = "轴流-单速.json";
        private string AXIAL_Parameters_Double = "轴流-双速.json";
        private string HTFC_Efficiency = "离心风机效率.json";
        private string AXIAL_Efficiency = "轴流风机效率.json";

        /// <summary>
        /// 离心风机信息
        /// </summary>
        public List<FanSelectionData> CentrifugalFanSelections { get; private set; }
        /// <summary>
        /// 轴流风机信息
        /// </summary>
        public List<AxialFanEfficiency> AxialFanSelections { get; private set; }
        /// <summary>
        /// 离心-前倾-单速
        /// </summary>
        public List<FanParameters> FanParametersForwardS { get; private set; }
        /// <summary>
        /// 离心-前倾-双速
        /// </summary>
        public List<FanParameters> FanParametersForwardD { get; private set; }
        /// <summary>
        /// 离心-后倾-单速
        /// </summary>
        public List<FanParameters> FanParametersBackwardS { get; private set; }
        /// <summary>
        /// 轴流-单速
        /// </summary>
        public List<AxialFanParameters> FanParametersAxialS { get; private set; }
        /// <summary>
        /// /轴流-双速
        /// </summary>
        public List<AxialFanParameters> FanParametersAxialD { get; private set; }
        /// <summary>
        /// 离心机能耗信息
        /// </summary>
        public List<FanEfficiency> FanEfficiencies { get; private set; }
        /// <summary>
        /// 轴流风机能耗信息
        /// </summary>
        public List<AxialFanEfficiency> AxialFanEfficiencies { get; private set; }
        /// <summary>
        /// 电机功率 - 双速
        /// </summary>
        public List<MotorPower> MotorPowersD { get; private set; }
        /// <summary>
        /// 电机功率 - 单速
        /// </summary>
        public List<MotorPower> MotorPowersS { get; private set; }
        public void InitFanData() 
        {
            //离心风机信息
            var fanSelStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), HTFC_Selection));
            CentrifugalFanSelections = JsonHelper.DeserializeJsonToList<FanSelectionData>(fanSelStr);
            //轴流风机信息
            var axialFanSelStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), AXIAL_Selection));
            AxialFanSelections = JsonHelper.DeserializeJsonToList<AxialFanEfficiency>(axialFanSelStr);
            //离心-前倾-单速
            var fanParamFSStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), HTFC_Parameters));
            FanParametersForwardS = JsonHelper.DeserializeJsonToList<FanParameters>(fanParamFSStr);
            //离心-前倾-双速
            var _fanParamFDStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), HTFC_Parameters_Double));
            FanParametersForwardD = JsonHelper.DeserializeJsonToList<FanParameters>(_fanParamFDStr);
            //离心-后倾-单速
            var fanParamBSStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), HTFC_Parameters_Single));
            FanParametersBackwardS = JsonHelper.DeserializeJsonToList<FanParameters>(fanParamBSStr);
            //轴流-单速
            var axialFParamSStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), AXIAL_Parameters));
            FanParametersAxialS = JsonHelper.DeserializeJsonToList<AxialFanParameters>(axialFParamSStr);
            //轴流-双速
            var axialFParamDStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), AXIAL_Parameters_Double));
            FanParametersAxialD = JsonHelper.DeserializeJsonToList<AxialFanParameters>(axialFParamDStr);

            //电机效率
            var fanEfficiencyStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), HTFC_Efficiency));
            FanEfficiencies = JsonHelper.DeserializeJsonToList<FanEfficiency>(fanEfficiencyStr);

            //轴流电机效率
            var axialFanEfficiencyStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), AXIAL_Efficiency));
            AxialFanEfficiencies = JsonHelper.DeserializeJsonToList<AxialFanEfficiency>(axialFanEfficiencyStr);
            //电机功率 - 单速
            var motorPowerStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), MOTOR_POWER));
            MotorPowersS = JsonHelper.DeserializeJsonToList<MotorPower>(motorPowerStr);
            //电机效率 - 双速
            var motorPowerDoubleStr = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), MOTOR_POWER_Double));
            MotorPowersD = JsonHelper.DeserializeJsonToList<MotorPower>(motorPowerDoubleStr);

            FanEfficiencies.ForEach(p =>
            {
                if (string.IsNullOrEmpty(p.No_Max)) p.No_Max = "9999";
                if (string.IsNullOrEmpty(p.No_Min)) p.No_Min = "0";
                if (string.IsNullOrEmpty(p.Rpm_Max)) p.Rpm_Max = "9999";
                if (string.IsNullOrEmpty(p.Rpm_Min)) p.Rpm_Min = "0";
            });
            AxialFanEfficiencies.ForEach(p =>
            {
                if (string.IsNullOrEmpty(p.No_Max)) p.No_Max = "9999";
                if (string.IsNullOrEmpty(p.No_Min)) p.No_Min = "0";
            });
        }
        public void ClearData() 
        {
            CentrifugalFanSelections.Clear();
            AxialFanSelections.Clear();
            FanParametersForwardS.Clear();
            FanParametersForwardD.Clear();
            FanParametersBackwardS.Clear();
            FanParametersAxialS.Clear();
            FanParametersAxialD.Clear();

            FanEfficiencies.Clear();
            AxialFanEfficiencies.Clear();
            MotorPowersS.Clear();
            MotorPowersD.Clear();
        }
        private string ReadTxt(string filePath)
        {
            StreamReader streamReader = null;
            try
            {
                using (streamReader = File.OpenText(filePath))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (null != streamReader)
                    streamReader.Dispose();
            }
        }

        public List<FanModelPicker> GetFanCanUseFanModels(FanDataModel fanData, List<double> point, string CCFName)
        {
            switch (fanData.VentStyle)
            {
                case EnumFanModelType.AxialFlow:
                    return GetAxialFanModelPickers(fanData, point, CCFName);
                default:
                    return GetFanModelPickers(fanData, point, CCFName);
            }
        }
        public FanParameters GetFanParameters(EnumFanControl controlType, EnumFanModelType modelType, FanModelPicker modelPicker)
        {
            FanParameters retParameter = null;
            var targetParams = GetTargetFanParameters(controlType, modelType);
            retParameter = targetParams.Where(c => modelPicker.Model == c.CCCF_Spec
                                && Convert.ToDouble(c.AirVolume) == modelPicker.AirVolume
                                && Convert.ToDouble(c.Pa) == modelPicker.Pa).FirstOrDefault();
            return retParameter;
        }
        public AxialFanParameters GetAxialFanParameters(EnumFanControl controlType, FanModelPicker modelPicker)
        {
            AxialFanParameters retParameter = null;
            if (controlType == EnumFanControl.TwoSpeed)
            {
                retParameter = FanParametersAxialD.Where(c =>
                                                         c.ModelNum == modelPicker.Model
                                                         && Convert.ToDouble(c.AirVolume) == modelPicker.AirVolume
                                                         && Convert.ToDouble(c.Pa) == modelPicker.Pa
                                                        ).FirstOrDefault();
            }
            else
            {
                retParameter = FanParametersAxialS.Where(c =>
                                                         c.ModelNum == modelPicker.Model
                                                         && Convert.ToDouble(c.AirVolume) == modelPicker.AirVolume
                                                         && Convert.ToDouble(c.Pa) == modelPicker.Pa
                                                        ).FirstOrDefault();
            }
            return retParameter;
        }
        public List<AxialFanParameters> GetAxialFanParameters(EnumFanControl controlType)
        {
            var retParameters = new List<AxialFanParameters>();
            if (controlType == EnumFanControl.TwoSpeed)
            {
                retParameters.AddRange(FanParametersAxialD);
            }
            else
            {
                retParameters.AddRange(FanParametersAxialS);
            }
            return retParameters;
        }
        public List<FanModelPicker> GetFanModelPickers(FanDataModel fanData, List<double> point, string CCFName)
        {
            IEqualityComparer<FanParameters> comparer = null;
            bool isBack = EQPMFanCommon.IsHTFCBackwardModelStyle(fanData.VentStyle);
            if (isBack)
            {
                comparer = new CCCFRpmComparer();
            }
            else
            {
                comparer = new CCCFComparer();
            }
            var models = new List<FanParameters>();
            switch (fanData.Control)
            {
                case EnumFanControl.TwoSpeed:
                    models.AddRange(FanParametersForwardD);
                    break;
                default:
                    if (isBack)
                        models.AddRange(FanParametersBackwardS);
                    else
                        models.AddRange(FanParametersForwardS);
                    break;
            }
            var coordinate = new Coordinate(
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[0]),
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[1])
                );
            if (!string.IsNullOrEmpty(CCFName))
                models = models.Where(c => c.CCCF_Spec == CCFName).ToList();
            var realPoint = ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(coordinate);
            var resModels = new Dictionary<Geometry, Point>();
            //当前项为高速档，需过滤掉gear档位为低的元素，保留高档位元素，反之过滤掉gear档位为高的元素
            if (fanData.IsChildFan)
            {
                resModels = models.ToGeometries(comparer, "高").ModelPick(realPoint);
            }
            else
            {
                resModels = models.ToGeometries(comparer, "低").ModelPick(realPoint);
            }
            var retModels = new List<FanModelPicker>();
            foreach (var item in resModels)
            {
                var addPicker = new FanModelPicker();
                string moelkeyname = item.Key.UserData as string;
                addPicker.Model = moelkeyname.Split('@')[0];
                addPicker.Pa = item.Value.Y;
                addPicker.AirVolume = item.Value.X;
                addPicker.IsOptimalModel = item.Key.IsOptimalModel(realPoint);
                addPicker.ModelGeometry = item.Key;
                retModels.Add(addPicker);
            }
            return retModels;
        }
        public List<FanModelPicker> GetAxialFanModelPickers(FanDataModel fanData, List<double> point, string CCFName)
        {
            IEqualityComparer<AxialFanParameters> comparer = new AxialModelNumberComparer();
            var coordinate = new Coordinate(
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[0]),
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[1])
                );
            var realPoint = ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(coordinate);
            var models = new List<AxialFanParameters>();
            switch (fanData.Control)
            {
                case EnumFanControl.TwoSpeed:
                    models.AddRange(FanParametersAxialD);
                    break;
                default:
                    models.AddRange(FanParametersAxialS);
                    break;
            }
            if (!string.IsNullOrEmpty(CCFName))
                models = models.Where(c => c.ModelNum == CCFName).ToList();
            var resModels = new Dictionary<Geometry, Point>();
            //当前项为高速档，需过滤掉gear档位为低的元素，保留高档位元素，反之过滤掉gear档位为高的元素
            if (fanData.IsChildFan)
            {
                resModels = models.ToGeometries(comparer, "高").ModelPick(realPoint);
            }
            else
            {
                resModels = models.ToGeometries(comparer, "低").ModelPick(realPoint);
            }
            var retModels = new List<FanModelPicker>();
            foreach (var item in resModels)
            {
                var addPicker = new FanModelPicker();
                string moelkeyname = item.Key.UserData as string;
                addPicker.Model = moelkeyname;//.Split('@')[0];
                addPicker.Pa = item.Value.Y;
                addPicker.AirVolume = item.Value.X;
                addPicker.IsOptimalModel = item.Key.IsOptimalModel(realPoint);
                addPicker.ModelGeometry = item.Key;
                retModels.Add(addPicker);
            }
            return retModels;
        }
        public List<FanModelPicker> GetFanModelPickers(FanDataModel fanData, FanModelPicker basePicker)
        {
            IEqualityComparer<FanParameters> comparer = null;
            bool isBack = EQPMFanCommon.IsHTFCBackwardModelStyle(fanData.VentStyle);
            if (isBack)
            {
                comparer = new CCCFRpmComparer();
            }
            else
            {
                comparer = new CCCFComparer();
            }
            var models = new List<FanParameters>();
            switch (fanData.Control)
            {
                case EnumFanControl.TwoSpeed:
                    models.AddRange(FanParametersForwardD);
                    break;
                default:
                    if (isBack)
                        models.AddRange(FanParametersBackwardS);
                    else
                        models.AddRange(FanParametersForwardS);
                    break;
            }
            var retModels = new List<FanModelPicker>();
            var strName = basePicker.Model;
            var startWith = strName.Substring(0, strName.LastIndexOf('-') + 1);
            
            foreach (var item in models)
            {
                if (string.IsNullOrEmpty(item.CCCF_Spec))
                    continue;
                if (item.CCCF_Spec == strName)
                {
                    if (!retModels.Any(c => c.Model == item.CCCF_Spec))
                        retModels.Add(basePicker);
                }
                else if (item.CCCF_Spec.StartsWith(startWith))
                {
                    if (!retModels.Any(c => c.Model == item.CCCF_Spec))
                    {
                        var addPicker = new FanModelPicker();
                        string moelkeyname = item.CCCF_Spec as string;
                        addPicker.Model = moelkeyname.Split('@')[0];
                        double.TryParse(item.Pa, out double pa);
                        addPicker.Pa = pa;
                        double.TryParse(item.AirVolume, out double airVolume);
                        addPicker.AirVolume = airVolume; ;
                        retModels.Add(addPicker);
                    }
                }
            }
            return retModels;
        }
        public List<FanModelPicker> GetAxialFanModelPickers(FanDataModel fanData, FanModelPicker basePicker)
        {
            var models = new List<AxialFanParameters>();
            switch (fanData.Control)
            {
                case EnumFanControl.TwoSpeed:
                    models.AddRange(FanParametersAxialD);
                    break;
                default:
                    models.AddRange(FanParametersAxialS);
                    break;
            }
            var strName = basePicker.Model;
            var startWith = strName.Substring(0, strName.LastIndexOf('-')+1);
            var retModels = new List<FanModelPicker>();
            foreach (var item in models)
            {
                if (string.IsNullOrEmpty(item.ModelNum))
                    continue;
                if (item.ModelNum == strName)
                {
                    if (!retModels.Any(c => c.Model == item.ModelNum))
                        retModels.Add(basePicker);
                }
                else if (item.ModelNum.StartsWith(startWith)) 
                {
                    if (!retModels.Any(c => c.Model == item.ModelNum))
                    {
                        var addPicker = new FanModelPicker();
                        addPicker.Model = item.ModelNum;
                        double.TryParse(item.Pa, out double pa);
                        addPicker.Pa = pa;
                        double.TryParse(item.AirVolume, out double airVolume);
                        addPicker.AirVolume = airVolume;;
                        retModels.Add(addPicker);
                    }
                }
            }
            return retModels;
        }
        public List<FanParameters> GetTargetFanParameters(EnumFanControl controlType, EnumFanModelType modelType) 
        {
            var retParameters = new List<FanParameters>();
            if (controlType == EnumFanControl.TwoSpeed)
            {
                retParameters.AddRange(FanParametersForwardD);
            }
            else
            {
                if (modelType == EnumFanModelType.BackwardTiltCentrifugation_Inner || modelType == EnumFanModelType.BackwardTiltCentrifugation_Out)
                {
                    retParameters.AddRange(FanParametersBackwardS);
                }
                else
                {
                    retParameters.AddRange(FanParametersForwardS);
                }
            }
            return retParameters;
        }

        public void CalcFanEfficiency(CalcFanModel calcFanModel, FanDataModel mainFanModel, FanDataModel childFanModel)
        {
            if (null != childFanModel && childFanModel.PID == mainFanModel.ID)
                calcFanModel.HaveChildFan = true;
            else
                calcFanModel.HaveChildFan = false;
            var fanControl = mainFanModel.Control;
            var ventStyle = mainFanModel.VentStyle;
            if (string.IsNullOrEmpty(calcFanModel.FanModelName))
                return;
            //比转速	等于5.54*风机转速（查询）*比转数下的流量的0.5次方 /全压输入值的0.75次方		
            //轴功率    风量乘以全压除以风机内效率除以传动效率（0.855）除以1000					
            //电机容量安全系数 =IF(AZ6<=0.5,1.5, IF(AZ6<=1,1.4,IF(AZ6<=2,1.3,IF(AZ6<=5,1.2,IF(AZ6<=20,1.15,1.1)))))
            var listMotors = GetListMotorPower(fanControl, ventStyle, calcFanModel.FanModelName);
            var fanAirVolume = Convert.ToDouble(calcFanModel.FanAirVolume);
            var fanWindResis = Convert.ToDouble(mainFanModel.WindResis);
            //var fanModelSpeed = Convert.ToDouble(calcFanModel.FanModelFanSpeed);
            double.TryParse(calcFanModel.FanModelFanSpeed, out double fanModelSpeed);
            var fanEleLev = CommonUtil.GetEnumDescription(mainFanModel.EleLev);
            var fanVentLev = CommonUtil.GetEnumDescription(mainFanModel.VentLev);
            var fanMotorTempo = calcFanModel.MotorTempo.ToString();
            int.TryParse(calcFanModel.FanModelNum, out int fanNum);
            if (ventStyle == EnumFanModelType.AxialFlow)
            {
                //轴流风机
                double safetyFactor = 0;
                double heightFlow = Math.Round(fanAirVolume / 3600, 5);
                var specificSpeed = 5.54 * fanModelSpeed * Math.Pow(heightFlow, 0.5) / Math.Pow(fanWindResis, 0.75);
                var noSplit = calcFanModel.FanModelName.Split('-');
                double no = 0;
                if (noSplit.Count() == 3)
                {
                    var str = noSplit[2];
                    str = Regex.Replace(str, @"[^\d.\d]", "");
                    double.TryParse(str, out no);
                }
                var axialFanEfficiency = AxialFanEfficiencies.Where(p =>
                {
                    if (fanVentLev != p.FanEfficiencyLevel)
                        return false;
                    int.TryParse(p.No_Min, out int minNo);
                    int.TryParse(p.No_Max, out int maxNo);
                    if (minNo <= fanNum && maxNo >= fanNum && fanVentLev == p.FanEfficiencyLevel)
                        return true;
                    return false;
                }).First();
                if (axialFanEfficiency == null)
                    return;
                var _FanEfficiency = Convert.ToInt32(axialFanEfficiency.FanEfficiency * 0.9);
                var shaftPower = fanAirVolume * fanWindResis / _FanEfficiency * 100 / 0.855 / 1000 / 3600;

                if (shaftPower <= 0.5)
                {
                    safetyFactor = 1.5;
                }
                else if (shaftPower <= 1)
                {
                    safetyFactor = 1.4;
                }
                else if (shaftPower <= 2)
                {
                    safetyFactor = 1.3;
                }
                else if (shaftPower <= 5)
                {
                    safetyFactor = 1.2;
                }
                else if (shaftPower <= 20)
                {
                    safetyFactor = 1.15;
                }
                else
                {
                    safetyFactor = 1.1;
                }
                var motorEfficiency = EQPMFanCommon.ListMotorEfficiency.Where(p => p.Key == "直连").First();
                var tmp = shaftPower / 0.85;


                var listMotorPower = listMotors.FindAll(p => Convert.ToDouble(p.Power) >= tmp && p.MotorEfficiencyLevel == fanEleLev && p.Rpm == fanMotorTempo);
                var motorPower = listMotorPower.OrderBy(p => Convert.ToDouble(p.Power)).First();

                var estimatedMotorPower = shaftPower / Convert.ToDouble(motorPower.MotorEfficiency) / Convert.ToDouble(motorEfficiency.Value) * safetyFactor * 100;
                listMotorPower = listMotors.FindAll(p => Convert.ToDouble(p.Power) >= estimatedMotorPower && p.MotorEfficiencyLevel == fanEleLev && p.Rpm == fanMotorTempo);
                motorPower = listMotorPower.OrderBy(p => Convert.ToDouble(p.Power)).FirstOrDefault();

                if (motorPower != null)
                {
                    calcFanModel.FanModelMotorPower = motorPower.Power;
                    calcFanModel.FanInternalEfficiency = axialFanEfficiency.FanEfficiency.ToString();
                }
                GetPower(calcFanModel, mainFanModel, childFanModel, axialFanEfficiency);
            }
            else
            {
                //离心
                double safetyFactor = 0;
                double flow = Math.Round(fanAirVolume / 3600, 5);
                var specificSpeed = (int)(5.54 * fanModelSpeed * Math.Pow(flow, 0.5) / Math.Pow(fanWindResis, 0.75));
                specificSpeed = specificSpeed < 0 ? 0 : specificSpeed;

                var fanEfficiency = FanEfficiencies.Find(p =>
                {
                    int.TryParse(p.No_Min, out int minNo);
                    
                    int.TryParse(p.No_Max, out int maxNo);
                    int.TryParse(p.Rpm_Min, out int minRpm);
                    int.TryParse(p.Rpm_Max, out int maxRpm);
                    if (minNo <= fanNum && maxNo >= fanNum && minRpm <= specificSpeed && maxRpm >= specificSpeed && fanVentLev == p.FanEfficiencyLevel)
                        return true;
                    return false;

                });
                if (fanEfficiency == null)
                    return;
                var fanInternalEfficiency = Convert.ToInt32(fanEfficiency.FanInternalEfficiency * 0.9);
                var shaftPower = fanAirVolume * fanWindResis / fanInternalEfficiency * 100 / 0.855 / 1000 / 3600;
                if (shaftPower <= 0.5)
                {
                    safetyFactor = 1.5;
                }
                else if (shaftPower <= 1)
                {
                    safetyFactor = 1.4;
                }
                else if (shaftPower <= 2)
                {
                    safetyFactor = 1.3;
                }
                else if (shaftPower <= 5)
                {
                    safetyFactor = 1.2;
                }
                else if (shaftPower <= 20)
                {
                    safetyFactor = 1.15;
                }
                else
                {
                    safetyFactor = 1.1;
                }
                var motorEfficiency = EQPMFanCommon.ListMotorEfficiency.Where(p => p.Key == "皮带").First();
                var tmp = shaftPower / 0.85;
                var listMotorPower = listMotors.FindAll(p => Convert.ToDouble(p.Power) >= tmp && p.MotorEfficiencyLevel == fanEleLev && p.Rpm == fanMotorTempo);
                var motorPower = listMotorPower.OrderBy(p => Convert.ToDouble(p.Power)).First();

                var _EstimatedMotorPower = shaftPower / Convert.ToDouble(motorPower.MotorEfficiency) * 100 / Convert.ToDouble(motorEfficiency.Value) * safetyFactor;
                listMotorPower = listMotors.FindAll(p => Convert.ToDouble(p.Power) >= _EstimatedMotorPower && p.MotorEfficiencyLevel == fanEleLev && p.Rpm == fanMotorTempo);
                motorPower = listMotorPower.OrderBy(p => Convert.ToDouble(p.Power)).First();

                if (motorPower != null)
                {
                    calcFanModel.FanModelMotorPower = motorPower.Power;
                    calcFanModel.FanInternalEfficiency = fanEfficiency.FanInternalEfficiency.ToString();
                }
                GetPower(calcFanModel, mainFanModel, childFanModel, fanEfficiency);

            }


        }
        private List<MotorPower> GetListMotorPower(EnumFanControl fanControl, EnumFanModelType ventStyle, string fanModelName)
        {
            if (fanControl == EnumFanControl.TwoSpeed)
            {
                if (string.IsNullOrEmpty(fanModelName))
                    return MotorPowersD;
                if (ventStyle == EnumFanModelType.AxialFlow)
                {
                    if (fanModelName.Contains("II"))
                        return MotorPowersD.Where(p => p.Axial2HighSpeed == "1").ToList();
                    if (fanModelName.Contains("IV"))
                        return MotorPowersD.Where(p => p.Axial4HighSpeed == "1").ToList();
                }
                else
                {
                    if (fanModelName.Contains("II"))
                        return MotorPowersD.FindAll(p => p.Centrifuge2HighSpeed == "1");
                    if (fanModelName.Contains("IV"))
                        return MotorPowersD.FindAll(p => p.Centrifuge4HighSpeed == "1");
                }
                return MotorPowersD;
            }
            else
            {
                return MotorPowersS;
            }
        }
        private void GetPower(CalcFanModel calcFanModel, FanDataModel mainFanModel, FanDataModel childFanModel, AxialFanEfficiency axialFanEfficiency)
        {
            double fanLowWindowResis = 0.0;
            double.TryParse(calcFanModel.FanModelPa, out double fanWindResis);
            if(null != childFanModel)
                double.TryParse(childFanModel.WindResis.ToString(), out fanLowWindowResis);
            var scenario = mainFanModel.Scenario;
            if (scenario == EnumScenario.FireAirSupplement || scenario == EnumScenario.FireSmokeExhaust
                || scenario == EnumScenario.FirePressurizedAirSupply
                || scenario == EnumScenario.KitchenFumeExhaust
                || scenario == EnumScenario.EmergencyExhaust
                || scenario == EnumScenario.AccidentAirSupplement)
            {
                calcFanModel.FanModelPower = "-";
                calcFanModel.FanModelPower = "-";
                return;
            }
            
            if (scenario == EnumScenario.KitchenFumeExhaustAndAirSupplement)
            {
                var power = fanWindResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = power.ToString("0.##");
                if (calcFanModel.HaveChildFan)
                    calcFanModel.FanModelPower = power.ToString("0.##");
                return;
            }
            if (scenario == EnumScenario.NormalAirSupply || scenario == EnumScenario.NormalExhaust)
            {
                //有低速、也可以没有
                var fanModelPower = fanWindResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = fanModelPower.ToString("0.##");
                if (calcFanModel.HaveChildFan)
                {
                    var lowPower = fanLowWindowResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                    calcFanModel.FanModelPower = lowPower.ToString("0.##");
                }
                return;
            }
            if (scenario == EnumScenario.FireAirSupplementAndNormalAirSupply || scenario == EnumScenario.FireSmokeExhaustAndNormalExhaust)
            {
                calcFanModel.FanModelPower = "-";
                var lowPower = fanLowWindowResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = lowPower.ToString("0.##");
                return;
            }
            if (scenario == EnumScenario.NormalExhaustAndAccidentExhaust || scenario == EnumScenario.NormalAirSupplyAndAccidentAirSupplement)
            {
                var fanModelPower = fanWindResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = fanModelPower.ToString("0.##");
                if (calcFanModel.HaveChildFan)
                {
                    var lowPower = fanLowWindowResis / (3600 * axialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                    calcFanModel.FanModelPower = "-";
                    calcFanModel.FanModelPower = lowPower.ToString("0.##");
                    //if (_FanDataModel.Use == "平时排风")
                    //{
                    //    //_FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");
                    //    _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");
                    //}
                    //else
                    //{
                    //    //_FanDataModel.FanModelPower = FuncStr.NullToDouble(_SonPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");
                    //    _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");
                    //}
                }
                return;

            }


        }

        private void GetPower(CalcFanModel calcFanModel, FanDataModel mainFanModel, FanDataModel childFanModel, FanEfficiency fanEfficiency)
        {
            double fanLowWindowResis = 0.0;
            double.TryParse(calcFanModel.FanModelPa, out double fanWindResis);
            if(null != childFanModel)
                double.TryParse(childFanModel.WindResis.ToString(), out fanLowWindowResis);
            var scenario = mainFanModel.Scenario;
            if (scenario == EnumScenario.FireAirSupplement
                || scenario == EnumScenario.FireSmokeExhaust
                || scenario == EnumScenario.FirePressurizedAirSupply
                || scenario == EnumScenario.KitchenFumeExhaust
                || scenario == EnumScenario.AccidentAirSupplement
                || scenario == EnumScenario.EmergencyExhaust)
            {
                calcFanModel.FanModelPower = "-";
                calcFanModel.FanModelPower = "-";
                return;
            }
            var fanVentLev = CommonUtil.GetEnumDescription(mainFanModel.VentLev);
            if (scenario == EnumScenario.KitchenFumeExhaustAndAirSupplement)
            {
                var fanModelPower = fanWindResis / (3600 * fanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = fanModelPower.ToString("0.##");
                if (calcFanModel.HaveChildFan)
                {
                    var lowPower = fanLowWindowResis / (3600 * fanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                    calcFanModel.FanModelPower = lowPower.ToString("0.##");
                }
                return;
            }
            if (scenario == EnumScenario.NormalAirSupply || scenario == EnumScenario.NormalExhaust)
            {
                //有低速、也可以没有
                var fanModelPower = fanWindResis / (3600 * fanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                calcFanModel.FanModelPower = fanModelPower.ToString("0.##");
                if (calcFanModel.HaveChildFan)
                {
                    double.TryParse(calcFanModel.FanModelFanSpeed, out double fanSpeed);
                    var childFanAirVolume = (double)childFanModel.AirVolume;
                    var childWindResis = childFanModel.WindResis;
                    double flow = Math.Round(childFanAirVolume / 3600, 5);
                    var specificSpeed = 5.54 * fanSpeed * Math.Pow(flow, 0.5) / Math.Pow(childWindResis, 0.75);

                    var _SonEfficiency = FanEfficiencies.Find(p => Convert.ToDouble(p.No_Min) < Convert.ToDouble(calcFanModel.FanModelNum) 
                    && Convert.ToDouble(p.No_Max) > Convert.ToDouble(calcFanModel.FanModelNum)
                         && Convert.ToDouble(p.Rpm_Min) < Convert.ToDouble(specificSpeed)
                          && Convert.ToDouble(p.Rpm_Max) > Convert.ToDouble(specificSpeed)
                          && fanVentLev == p.FanEfficiencyLevel);
                    if (_SonEfficiency == null) { return; }


                    var childPower = childWindResis / (3600 * _SonEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                    calcFanModel.FanModelPower = childPower.ToString("0.##");
                }
                return;
            }
            if (scenario == EnumScenario.FireSmokeExhaustAndNormalExhaust || scenario == EnumScenario.FireAirSupplementAndNormalAirSupply)
            {
                calcFanModel.FanModelPower = "-";

                if (calcFanModel.HaveChildFan)
                {
                    double.TryParse(calcFanModel.FanModelFanSpeed, out double fanSpeed);
                    var childFanAirVolume = (double)childFanModel.AirVolume;
                    var childWindResis = childFanModel.WindResis;
                    double flow = Math.Round(childFanAirVolume / 3600, 5);
                    var specificSpeed = 5.54 * fanSpeed * Math.Pow(flow, 0.5) / Math.Pow(childWindResis, 0.75);
                    var childEfficiency = FanEfficiencies.Find(p => Convert.ToDouble(p.No_Min) <= Convert.ToDouble(calcFanModel.FanModelNum)
                    && Convert.ToDouble(p.No_Max) >= Convert.ToDouble(calcFanModel.FanModelNum)
                         && Convert.ToDouble(p.Rpm_Min) <= Convert.ToDouble(specificSpeed)
                          && Convert.ToDouble(p.Rpm_Max) >= Convert.ToDouble(specificSpeed) && fanVentLev == p.FanEfficiencyLevel);
                    if (childEfficiency == null)
                        return;
                    var childPower = childWindResis / (3600 * childEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                    calcFanModel.FanModelPower = childPower.ToString("0.##");
                    childFanModel.FanModelTypeCalcModel.FanModelPower = childPower.ToString("0.##");
                }
                return;
            }
            if (scenario == EnumScenario.NormalAirSupplyAndAccidentAirSupplement || scenario == EnumScenario.NormalExhaustAndAccidentExhaust)
            {
                var fanModelPower = fanWindResis / (3600 * fanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                calcFanModel.FanModelPower = fanModelPower.ToString("0.##");

                if (calcFanModel.HaveChildFan)
                {
                    double.TryParse(calcFanModel.FanModelFanSpeed, out double fanSpeed);
                    var childFanAirVolume = (double)childFanModel.AirVolume;
                    var childWindResis = childFanModel.WindResis;
                    double flow = Math.Round(childFanAirVolume / 3600, 5);
                    var specificSpeed = 5.54 * fanSpeed * Math.Pow(flow, 0.5) / Math.Pow(childWindResis, 0.75);

                    var childEfficiency = FanEfficiencies.Find(p => Convert.ToDouble(p.No_Min) < Convert.ToDouble(calcFanModel.FanModelNum) && Convert.ToInt32(p.No_Max) > Convert.ToInt32(calcFanModel.FanModelNum)
                         && Convert.ToDouble(p.Rpm_Min) < Convert.ToDouble(specificSpeed)
                          && Convert.ToDouble(p.Rpm_Max) > Convert.ToDouble(specificSpeed) && fanVentLev == p.FanEfficiencyLevel);
                    if (childEfficiency == null)
                    {
                        calcFanModel.FanModelPower = string.Empty;
                        return;
                    }

                    var childPower = childWindResis / (3600 * childEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                    calcFanModel.FanModelPower = childPower.ToString("0.##");
                    //if (_FanDataModel.Use == "平时排风")
                    //{
                    //    _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");
                    //}
                    //else
                    //{
                    //    _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");
                    //}

                }
                return;

            }

        }
    }

    public class FanModelPicker
    {
        /// <summary>
        /// CCCF规格
        /// </summary>
        /// <returns></returns>
        public string Model { get; set; }
        /// <summary>
        /// 全压
        /// </summary>
        /// <returns></returns>
        public double Pa { get; set; }
        /// <summary>
        /// 风量
        /// </summary>
        /// <returns></returns>
        public double AirVolume { get; set; }
        /// <summary>
        /// 是否为最优
        /// </summary>
        /// <returns></returns>
        public bool IsOptimalModel { get; set; }
        public Geometry ModelGeometry { get; set; }
    }
}
