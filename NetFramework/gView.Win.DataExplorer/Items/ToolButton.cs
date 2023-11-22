using gView.Desktop.Wpf.Controls;
using gView.Framework.Carto;
using gView.Framework.Globalisation;
using gView.Framework.UI;
using System.Windows;

namespace gView.Win.DataExplorer.Items
{
    internal class ToolButton : Fluent.Button
    {
        public ToolButton(IExTool tool)
        {
            this.Tool = tool;

            base.Header = LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as global::System.Drawing.Image);
        }

        public IExTool Tool { get; private set; }
    }

    internal class ContextMenuButton : Fluent.Button
    {
        private IContextType _contextType;
        private IMapDocument _doc;

        public ContextMenuButton(IContextMenuTool tool, IContextType contextType, IMapDocument mapDocument)
        {
            this.Tool = tool;
            _contextType = contextType;
            _doc = mapDocument;

            base.Header = LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as global::System.Drawing.Image);

            base.Click += new RoutedEventHandler(ContextMenuButton_Click);
        }

        void ContextMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (_doc == null || Tool == null || _contextType == null)
            {
                return;
            }

            if (_contextType.ContextObject is IMap)
            {
                Tool.OnEvent(_contextType.ContextObject, _doc);
            }
            else if (_contextType.ContextObject is ITocElement && ((ITocElement)_contextType.ContextObject).Layers != null)
            {
                ITocElement tocElement = (ITocElement)_contextType.ContextObject;

                foreach (var layer in tocElement.Layers)
                {
                    if (layer.Class == null || layer.Class.Dataset == null)
                    {
                        continue;
                    }

                    Tool.OnEvent(layer, layer.Class.Dataset);
                }
            }
        }

        public IContextMenuTool Tool { get; private set; }
    }

    /*
    internal class ToolToggleButton : Fluent.ToggleButton, ICheckAbleButton
    {
        public ToolToggleButton(ITool tool)
        {
            this.Tool = tool;

            base.Header = LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);
        }

        public ITool Tool { get; private set; }


        public bool Checked
        {
            get
            {
                return (bool)base.IsChecked;
            }
            set
            {
                base.IsChecked = value;
            }
        }
    }

  */
}