using System.Collections.Generic;
using System.Collections.ObjectModel;
using TianHua.Electrical.PDS.UI.Models;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSTransformerVM : ObservableObject
    {
        private readonly IList<ThPDSTransformer> _transformers;

        public IList<ThPDSTransformer> DataList => _transformers;

        public ThPDSTransformerVM()
        {
            _transformers = new List<ThPDSTransformer>();
        }

        internal ObservableCollection<ThPDSTransformer> GetCardDataList()
        {
            return new ObservableCollection<ThPDSTransformer>
            {
                new ThPDSTransformer()
                {
                    Number = "1T1",
                    LoadRate = 0.8,
                },
                new ThPDSTransformer()
                {
                    Number = "1T2",
                    LoadRate = 0.8,
                },
                new ThPDSTransformer()
                {
                    Number = "1T3",
                    LoadRate = 0.75,
                },
                new ThPDSTransformer()
                {
                    Number = "1T4",
                    LoadRate = 0.75,
                }
            };
        }
    }
}
