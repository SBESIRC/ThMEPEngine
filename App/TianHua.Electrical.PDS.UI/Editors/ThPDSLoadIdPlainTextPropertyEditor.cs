using System.Windows;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSLoadIdPlainTextPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.TextBox
        {
            IsReadOnly = GetIsReadOnly(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => System.Windows.Controls.TextBox.TextProperty;

        private bool GetIsReadOnly(PropertyItem propertyItem)
        {
            var model = propertyItem.Value as ThPDSCircuitModel;
            if (model != null)
            {
                return !string.IsNullOrEmpty(model.LoadId);
            }
            else
            {
                return propertyItem.IsReadOnly;
            }
        }
    }
}
