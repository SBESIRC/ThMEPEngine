using System;
using System.Linq;
using System.Windows;
using System.Collections;
using System.Windows.Controls.Primitives;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.UI.Project.Module.Component;
using HandyControl.Controls;

namespace TianHua.Electrical.PDS.UI.Editors
{
    public class ThPDSCircuitPhaseSequenceEnumPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
        {
            IsEnabled = !propertyItem.IsReadOnly,
            ItemsSource = GetItemsSource(propertyItem),
        };

        public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;

        private IEnumerable GetItemsSource(PropertyItem propertyItem)
        {
            if (propertyItem.Value is ThPDSCircuitModel circuit)
            {
                // 未知的负载，默认是三相(L123)，但是可以切换成单相(L1，L2，L3)
                // 已知的负载，如果是三相，那么就只能是三相。如果是单相，那么可以在L1,L2,L3切换
                var values = Enum.GetValues(propertyItem.PropertyType);
                if (circuit.LoadType is PDSNodeType.Unkown or PDSNodeType.Empty)
                {
                    // 剔除掉L相序
                    return values.OfType<PhaseSequence>()
                        .Where(o => o != PhaseSequence.L);
                }
                else
                {
                    if (circuit.PhaseSequence == PhaseSequence.L123)
                    {
                        // 只保留L123相序
                        return values.OfType<PhaseSequence>().Where(o => o == PhaseSequence.L123);
                    }
                    else if (circuit.PhaseSequence == PhaseSequence.L)
                    {
                        // 只保留L相序
                        return values.OfType<PhaseSequence>().Where(o => o == PhaseSequence.L);
                    }
                    else
                    {
                        // 剔除掉L和L123相序
                        return values.OfType<PhaseSequence>()
                            .Where(o => o != PhaseSequence.L)
                            .Where(o => o != PhaseSequence.L123);
                    }
                }
            }
            throw new NotSupportedException();
        }
    }
}
