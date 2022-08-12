using ThCADExtension;
using System.Windows.Media;
using TianHua.Electrical.PDS.Project.Module;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSLoadItemTypeVM : ObservableObject
    {
        private ImageLoadType _Type;
        public ImageLoadType Type
        {
            get => _Type;
            set => SetProperty(ref _Type, value);
        }

        public string Name => _Type.GetEnumDescription();

        private ImageSource _Image;
        public ImageSource Image
        {
            get => _Image;
            set => SetProperty(ref _Image, value);
        }
    }
}
