using AcHelper;
using System.Linq;
using System.Collections.ObjectModel;
using ThPlatform3D.StructPlane;

namespace Tianhua.Platform3D.UI.StructurePlane
{
    public class DrawingParameterSetVM
    {
        public DrawingParameterSetModel Model { get; set; }
        /// <summary>
        /// 楼层集合
        /// </summary>
        public ObservableCollection<string> Storeies { get; set; }
        /// <summary>
        /// 图纸比例集合
        /// </summary>
        public ObservableCollection<string> DrawingScales { get; set; }
        public DrawingParameterSetVM()
        {
            Model = new DrawingParameterSetModel();
            var stdFlrNoDisplayNames = ThDrawingParameterConfig.Instance.StdFlrNoDisplayName.Values.ToList();
            Storeies = new ObservableCollection<string>(stdFlrNoDisplayNames);
            DrawingScales = new ObservableCollection<string>(ThDrawingParameterConfig.Instance.DrawingScales);
        }
        public void Run()
        {
            Model.Write();
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
