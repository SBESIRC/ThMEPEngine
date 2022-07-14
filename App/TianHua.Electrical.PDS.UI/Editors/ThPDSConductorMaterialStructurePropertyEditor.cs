﻿using System;
using System.Linq;
using System.Windows;
using ThCADExtension;
using System.Collections;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSConductorMaterialStructurePropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem)
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        protected override IValueConverter GetConverter(PropertyItem propertyItem)
        {
            return new ThPDSEnumDescriptionConverter<MaterialStructure>();
        }

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSConductorModel conductor)
            {
                if (propertyItem.DisplayName == "材料特征及结构")
                {
                    return conductor.AlternativeOuterSheathMaterialTypes
                        .Where(o => Convert.ToUInt32(o) > 0)
                        .Select(o => o.GetEnumDescription());
                }
            }
            throw new NotSupportedException();
        }
    }
}