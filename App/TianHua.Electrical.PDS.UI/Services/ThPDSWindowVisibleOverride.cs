using System;
using System.Windows;

namespace TianHua.Electrical.PDS.UI.Services
{
    public class ThPDSWindowVisibleOverride : IDisposable
    {
        private readonly Window _window;

        public ThPDSWindowVisibleOverride(Window window)
        {
            _window = window;
            if (_window != null)
            {
                _window.Hide();
            }
        }

        public void Dispose()
        {
            if (_window != null)
            {
                _window.Show();
            }
        }
    }
}
