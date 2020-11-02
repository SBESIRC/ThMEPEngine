using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public class PresenterSmokeLayout : Presenter<ISmokeLayout>
    {
        public PresenterSmokeLayout(ISmokeLayout View) : base(View)
        {

        }

        public override void OnViewEvent()
        {

        }

        public override void OnViewLoaded()
        {
            View.m_ListSmokeRoomArea = InitListSmokeRoomArea();

            View.m_ListThalposisRoomArea = InitThalposisRoomArea();

            View.m_ListSmokeRoomHeight = InitListSmokeRoomHeight();

            View.m_ListThalposisRoomHeight = InitThalposisRoomHeight();
        }



        private List<string> InitListSmokeRoomHeight()
        {
            List<string> _List = new List<string>();
            _List.Add("h≤12");
            _List.Add("6<h≤12");
            _List.Add("h≤6");
            return _List;
        }

        private List<string> InitThalposisRoomHeight()
        {
            List<string> _List = new List<string>();
            _List.Add("h≤8");
            return _List;
        }

        private List<string> InitThalposisRoomArea()
        {
            List<string> _List = new List<string>();
            _List.Add("S≤30");
            _List.Add("S＞30");
            return _List;
        }

        private List<string> InitListSmokeRoomArea()
        {
            List<string> _List = new List<string>();
            _List.Add("S≤80");
            _List.Add("S＞80");
            return _List;
        }


    }
}
