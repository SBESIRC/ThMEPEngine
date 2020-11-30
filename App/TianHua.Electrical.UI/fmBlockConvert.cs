using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.Publics.BaseCode;

namespace TianHua.Electrical.UI
{
    public partial class fmBlockConvert : DevExpress.XtraEditors.XtraForm, IFireBlockConvert
    {
        public List<ViewFireBlockConvert> m_ListStrongBlockConver { get; set; }

        public List<ViewFireBlockConvert> m_ListWeakBlockConver { get; set; }

        public PresenterFireBlockConver m_Presenter;

        public void RessetPresenter()
        {
            if (m_Presenter != null)
            {
                this.Dispose();
                m_Presenter = null;
            }
            m_Presenter = new PresenterFireBlockConver(this);
        }

        public fmBlockConvert()
        {
            InitializeComponent();
        }

        private void fmFireBlockConver_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        public void InitForm()
        {
            RessetPresenter();
            m_ListStrongBlockConver = InitViewRelation();
            m_ListWeakBlockConver = InitViewRelation();
            GdcStrongCurrent.DataSource = m_ListStrongBlockConver;
            GdcWeakCurrent.DataSource = m_ListWeakBlockConver;
        }

        private List<ViewFireBlockConvert> InitViewRelation()
        {
            List<ViewFireBlockConvert> _List = new List<ViewFireBlockConvert>();
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,


                UpstreamBlockInfo = new BlockDataModel()
                {
                    Name = "70°C防火风口—2018",
                    ID = "2018",
                    Icon = Properties.Resources._1,
                    Visibility = "70防火阀FD"
                },
                DownstreamBlockInfo = new BlockDataModel() { Name = "70°防火风口", ID = "防火风口", Icon = Properties.Resources._1_1, Visibility = "70防火阀FD" }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,

                UpstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口—2018", ID = "板式排烟口-2018", Icon = Properties.Resources._2, Visibility = "770防火阀FD", },
                DownstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口", ID = "板式排烟口", Icon = Properties.Resources._2_1, Visibility = "770防火阀FD", }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,

                UpstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口—2018", ID = "板式排烟口-2018", Icon = Properties.Resources._3, Visibility = "111" },
                DownstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口", ID = "板式排烟口", Icon = Properties.Resources._3_1, Visibility = "222" }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,
                UpstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口—2018", ID = "板式排烟口-2018", Icon = Properties.Resources._4 },
                DownstreamBlockInfo = new BlockDataModel() { Name = "板式排烟口", ID = "板式排烟口", Icon = Properties.Resources._4_1 }
            });
            return _List;
        }


        private void Tab_SelectedPageChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {
            if (Tab.SelectedTabPage == PageStrongCurrent)
            {
                BtnOK.Text = "强电转换";
            }
            if (Tab.SelectedTabPage == PageWeakCurrent)
            {
                BtnOK.Text = "弱电转换";
            }

        }



        private void BtnOK_Click(object sender, EventArgs e)
        {
            int[] _Rownumber = new int[] { };

            if (Tab.SelectedTabPage == PageStrongCurrent)
            {
                _Rownumber = this.GdvStrongCurrent.GetSelectedRows();
            }
            if (Tab.SelectedTabPage == PageWeakCurrent)
            {
                _Rownumber = this.GdvWeakCurrent.GetSelectedRows();
            }
 
            List<ViewFireBlockConvert> _List = new List<ViewFireBlockConvert>();

            for (int i = 0; i < _Rownumber.Count(); i++)
            {
                ViewFireBlockConvert _ViewFireBlockConver = this.GdvStrongCurrent.GetRow(_Rownumber[i]) as ViewFireBlockConvert;
                _List.Add(_ViewFireBlockConver);
            }
        }


    }
}
