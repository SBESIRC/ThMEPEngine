using Linq2Acad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThCADExtension;
using ThMEPElectrical.BlockConvert;
using TianHua.Publics.BaseCode;
using TianHua.Electrical.UI.CAD;

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
        }

        private void fmFireBlockConver_Load(object sender, EventArgs e)
        {
            InitForm();
        }

        public void InitForm()
        {
            RessetPresenter();
            InitViewRelation();
            GdcWeakCurrent.DataSource = m_ListWeakBlockConvert;
            GdcStrongCurrent.DataSource = m_ListStrongBlockConvert;
        }

        private string BlockDwgPath()
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThBConvertCommon.BLOCK_MAP_RULES_FILE);
        }

        private void InitViewRelation()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            using (ThBConvertManager manager = ThBConvertManager.CreateManager(blockDb.Database, ConvertMode.ALL))
            {
                m_ListStrongBlockConvert.Clear();
                foreach (var rule in manager.Rules.Where(o => (o.Mode & ConvertMode.STRONGCURRENT) == ConvertMode.STRONGCURRENT))
                {
                    m_ListStrongBlockConvert.Add(new ViewFireBlockConvert()
                    {
                        UpstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item1),
                        DownstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item2),
                    });
                }

                m_ListWeakBlockConvert.Clear();
                foreach (var rule in manager.Rules.Where(o => (o.Mode & ConvertMode.WEAKCURRENT) == ConvertMode.WEAKCURRENT))
                {
                    m_ListWeakBlockConvert.Add(new ViewFireBlockConvert()
                    {
                        UpstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item1),
                        DownstreamBlockInfo = blockDb.Database.CreateBlockDataModel(rule.Transformation.Item2),
                    });
                }
            }
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
 
            List<ViewFireBlockConvert> _List = new List<ViewFireBlockConvert>();

            for (int i = 0; i < _Rownumber.Count(); i++)
            {
                ViewFireBlockConvert _ViewFireBlockConver = this.GdvStrongCurrent.GetRow(_Rownumber[i]) as ViewFireBlockConvert;
                _List.Add(_ViewFireBlockConver);
            }
        }
    }
}
