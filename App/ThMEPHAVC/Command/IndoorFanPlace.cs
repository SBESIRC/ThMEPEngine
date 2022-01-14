using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Command;
using ThMEPHVAC.IndoorFanLayout;
using ThMEPHVAC.IndoorFanLayout.Business;
using ThMEPHVAC.IndoorFanLayout.Models;
using ThMEPHVAC.IndoorFanModels;
using ThMEPHVAC.ParameterService;

namespace ThMEPHVAC.Command
{
    /// <summary>
    /// 室内机放置模式
    /// </summary>
    class IndoorFanPlace: ThMEPBaseCommand
    {
        int ventCount = 1;
        double FirstVentDistanceToFan = 1000.0;
        double VentDistanceToPreVent = 2700.0;
        double ReturnSideDistanceToStartAdd = 100.0;
        double LastVentDistanceToEndAdd = 200;
        double MultipleValue = 50.0;
        public IndoorFanPlace() 
        {
            CommandName = "THSNJFZ";
            ActionName = "室内机放置";
            ventCount = IndoorFanParameter.Instance.PlaceModel.HisVentCount;
        }
        public override void SubExecute()
        {
            if (IndoorFanParameter.Instance.PlaceModel == null)
                return;
            using (Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                IndoorFanBlockServices.LoadBlockLayerToDocument(acdb.Database);
            }
            
            var fanData = IndoorFanParameter.Instance.PlaceModel.TargetFanInfo;
            FanLoadBase fanLoad = null;
            var fanType = IndoorFanParameter.Instance.PlaceModel.LayoutModel.FanType;
            double correctionFactor = IndoorFanParameter.Instance.PlaceModel.LayoutModel.CorrectionFactor;
            bool haveVent = false;
            
            switch (fanType) 
            {
                case EnumFanType.FanCoilUnitFourControls:
                case EnumFanType.FanCoilUnitTwoControls:
                    haveVent = true;
                    fanLoad = new CoilFanLoad(fanData, fanType, EnumHotColdType.Cold, correctionFactor);
                    break;
                case EnumFanType.IntegratedAirConditionin:
                    fanLoad = new AirConditionFanLoad(fanData, fanType, EnumHotColdType.Cold, correctionFactor);
                    break;
                case EnumFanType.VRFConditioninConduit:
                case EnumFanType.VRFConditioninFourSides:
                    haveVent = fanType == EnumFanType.VRFConditioninConduit;
                    fanLoad = new VRFImpellerFanLoad(fanData, fanType, EnumHotColdType.Cold, correctionFactor);
                    break;
            }
            bool canChange = true; ;
            if (haveVent) 
            {
                if (fanLoad.FanVentSizeCount > 1)
                {
                    if (!IndoorFanParameter.Instance.PlaceModel.LayoutModel.CreateBlastPipe)
                        canChange = false;
                    else
                        canChange = true;
                }
                else
                {
                    IndoorFanParameter.Instance.PlaceModel.HisVentCount = 1;
                    ventCount = 1;
                    canChange = false; 
                }
            }
            var yVecotr = Active.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Yaxis;
            var fanRectangleToBlock = new FanRectangleToBlock(new List<FanLoadBase> { fanLoad }, null, IndoorFanParameter.Instance.PlaceModel.LayoutModel);
            while (true)
            {
               
                //using放到while外部会有using未结束，ucs下显示文字重叠问题
                using (var acadDatabase = AcadDatabase.Active())
                {
                    var isTrue = SelectLayoutPoint(haveVent, canChange, out Point3d createPoint);
                    if (!isTrue)
                        break;
                    
                    var addFans =new List<FanLayoutDetailed>();
                    switch (fanType) 
                    {
                        case EnumFanType.FanCoilUnitFourControls:
                        case EnumFanType.FanCoilUnitTwoControls:
                            var addFan = CoilFanLayoutData(fanLoad, createPoint, yVecotr, ventCount);
                            addFans.Add(addFan);
                            break;
                        case EnumFanType.VRFConditioninConduit:
                            var addVrfFan = VRFFanLayoutData(fanLoad, createPoint, yVecotr, ventCount);
                            addFans.Add(addVrfFan);
                            break;
                        case EnumFanType.VRFConditioninFourSides:
                            var addVrfFourFan = VRFFourSideLayoutData(fanLoad, createPoint, yVecotr);
                            addFans.Add(addVrfFourFan);
                            break;
                        case EnumFanType.IntegratedAirConditionin:
                            var addAirFan = AirFanLayoutData(fanLoad, createPoint, yVecotr);
                            addFans.Add(addAirFan);
                            break;
                    }
                    fanRectangleToBlock.AddBlock(addFans, fanType);
                }
            }
            IndoorFanParameter.Instance.PlaceModel.HisVentCount =ventCount;
        }
        bool SelectLayoutPoint(bool haveVentCount,bool canChangeCount,out Point3d createPoint) 
        {
            createPoint = new Point3d();
            string showMsg = haveVentCount? string.Format("\n点击放置设备,当前送风口{0}个", ventCount): "\n点击进行放置风机";
            var opt = new PromptPointOptions(showMsg);
            if(haveVentCount && canChangeCount)
                opt.Keywords.Add("C", "C", "设置个数(C)");
            opt.AppendKeywordsToMessage = true;

            var propmptResult = Active.Editor.GetPoint(opt);
            if (propmptResult.Status == PromptStatus.Keyword)
            {
                if (propmptResult.StringResult.ToUpper() == "C")
                {
                    if (!haveVentCount || !canChangeCount)
                        return false;
                    //输入出图比例
                    var options = new PromptKeywordOptions("选择风口个数");
                    options.Keywords.Add("1", "1", "1个风口(1)");
                    options.Keywords.Add("2", "2", "2个风口(2)");
                    options.Keywords.Default = ventCount.ToString();
                    var result = Active.Editor.GetKeywords(options);
                    if (result.Status == PromptStatus.OK)
                    {
                        int.TryParse(result.StringResult, out ventCount);
                    }
                }
                bool isTrue = SelectLayoutPoint(haveVentCount, canChangeCount, out createPoint);
                return isTrue;
            }
            if (propmptResult.Status != PromptStatus.OK)
                return false;
            createPoint = propmptResult.Value.TransformBy(Active.Editor.UCS2WCS());
            return true;
        }
        FanLayoutDetailed CoilFanLayoutData(FanLoadBase fanLoad,Point3d fanPoint, Vector3d fanDir,int ventCount)
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength+ MultipleValue + fanLoad.ReturnAirSizeLength / 2;
            if (IndoorFanParameter.Instance.PlaceModel.LayoutModel.AirReturnType == EnumAirReturnType.AirReturnPipe)
                returnVentCenterDisTonFan += IndoorFanCommon.ReducingLength -100;
            var col = (int)Math.Floor(returnVentCenterDisTonFan / MultipleValue);
            var remainder = returnVentCenterDisTonFan % MultipleValue;
            returnVentCenterDisTonFan = (remainder * 2) > MultipleValue ? (col + 1) * MultipleValue : col * MultipleValue;
            var sp = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan + fanLoad.ReturnAirSizeLength / 2 + ReturnSideDistanceToStartAdd);
            var ep = fanPoint;
            var ventPoints = new List<Point3d>();
            for (int i = 0; i < ventCount; i++)
            {
                var ventPoint = ep + fanDir.MultiplyBy(FirstVentDistanceToFan) + fanDir.MultiplyBy((i) * VentDistanceToPreVent);
                ventPoints.Add(ventPoint);
            }
            if (ventPoints.Count > 0) 
            {
                var ventWidth = fanLoad.GetCoilFanVentSize(ventCount,out double ventLength);
                ep = ventPoints.Last() + fanDir.MultiplyBy(ventWidth/2+ LastVentDistanceToEndAdd);
            }
                
            var fanLayout = new FanLayoutDetailed(sp,ep, fanLoad.FanWidth,fanDir);
            fanLayout.FanPoint = fanPoint;
            fanLayout.FanLayoutName = fanLoad.FanNumber;
            fanLayout.FanInnerVents.AddRange(ventPoints);
            fanLayout.HaveReturnVent = true;
            fanLayout.FanReturnVentCenterPoint = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan);
            return fanLayout;
        }
        FanLayoutDetailed VRFFanLayoutData(FanLoadBase fanLoad, Point3d fanPoint, Vector3d fanDir, int ventCount) 
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength + MultipleValue + fanLoad.ReturnAirSizeLength / 2;
            var col = (int)Math.Floor(returnVentCenterDisTonFan / MultipleValue);
            var remainder = returnVentCenterDisTonFan % MultipleValue;
            returnVentCenterDisTonFan = (remainder * 2) > MultipleValue ? (col + 1) * MultipleValue : col * MultipleValue;

            var sp = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan + fanLoad.ReturnAirSizeLength / 2 + ReturnSideDistanceToStartAdd);
            var ep = fanPoint;
            var ventPoints = new List<Point3d>();
            for (int i = 0; i < ventCount; i++)
            {
                var ventPoint = ep + fanDir.MultiplyBy(FirstVentDistanceToFan) + fanDir.MultiplyBy((i) * VentDistanceToPreVent);
                ventPoints.Add(ventPoint);
            }
            if (ventPoints.Count > 0) 
            {
                var ventWidth = fanLoad.GetCoilFanVentSize(ventCount, out double ventLength);
                ep = ventPoints.Last() + fanDir.MultiplyBy(ventWidth / 2 + LastVentDistanceToEndAdd);
            }
            var fanLayout = new FanLayoutDetailed(sp, ep, fanLoad.FanWidth, fanDir);
            fanLayout.FanPoint = fanPoint;
            fanLayout.FanLayoutName = fanLoad.FanNumber;
            fanLayout.FanInnerVents.AddRange(ventPoints);
            fanLayout.HaveReturnVent = true;
            fanLayout.FanReturnVentCenterPoint = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan);
            return fanLayout;
        }
        FanLayoutDetailed VRFFourSideLayoutData(FanLoadBase fanLoad, Point3d fanPoint, Vector3d fanDir)
        {
            var posion = fanPoint;
            var fanLayout = new FanLayoutDetailed(posion, posion, fanLoad.FanWidth, fanDir);
            fanLayout.FanLayoutName = fanLoad.FanNumber;
            fanLayout.HaveReturnVent = false;
            fanLayout.FanPoint = posion;
            return fanLayout;
        }
        FanLayoutDetailed AirFanLayoutData(FanLoadBase fanLoad, Point3d fanPoint, Vector3d fanDir)
        {
            var returnVentCenterDisTonFan = fanLoad.FanLength + MultipleValue+50.0 + fanLoad.ReturnAirSizeLength / 2;
            if (IndoorFanParameter.Instance.PlaceModel.LayoutModel.AirReturnType == EnumAirReturnType.AirReturnPipe)
                returnVentCenterDisTonFan += IndoorFanCommon.ReducingLength;
            var col = (int)Math.Floor(returnVentCenterDisTonFan / MultipleValue);
            var remainder = returnVentCenterDisTonFan % MultipleValue;
            returnVentCenterDisTonFan = (remainder * 2) > MultipleValue ? (col + 1) * MultipleValue : col * MultipleValue;
            var posion = fanPoint;
            var sp = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan + fanLoad.ReturnAirSizeLength / 2 + ReturnSideDistanceToStartAdd);
            var ep = fanPoint;
            var ventPoints = new List<Point3d>();
            for (int i = 0; i < ventCount; i++)
            {
                var ventPoint = ep + fanDir.MultiplyBy(FirstVentDistanceToFan) + fanDir.MultiplyBy((i) * VentDistanceToPreVent);
                ventPoints.Add(ventPoint);
            }
            if (ventPoints.Count > 0)
            {
                var ventWidth = fanLoad.GetCoilFanVentSize(ventCount, out double ventLength);
                ep = ventPoints.Last() + fanDir.MultiplyBy(ventWidth / 2 + LastVentDistanceToEndAdd);
            }
            var fanLayout = new FanLayoutDetailed(sp, ep, fanLoad.FanWidth, fanDir);
            fanLayout.FanLayoutName = fanLoad.FanNumber;
            fanLayout.HaveReturnVent = true;
            fanLayout.FanPoint = posion;
            fanLayout.FanReturnVentCenterPoint = fanPoint - fanDir.MultiplyBy(returnVentCenterDisTonFan);
            return fanLayout;
        }
    }
}
