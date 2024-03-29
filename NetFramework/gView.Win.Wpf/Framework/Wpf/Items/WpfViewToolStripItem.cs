﻿using gView.Desktop.Wpf.Controls;
using gView.Framework.UI;

namespace gView.Desktop.Wpf.Items
{
    public class WpfViewToolStripItem : System.Windows.Controls.MenuItem
    {
        IDockableWindow _window;
        public WpfViewToolStripItem(string text, System.Drawing.Image image, IDockableWindow window)
            : base()
        {
            base.Header = text;
            if (image != null)
            {
                base.Icon = ImageFactory.FromBitmap(image);
            }

            _window = window;
        }

        public IDockableWindow DockableToolWindow
        {
            get { return _window; }
        }
    }
}
