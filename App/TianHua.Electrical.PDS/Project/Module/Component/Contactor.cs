using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 接触器
    /// </summary>
    [Serializable]
    public class Contactor : PDSBaseComponent
    {
        /// <summary>
        /// 接触器
        /// </summary>
        /// <param name="contactorConfig"></param>
        public Contactor(string contactorConfig)
        {
            ComponentType = ComponentType.QAC;

            //CJ20-9/3P
            string[] configs = contactorConfig.Split('-');
            string[] detaileds = configs[1].Split('/');
            var model = configs[0];
            var polesNum =detaileds[1];
            var ratedCurrent = detaileds[0];

            var contactors = ContactorConfiguration.contactorInfos.Where(o => o.Poles == polesNum && o.Amps.ToString() == ratedCurrent && o.Model == model).Take(1).ToList();
            if (contactors.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的Contactor");
            }
            this.Contactors = contactors;
            var contactor = contactors.First();
            Model = contactor.Model;
            PolesNum = contactor.Poles;
            RatedCurrent = contactor.Amps.ToString();
            AlternativeModels = Contactors.Select(o => o.Model).Distinct().ToList();
            AlternativePolesNums = Contactors.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = Contactors.Select(o => o.Amps.ToString()).Distinct().ToList();
        }

        /// <summary>
        /// 接触器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="polesNum">级数</param>
        public Contactor(double calculateCurrent, string polesNum)
        {
            ComponentType = ComponentType.QAC;
            var contactors = ContactorConfiguration.contactorInfos.Where(o => o.Poles == polesNum && o.Amps > calculateCurrent).ToList();
            if (contactors.Count == 0)
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的Contactor");
            }
            this.Contactors = contactors;
            var contactor = contactors.First();
            Model = contactor.Model;
            PolesNum = contactor.Poles;
            RatedCurrent = contactor.Amps.ToString();
            AlternativeModels = Contactors.Select(o => o.Model).Distinct().ToList();
            AlternativePolesNums = Contactors.Select(o => o.Poles).Distinct().ToList();
            AlternativeRatedCurrents = Contactors.Select(o => o.Amps.ToString()).Distinct().ToList();
        }

        /// <summary>
        /// 修改型号
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetModel(string model)
        {
            if (Contactors.Any(o => o.Model == model
            && o.Poles == PolesNum
            && o.Amps.ToString() == RatedCurrent))
            {
                this.Model = model;
            }
            else
            {
                var contactor = Contactors.First(o => o.Model == model);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetModels()
        {
            return AlternativeModels;
        }

        /// <summary>
        /// 修改级数
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetPolesNum(string polesNum)
        {
            if (Contactors.Any(o => o.Poles == polesNum
            && o.Model == Model
            && o.Amps.ToString() == RatedCurrent))
            {
                this.PolesNum = polesNum;
            }
            else
            {
                var contactor = Contactors.First(o => o.Poles == polesNum);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetPolesNums()
        {
            return AlternativePolesNums;
        }

        /// <summary>
        /// 修改额定电流
        /// </summary>
        /// <param name="polesNum"></param>
        public void SetRatedCurrent(string ratedCurrent)
        {
            if (Contactors.Any(o => o.Poles == PolesNum
            && o.Model == Model
            && o.Amps.ToString() == ratedCurrent))
            {
                this.RatedCurrent = ratedCurrent;
            }
            else
            {
                var contactor = Contactors.First(o => o.Amps.ToString() == ratedCurrent);
                Model = contactor.Model;
                PolesNum = contactor.Poles;
                RatedCurrent = contactor.Amps.ToString();
            }
        }
        public List<string> GetRatedCurrents()
        {
            return AlternativeRatedCurrents;
        }

        /// <summary>
        /// 模型
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        private List<string> AlternativeModels { get;set; }
        private List<string> AlternativePolesNums { get;set; }
        private List<string> AlternativeRatedCurrents { get;set; }
        private List<ContactorConfigurationItem> Contactors { get;  set; }
    }
}
