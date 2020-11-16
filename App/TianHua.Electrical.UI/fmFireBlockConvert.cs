﻿using System;
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
    public partial class fmFireBlockConvert : DevExpress.XtraEditors.XtraForm, IFireBlockConvert
    {
        public List<ViewFireBlockConvert> m_ListFireBlockConver { get; set; }

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

        public fmFireBlockConvert()
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
            m_ListFireBlockConver =  InitViewRelation();
            Gdc.DataSource = m_ListFireBlockConver;
        }

        private List<ViewFireBlockConvert> InitViewRelation()
        {
            List<ViewFireBlockConvert> _List = new List<ViewFireBlockConvert>();
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,
                Visibility = "70防火阀FD",

                UpstreamBlockInfo = new BlockDataModel() { RealName = "70°C防火风口—2018", Name = "2018", Icon = Properties.Resources._1 },
                DownstreamBlockInfo = new BlockDataModel() { RealName = "70°防火风口", Name = "防火风口", Icon = Properties.Resources._1_1 }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,
                Visibility = "770防火阀FD",
                UpstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口—2018", Name = "板式排烟口-2018", Icon = Properties.Resources._2 },
                DownstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口", Name = "板式排烟口", Icon = Properties.Resources._2_1 }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,
                Visibility = "111",
                UpstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口—2018", Name = "板式排烟口-2018", Icon = Properties.Resources._3 },
                DownstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口", Name = "板式排烟口", Icon = Properties.Resources._3_1 }
            });
            _List.Add(new ViewFireBlockConvert()
            {
                IsSelect = false,
                Visibility = "222",
                UpstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口—2018", Name = "板式排烟口-2018", Icon = Properties.Resources._4 },
                DownstreamBlockInfo = new BlockDataModel() { RealName = "板式排烟口", Name = "板式排烟口", Icon = Properties.Resources._4_1 }
            });
            return _List;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            int[] _Rownumber = this.Gdv.GetSelectedRows();

            List<ViewFireBlockConvert> _List = new List<ViewFireBlockConvert>();

            for (int i = 0; i < _Rownumber.Count(); i++)
            {
                ViewFireBlockConvert _ViewFireBlockConver = this.Gdv.GetRow(_Rownumber[i]) as ViewFireBlockConvert;
                _List.Add(_ViewFireBlockConver);
            }
        }
    }
}
