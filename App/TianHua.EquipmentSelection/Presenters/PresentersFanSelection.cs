using System;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;
using TianHua.FanSelection.Messaging;

namespace TianHua.FanSelection
{
    public class PresentersFanSelection : Presenter<IFanSelection>
    {
        public PresentersFanSelection(IFanSelection View) : base(View)
        {

        }

        public override void OnViewEvent()
        {
        }

        public override void OnViewLoaded()
        {
            View.m_ListScenario = InitScenario();

            View.m_ListFan = InitFan();

            View.m_ListVentStyle = GetVentStyle();

            View.m_ListVentConnect = GetVentConnect();

            View.m_ListVentLev = GetVentLev();

            View.m_ListEleLev = GetEleLev();

            View.m_ListMotorTempo = GetMotorTempo();

            View.m_ListMountType = GetMountType();
        }

        private List<FanDataModel> InitFan()
        {
            return new List<FanDataModel>();
        }

        private List<string> InitScenario()
        {
            List<string> _EnumList = new List<string>();
            foreach (var _Item in Enum.GetValues(typeof(EnumScenario)))
            {
                string _Enum = string.Empty;
                _Enum = FuncStr.NullToStr(_Item);
                _EnumList.Add(_Enum);
            }
            return _EnumList;
        }


        public List<string> GetVentStyle()
        {
            List<string> _List = new List<string>();
            _List.Add("前倾离心(电机内置)");
            _List.Add("前倾离心(电机外置)");
            _List.Add("后倾离心(电机内置)");
            _List.Add("后倾离心(电机外置)");
            _List.Add("轴流");
            return _List;
        }


        public List<string> GetVentConnect()
        {
            List<string> _List = new List<string>();
            _List.Add("皮带");
            _List.Add("直连");
            return _List;
        }


        public List<string> GetVentLev()
        {
            List<string> _List = new List<string>();
            _List.Add("1级");
            _List.Add("2级");
            _List.Add("3级");
            return _List;
        }

        public List<string> GetEleLev()
        {
            List<string> _List = new List<string>();
            _List.Add("1级");
            _List.Add("2级");
            _List.Add("3级");
            return _List;
        }

        public List<int> GetMotorTempo()
        {
            List<int> _List = new List<int>();
            _List.Add(2900);
            _List.Add(1450);
            _List.Add(960);
            return _List;
        }

        public List<string> GetMountType()
        {
            List<string> _List = new List<string>();
            _List.Add("吊装");
            _List.Add("落地");
            return _List;
        }

    }
}
