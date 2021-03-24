using System;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Pipe.Engine;

namespace TianHua.Plumbing.UI
{
    public partial class fmFDUse : DevExpress.XtraEditors.XtraForm
    {
        public fmFDUse()
        {
            InitializeComponent();
        }

        public void InitForm()
        {
            var floorNames = new List<string>();
            List<string> _List = new List<string> { };
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var storey = new ThReadStoreyInformationService();
                storey.Read(acadDatabase.Database);
                if (storey.StoreyNames.Count == 0)
                {
                    return;
                }
                storey.StoreyNames.ForEach(o => floorNames.Add(o.Item2));
                CheckList.DataSource = storey.StoreyNames.Count > 0 ? floorNames : _List;
            }
        }

        private void fmFDUse_Load(object sender, EventArgs e)
        {
            InitForm();
        }
        private void BtnOK_Click(object sender, EventArgs e)
        {
            var result = new List<Tuple<string, bool>>();
            foreach (var item in CheckList.CheckedItems)
            {
                result.Add(Tuple.Create(item.ToString(), CheckLabel.Checked));
            }
            ThTagParametersService.targetFloors = result;
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void CheckList_DoubleClick(object sender, EventArgs e)
        {
            var item = CheckList.SelectedItem as string;
            ThBlockSelectionEngine.ZoomToModels(item);
        }
    }
}
