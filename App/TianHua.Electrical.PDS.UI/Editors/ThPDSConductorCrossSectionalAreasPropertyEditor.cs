using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSConductorCrossSectionalAreasPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private List<double> GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSConductorModel conductor)
            {
                switch (conductor.ComponentType)
                {
                    case ComponentType.Conductor:
                    case ComponentType.ControlConductor:
                        return conductor.AlternativeConductorCrossSectionalAreas;
                }
            }
            throw new NotSupportedException();
        }
    }
}
