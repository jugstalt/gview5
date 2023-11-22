using gView.Desktop.Wpf.Controls;
using gView.Framework.Carto;
using gView.Framework.Globalisation;
using gView.Framework.UI;
using System;
using System.Windows;

namespace gView.Win.Carto.Items
{
    internal class ToolButton : Fluent.Button
    {
        public ToolButton(ITool tool)
        {
            this.Tool = tool;

            String toolName = !String.IsNullOrEmpty(tool.ToolTip) ? tool.ToolTip : LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Header = toolName;
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);

            if (tool is IScreenTip)
            {
                base.ToolTip = new Fluent.ScreenTip()
                {
                    Text = ((IScreenTip)tool).ScreenTip
                };
            }
        }

        public ITool Tool { get; private set; }
    }

    internal class ContextToolButton : Fluent.Button
    {
        private object _contextObject = null;

        public ContextToolButton(ITool tool, object contextObject)
        {
            this.Tool = tool;

            String toolName = !String.IsNullOrEmpty(tool.ToolTip) ? tool.ToolTip : LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Header = toolName;
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);

            if (tool is IScreenTip)
            {
                base.ToolTip = new Fluent.ScreenTip()
                {
                    Text = ((IScreenTip)tool).ScreenTip
                };
            }

            _contextObject = contextObject;
            this.Click += new RoutedEventHandler(ContextToolButton_Click);
        }

        private void ContextToolButton_Click(object sender, RoutedEventArgs e)
        {
            Tool.OnEvent(_contextObject);
        }

        public ITool Tool { get; private set; }
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
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);

            base.Click += new RoutedEventHandler(ContextMenuButton_Click);

            bool visible = false, enable = false;
            if (_contextType.ContextObject is IMap)
            {
                enable = tool.Enable(contextType.ContextObject);
                visible = tool.Visible(contextType.ContextObject);
            }
            else if (_contextType.ContextObject is ITocElement && ((ITocElement)_contextType.ContextObject).Layers != null)
            {
                ITocElement tocElement = (ITocElement)_contextType.ContextObject;

                foreach (var layer in tocElement.Layers)
                {
                    if (visible == true && enable == true)
                    {
                        break;
                    }

                    if (!enable)
                    {
                        enable = tool.Enable(layer);
                    }

                    if (!visible)
                    {
                        visible = tool.Visible(layer);
                    }
                }
            }

            base.IsEnabled = enable;
            base.Visibility = visible == false ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ContextMenuButton_Click(object sender, RoutedEventArgs e)
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

    internal class ToolToggleButton : Fluent.ToggleButton, ICheckAbleButton
    {
        public ToolToggleButton(ITool tool)
        {
            this.Tool = tool;

            String toolName = !String.IsNullOrEmpty(tool.ToolTip) ? tool.ToolTip : LocalizedResources.GetResString(tool.Name, tool.Name);
            base.Header = toolName;
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(tool.Image as System.Drawing.Image);

            if (tool is IScreenTip)
            {
                base.ToolTip = new Fluent.ScreenTip()
                {
                    Text = ((IScreenTip)tool).ScreenTip
                };
            }
        }

        public ITool Tool { get; private set; }

        public new bool Checked
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

    internal class OptionsButton : Fluent.Button
    {
        public OptionsButton(IMapOptionPage page)
        {
            this.MapOptionPage = page;

            base.Header = page.Title;
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(
                page.Image == null ? global::gView.Win.Carto.Properties.Resources.options : page.Image as System.Drawing.Image
                );
        }

        public IMapOptionPage MapOptionPage { get; private set; }
    }

    internal class PdfLinkButton : Fluent.Button
    {
        public PdfLinkButton(System.IO.FileInfo pdf)
        {
            this.PdfFile = pdf;

            base.Header = pdf.Name.Substring(0, pdf.Name.Length - pdf.Extension.Length);
            base.Icon = base.LargeIcon = ImageFactory.FromBitmap(global::gView.Win.Carto.Properties.Resources.pdf);
            base.Click += new RoutedEventHandler(PdfLinkButton_Click);
        }

        private void PdfLinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = this.PdfFile.FullName;
                p.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public System.IO.FileInfo PdfFile { get; private set; }
    }
}