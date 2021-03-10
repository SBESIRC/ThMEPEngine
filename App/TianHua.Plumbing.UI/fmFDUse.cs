using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThMEPWSS.Pipe.Service;

namespace TianHua.Plumbing.UI
{
    public partial class fmFDUse : DevExpress.XtraEditors.XtraForm
    {
        public fmFDUse()
        {
            InitializeComponent();
        }

        public void InitForm( )
        {
            List<string> _List = new List<string> { "小屋面", "大屋面", "43F（标）", "44F（标）", "43F（非）" };
            var floorNames = new List<string>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var storey = new ThReadStoreyInformationService();
                storey.Read(acadDatabase.Database);
                storey.StoreyNames.ForEach(o => floorNames.Add(o.Item2));
                CheckList.DataSource = storey.StoreyNames.Count>0? floorNames : _List;              
            }
        }

        private void fmFDUse_Load(object sender, EventArgs e)
        {
            InitForm();
        }
        private void BtnOK_Click(object sender, EventArgs e)
        {

            var result = new List<Tuple<string, bool>>();
            foreach(var item in CheckList.CheckedItems)
            {
                result.Add(Tuple.Create(item.ToString(),CheckLabel.Checked));
            }      
            ThTagParametersService.targetFloors = result;
            //聚焦到CAD
            SetFocusToDwgView();
            //发送命令
            CommandHandlerBase.ExecuteFromCommandLine(false, "THLGYY");
        }
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void SetFocusToDwgView()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
