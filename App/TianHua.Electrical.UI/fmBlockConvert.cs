﻿using System;
using System.IO;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using ThMEPElectrical.BlockConvert;

namespace TianHua.Electrical.UI
{
    public partial class fmBlockConvert : DevExpress.XtraEditors.XtraForm, IFireBlockConvert
    {
        public PresenterFireBlockConvert m_Presenter;

        public List<ViewFireBlockConvert> m_ListWeakBlockConvert { get; set; }

        public List<ViewFireBlockConvert> m_ListStrongBlockConvert { get; set; }

        public ConvertMode ActiveConvertMode
        {
            get
            {
                if (Tab.SelectedTabPage == PageStrongCurrent)
                {
                    return ConvertMode.STRONGCURRENT;
                }
                else if (Tab.SelectedTabPage == PageWeakCurrent)
                {
                    return ConvertMode.WEAKCURRENT;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public void RessetPresenter()
        {
            if (m_Presenter != null)
            {
                this.Dispose();
                m_Presenter = null;
            }
            m_Presenter = new PresenterFireBlockConvert(this);
        }

        public fmBlockConvert()
        {
            InitializeComponent();
            m_ListWeakBlockConvert = new List<ViewFireBlockConvert>();
            m_ListStrongBlockConvert = new List<ViewFireBlockConvert>();
        }

        private void fmFireBlockConver_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        public void InitForm()
        {
            RessetPresenter();
            GdcWeakCurrent.DataSource = m_ListWeakBlockConvert;
            GdcStrongCurrent.DataSource = m_ListStrongBlockConvert;
        }

        private string BlockDwgPath()
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThBConvertCommon.BLOCK_MAP_RULES_FILE);
        }

        private void Tab_SelectedPageChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {

            switch(ActiveConvertMode)
            {
                case ConvertMode.STRONGCURRENT:
                    BtnOK.Text = "强电转换";
                    break;
                case ConvertMode.WEAKCURRENT:
                    BtnOK.Text = "弱电转换";
                    break;
                default:
                    throw new NotSupportedException();
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

            for (int i = 0; i < _Rownumber.Count(); i++)
            {
                ViewFireBlockConvert _ViewFireBlockConvert = this.GdvStrongCurrent.GetRow(_Rownumber[i]) as ViewFireBlockConvert;
                _ViewFireBlockConvert.IsSelect = true;
            }
        }
    }
}
