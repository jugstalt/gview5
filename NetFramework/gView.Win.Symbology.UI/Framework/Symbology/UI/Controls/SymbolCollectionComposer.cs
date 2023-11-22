using gView.Framework.Carto.UI;
using gView.Framework.Sys.UI.Extensions;
using gView.Framework.system;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace gView.Framework.Symbology.UI.Controls
{
    /// <summary>
    /// Zusammenfassung für SymbolCollectionComposer.
    /// </summary>
    public class SymbolCollectionComposer : System.Windows.Forms.UserControl
    {
        private System.Windows.Forms.GroupBox boxPreview;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Splitter splitter1;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ListBox lbSymbols;
        private System.Windows.Forms.ImageList iList;
        private SymbolCollection _symbol = null;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private ToolStripButton btnRemove;
        private ToolStripButton btnMoveDown;
        private ToolStripButton btnMoveUp;
        private ToolStripButton toolStripButton5;
        private ToolStripButton toolStripButton6;
        private ToolStripButton toolStripButton7;
        private ToolStripButton toolStripButton8;
        private ToolStripButton toolStripButton9;
        private ToolStripButton toolStripButton10;
        private ToolStripSplitButton toolStripSplitButton1;
        private ToolStripTextBox txtYOffset;
        private ToolStripTextBox txtRotation;
        private ToolStripTextBox txtXOffset;
        private ToolStripComboBox toolStripComboBox1;
        private Panel panelPreview;
        private ToolStrip toolStrip2;
        private ISymbol _selectedSymbol = null;

        public delegate void SelectedSymbolChangedEvent(ISymbol symbol);
        public event SelectedSymbolChangedEvent SelectedSymbolChanged = null;

        public SymbolCollectionComposer()
        {
            // Dieser Aufruf ist für den Windows Form-Designer erforderlich.
            InitializeComponent();
        }

        /// <summary> 
        /// Die verwendeten Ressourcen bereinigen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public ISymbol Symbol
        {
            get
            {
                if (_symbol == null)
                {
                    return null;
                }

                if (_symbol.Symbols.Count == 1)
                {
                    return ((SymbolCollectionItem)_symbol.Symbols[0]).Symbol;
                }

                return _symbol;
            }
            set
            {
                if (_symbol != null)
                {
                    _symbol.Release();
                }
                if (value is SymbolCollection)
                {
                    _symbol = (SymbolCollection)value;
                }
                else if (value is ISymbol)
                {
                    _symbol = new SymbolCollection(_symbol);
                }
            }
        }

        public void AddSymbol(ISymbol symbol)
        {
            if (_symbol == null)
            {
                _symbol = new SymbolCollection();
            }

            _symbol.AddSymbol(symbol);

            if (symbol is SymbolCollection)
            {
                if (((SymbolCollection)symbol).Symbols.Count > 0)
                {
                    _selectedSymbol = ((SymbolCollectionItem)((SymbolCollection)symbol).Symbols[0]).Symbol;
                }
            }
            else
            {
                _selectedSymbol = symbol;
            }
            buildList();
            DrawPreview();
        }

        private void RemoveSymbol(ISymbol symbol)
        {
            if (symbol == null || _symbol == null)
            {
                return;
            }

            if (_symbol.Symbols.Count < 2)
            {
                return;
            }

            _symbol.RemoveSymbol(symbol);
            buildList();
            DrawPreview();
        }

        private void MoveUpSymbol(ISymbol symbol)
        {
            if (symbol == null || _symbol == null)
            {
                return;
            }

            int index = _symbol.IndexOf(symbol);
            if (index < 1)
            {
                return;
            }

            ISymbol prev = ((SymbolCollectionItem)_symbol.Symbols[index - 1]).Symbol;
            bool visible = _symbol.IsVisible(symbol);
            _symbol.RemoveSymbol(symbol);
            _symbol.InsertBefore(symbol, prev, visible);
        }

        private void MoveDownSymbol(ISymbol symbol)
        {
            if (symbol == null || _symbol == null)
            {
                return;
            }

            int index = _symbol.IndexOf(symbol);
            if (index == -1 || index >= _symbol.Symbols.Count - 1)
            {
                return;
            }

            bool visible = _symbol.IsVisible(symbol);
            if (index < _symbol.Symbols.Count - 2)
            {
                ISymbol next = ((SymbolCollectionItem)_symbol.Symbols[index + 2]).Symbol;
                _symbol.RemoveSymbol(symbol);
                _symbol.InsertBefore(symbol, next, visible);
            }
            else
            {
                _symbol.RemoveSymbol(symbol);
                _symbol.AddSymbol(symbol, visible);
            }
        }

        public ISymbol ReplaceSelectedSymbol(ISymbol newSymbol)
        {
            if (_selectedSymbol == null)
            {
                return null;
            }

            _symbol.ReplaceSymbol(_selectedSymbol, newSymbol);
            ISymbol old = _selectedSymbol;
            _selectedSymbol = newSymbol;

            Refresh();

            return old;
        }
        public ISymbol SelectedSymol
        {
            get
            {
                return _selectedSymbol;
            }
        }

        public void Init()
        {
            if (lbSymbols.Items.Count > 0)
            {
                lbSymbols.SelectedIndex = 0;
            }
        }

        new public void Refresh()
        {
            lbSymbols.Refresh();
            DrawPreview();
        }

        private void buildList()
        {
            lbSymbols.Items.Clear();
            foreach (SymbolCollectionItem item in _symbol.Symbols)
            {
                lbSymbols.Items.Add(item);
            }
            DrawPreview();
        }
        #region Vom Komponenten-Designer generierter Code
        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SymbolCollectionComposer));
            this.boxPreview = new System.Windows.Forms.GroupBox();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton5 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton6 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton7 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton8 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton9 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton10 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.txtXOffset = new System.Windows.Forms.ToolStripTextBox();
            this.txtYOffset = new System.Windows.Forms.ToolStripTextBox();
            this.txtRotation = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.btnRemove = new System.Windows.Forms.ToolStripButton();
            this.btnMoveDown = new System.Windows.Forms.ToolStripButton();
            this.btnMoveUp = new System.Windows.Forms.ToolStripButton();
            this.lbSymbols = new System.Windows.Forms.ListBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.iList = new System.Windows.Forms.ImageList(this.components);
            this.boxPreview.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // boxPreview
            // 
            resources.ApplyResources(this.boxPreview, "boxPreview");
            this.boxPreview.Controls.Add(this.panelPreview);
            this.boxPreview.Controls.Add(this.toolStrip2);
            this.boxPreview.Name = "boxPreview";
            this.boxPreview.TabStop = false;
            // 
            // panelPreview
            // 
            resources.ApplyResources(this.panelPreview, "panelPreview");
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint_1);
            // 
            // toolStrip2
            // 
            resources.ApplyResources(this.toolStrip2, "toolStrip2");
            this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton5,
            this.toolStripButton6,
            this.toolStripButton7,
            this.toolStripButton8,
            this.toolStripButton9,
            this.toolStripButton10,
            this.toolStripSplitButton1,
            this.toolStripComboBox1});
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.ShowItemToolTips = false;
            // 
            // toolStripButton5
            // 
            resources.ApplyResources(this.toolStripButton5, "toolStripButton5");
            this.toolStripButton5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton5.Name = "toolStripButton5";
            this.toolStripButton5.Click += new System.EventHandler(this.btnLeft_Click);
            // 
            // toolStripButton6
            // 
            resources.ApplyResources(this.toolStripButton6, "toolStripButton6");
            this.toolStripButton6.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton6.Name = "toolStripButton6";
            this.toolStripButton6.Click += new System.EventHandler(this.btn_Up_Click);
            // 
            // toolStripButton7
            // 
            resources.ApplyResources(this.toolStripButton7, "toolStripButton7");
            this.toolStripButton7.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton7.Name = "toolStripButton7";
            this.toolStripButton7.Click += new System.EventHandler(this.btn_Down_Click);
            // 
            // toolStripButton8
            // 
            resources.ApplyResources(this.toolStripButton8, "toolStripButton8");
            this.toolStripButton8.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton8.Name = "toolStripButton8";
            this.toolStripButton8.Click += new System.EventHandler(this.btnRight_Click);
            // 
            // toolStripButton9
            // 
            resources.ApplyResources(this.toolStripButton9, "toolStripButton9");
            this.toolStripButton9.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton9.Name = "toolStripButton9";
            this.toolStripButton9.Click += new System.EventHandler(this.btnRotate2_Click);
            // 
            // toolStripButton10
            // 
            resources.ApplyResources(this.toolStripButton10, "toolStripButton10");
            this.toolStripButton10.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton10.Name = "toolStripButton10";
            this.toolStripButton10.Click += new System.EventHandler(this.btnRotate1_Click);
            // 
            // toolStripSplitButton1
            // 
            resources.ApplyResources(this.toolStripSplitButton1, "toolStripSplitButton1");
            this.toolStripSplitButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
            this.toolStripSplitButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.txtXOffset,
            this.txtYOffset,
            this.txtRotation});
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            // 
            // txtXOffset
            // 
            resources.ApplyResources(this.txtXOffset, "txtXOffset");
            this.txtXOffset.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtXOffset.HideSelection = false;
            this.txtXOffset.Name = "txtXOffset";
            this.txtXOffset.TextChanged += new System.EventHandler(this.txtXOffset_TextChanged);
            // 
            // txtYOffset
            // 
            resources.ApplyResources(this.txtYOffset, "txtYOffset");
            this.txtYOffset.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtYOffset.HideSelection = false;
            this.txtYOffset.Name = "txtYOffset";
            this.txtYOffset.TextChanged += new System.EventHandler(this.txtYOffset_TextChanged);
            // 
            // txtRotation
            // 
            resources.ApplyResources(this.txtRotation, "txtRotation");
            this.txtRotation.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtRotation.HideSelection = false;
            this.txtRotation.Name = "txtRotation";
            this.txtRotation.TextChanged += new System.EventHandler(this.txtRotation_TextChanged);
            // 
            // toolStripComboBox1
            // 
            resources.ApplyResources(this.toolStripComboBox1, "toolStripComboBox1");
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            // 
            // panel2
            // 
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Controls.Add(this.toolStrip1);
            this.panel2.Name = "panel2";
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.btnRemove,
            this.btnMoveDown,
            this.btnMoveUp});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // toolStripButton1
            // 
            resources.ApplyResources(this.toolStripButton1, "toolStripButton1");
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            resources.ApplyResources(this.btnRemove, "btnRemove");
            this.btnRemove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnMoveDown
            // 
            resources.ApplyResources(this.btnMoveDown, "btnMoveDown");
            this.btnMoveDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnMoveUp
            // 
            resources.ApplyResources(this.btnMoveUp, "btnMoveUp");
            this.btnMoveUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // lbSymbols
            // 
            resources.ApplyResources(this.lbSymbols, "lbSymbols");
            this.lbSymbols.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbSymbols.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbSymbols.Name = "lbSymbols";
            this.lbSymbols.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbSymbols_DrawItem);
            this.lbSymbols.SelectedIndexChanged += new System.EventHandler(this.lbSymbols_SelectedIndexChanged);
            this.lbSymbols.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lbSymbols_MouseDown);
            this.lbSymbols.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lbSymbols_MouseMove);
            this.lbSymbols.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lbSymbols_MouseUp);
            // 
            // splitter1
            // 
            resources.ApplyResources(this.splitter1, "splitter1");
            this.splitter1.Name = "splitter1";
            this.splitter1.TabStop = false;
            // 
            // iList
            // 
            this.iList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("iList.ImageStream")));
            this.iList.TransparentColor = System.Drawing.Color.Transparent;
            this.iList.Images.SetKeyName(0, "");
            this.iList.Images.SetKeyName(1, "");
            // 
            // SymbolCollectionComposer
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.lbSymbols);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.boxPreview);
            this.Name = "SymbolCollectionComposer";
            this.boxPreview.ResumeLayout(false);
            this.boxPreview.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private void lbSymbols_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            int index = 0;
            using (Font font = new Font("Arial", 9f))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Far;
                foreach (SymbolCollectionItem item in lbSymbols.Items)
                {
                    try
                    {
                        if (e.Index == index)
                        {
                            e.Graphics.DrawString((index + 1).ToString() + ":", font, Brushes.Black, 20f, e.Bounds.Top + 3, format);
                            e.Graphics.DrawImage(
                                iList.Images[((item.Visible) ? 1 : 0)],
                                20, e.Bounds.Top + e.Bounds.Height / 2 - 10, 19, 20);

                            Rectangle rect = new Rectangle(
                                e.Bounds.X + 42, e.Bounds.Y,
                                e.Bounds.Width - 43, e.Bounds.Height);
                            if (item.Symbol == _selectedSymbol)
                            {
                                using (SolidBrush brush = new SolidBrush(Color.BlueViolet))
                                {
                                    e.Graphics.FillRectangle(brush, rect);
                                }
                            }
                            using (Pen pen = new Pen(Color.Gray))
                            {
                                e.Graphics.DrawRectangle(pen, rect);
                            }
                            rect.X += 1;
                            rect.Y += 1;
                            rect.Width -= 2;
                            rect.Height -= 2;

                            if (GraphicsEngine.Current.Engine != null)
                            {
                                using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(rect.Width, rect.Height))
                                using (var canvas = bitmap.CreateCanvas())
                                {
                                    new SymbolPreview(null).Draw(canvas,
                                        new GraphicsEngine.CanvasRectangle(0, 0, rect.Width, rect.Height),
                                        //item.Symbol?.Clone(null) as ISymbol,
                                        item.Symbol,
                                        false);
                                    e.Graphics.DrawImage(bitmap.ToGdiBitmap(), new Point(rect.X, rect.Y));
                                }
                            }
                        }
                    }
                    catch { }
                    index++;
                }
            }
        }

        private void btnAdd_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol == null)
            {
                return;
            }

            if (PlugInManager.IsPlugin(_selectedSymbol))
            {
                PlugInManager comMan = new PlugInManager();
                ISymbol newSymbol = (ISymbol)comMan.CreateInstance(PlugInManager.PlugInID(_selectedSymbol));

                if (newSymbol != null)
                {
                    AddSymbol(newSymbol);
                }
                if (SelectedSymbolChanged != null)
                {
                    SelectedSymbolChanged(_selectedSymbol);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (_selectedSymbol == null)
            {
                return;
            }

            this.RemoveSymbol(_selectedSymbol);
            lbSymbols.SelectedItem = lbSymbols.Items[0];
            if (SelectedSymbolChanged != null)
            {
                SelectedSymbolChanged(_selectedSymbol);
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (_selectedSymbol == null)
            {
                return;
            }

            this.MoveDownSymbol(_selectedSymbol);
            if (SelectedSymbolChanged != null)
            {
                SelectedSymbolChanged(_selectedSymbol);
            }

            buildList();
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (_selectedSymbol == null)
            {
                return;
            }

            this.MoveUpSymbol(_selectedSymbol);
            if (SelectedSymbolChanged != null)
            {
                SelectedSymbolChanged(_selectedSymbol);
            }

            buildList();
        }

        private void lbSymbols_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbSymbols.SelectedItem == null)
            {
                return;
            }

            _selectedSymbol = ((SymbolCollectionItem)lbSymbols.SelectedItem).Symbol;
            toolStrip2.Visible = (_selectedSymbol is IPointSymbol) || (_selectedSymbol is ITextSymbol);
            if (toolStrip2.Visible)
            {
                if (_selectedSymbol is ISymbolTransformation)
                {
                    txtXOffset.Text = ((ISymbolTransformation)_selectedSymbol).HorizontalOffset.ToString();
                    txtYOffset.Text = ((ISymbolTransformation)_selectedSymbol).VerticalOffset.ToString();
                    txtRotation.Text = ((ISymbolTransformation)_selectedSymbol).Angle.ToString();
                }
            }
            lbSymbols.Refresh();

            if (SelectedSymbolChanged != null)
            {
                SelectedSymbolChanged(_selectedSymbol);
            }

            DrawPreview();
        }

        private void lbSymbols_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            SymbolCollectionItem item = null;
            for (int i = 0; i < lbSymbols.Items.Count; i++)
            {
                Rectangle rect = lbSymbols.GetItemRectangle(i);

                if (rect.Y <= e.Y && (rect.Y + rect.Height) >= e.Y)
                {
                    item = (SymbolCollectionItem)lbSymbols.Items[i];
                    break;
                }
            }
            if (item == null)
            {
                return;
            }

            _mouseDownItem = item;

            if (e.X < 20)
            {
                item.Visible = !item.Visible;
                DrawPreview();
            }
        }

        private int _mX = 0, _mY = 0;
        private Rectangle _rect = new Rectangle(-1, -1, -1, -1);
        private SymbolCollectionItem _mouseOverItem = null;
        private SymbolCollectionItem _mouseDownItem = null;
        private void lbSymbols_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _mX = e.X;
            _mY = e.Y;

            if (e.Button == MouseButtons.Left && lbSymbols.SelectedIndices.Count > 0 && _mouseDownItem != null)
            {
                SymbolCollectionItem item = null;
                Rectangle rect = new Rectangle(0, 0, 0, 0);

                for (int i = 0; i < lbSymbols.Items.Count; i++)
                {
                    rect = lbSymbols.GetItemRectangle(i);
                    //if(rect==null) continue;

                    if (rect.Y <= e.Y && (rect.Y + rect.Height) >= e.Y)
                    {
                        item = (SymbolCollectionItem)lbSymbols.Items[i];
                        break;
                    }
                }
                if (item == null)
                {
                    return;
                }

                _mouseOverItem = item;

                System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(lbSymbols.Handle);

                Pen pen = new Pen(Color.Blue, 2);
                gr.DrawLine(pen, rect.Left, rect.Top, rect.Width, rect.Top);

                if (_rect != rect)
                {
                    pen.Color = lbSymbols.BackColor;
                    gr.DrawLine(pen, _rect.Left, _rect.Top, _rect.Width, _rect.Top);
                    _rect = rect;
                }
                pen.Dispose(); pen = null;
                gr.Dispose(); gr = null;
            }
        }

        private void lbSymbols_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (_mouseOverItem != null &&
                _mouseDownItem != null &&
                _mouseOverItem != _mouseDownItem)
            {
                _symbol.RemoveSymbol(_mouseDownItem.Symbol);
                _symbol.InsertBefore(_mouseDownItem.Symbol, _mouseOverItem.Symbol, _mouseDownItem.Visible);
                buildList();

                _mouseOverItem = _mouseDownItem = null;
            }
        }

        public void DrawPreview()
        {
            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromHwnd(panelPreview.Handle))
            {
                Rectangle rect = new Rectangle(0, 0, panelPreview.Width, panelPreview.Height);

                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    gr.FillRectangle(brush, rect);
                }

                if (GraphicsEngine.Current.Engine != null)
                {
                    using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(rect.Width, rect.Height))
                    using (var canvas = bitmap.CreateCanvas())
                    {
                        new SymbolPreview(null).Draw(canvas, rect.ToCanvasRectangle(), _symbol?.Clone(null) as ISymbol, false);
                        gr.DrawImage(bitmap.ToGdiBitmap(), new Point(rect.X, rect.Y));
                    }
                }

                using (Pen pen = new Pen(Color.Gray, 0))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    gr.DrawLine(pen, 0, rect.Height / 2, rect.Width, rect.Height / 2);
                    gr.DrawLine(pen, rect.Width / 2, 0, rect.Width / 2, rect.Height);
                }
            }
        }

        private void btnLeft_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolPositioningUI)
            {
                ((ISymbolPositioningUI)_selectedSymbol).HorizontalMove(-1);
                txtXOffset.Text = ((ISymbolTransformation)_selectedSymbol).HorizontalOffset.ToString();
                DrawPreview();
            }
            else if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).HorizontalOffset -= 1;
                txtXOffset.Text = ((ISymbolTransformation)_selectedSymbol).HorizontalOffset.ToString();
                DrawPreview();
            }
        }

        private void btn_Up_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolPositioningUI)
            {
                ((ISymbolPositioningUI)_selectedSymbol).VertiacalMove(-1);
                txtYOffset.Text = ((ISymbolTransformation)_selectedSymbol).VerticalOffset.ToString();
                DrawPreview();
            }
            else if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).VerticalOffset -= 1;
                txtYOffset.Text = ((ISymbolTransformation)_selectedSymbol).VerticalOffset.ToString();
                DrawPreview();
            }
        }

        private void btnRight_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolPositioningUI)
            {
                ((ISymbolPositioningUI)_selectedSymbol).HorizontalMove(1);
                txtXOffset.Text = ((ISymbolTransformation)_selectedSymbol).HorizontalOffset.ToString();
                DrawPreview();
            }
            else if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).HorizontalOffset += 1;
                txtXOffset.Text = ((ISymbolTransformation)_selectedSymbol).HorizontalOffset.ToString();
                DrawPreview();
            }
        }

        private void btn_Down_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolPositioningUI)
            {
                ((ISymbolPositioningUI)_selectedSymbol).VertiacalMove(1);
                txtYOffset.Text = ((ISymbolTransformation)_selectedSymbol).VerticalOffset.ToString();
                DrawPreview();
            }
            else if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).VerticalOffset += 1;
                txtYOffset.Text = ((ISymbolTransformation)_selectedSymbol).VerticalOffset.ToString();
                DrawPreview();
            }
        }

        private void btnRotate1_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).Angle += (float)5;
                txtRotation.Text = ((ISymbolTransformation)_selectedSymbol).Angle.ToString();
                DrawPreview();
            }
        }

        private void btnRotate2_Click(object sender, System.EventArgs e)
        {
            if (_selectedSymbol is ISymbolTransformation)
            {
                ((ISymbolTransformation)_selectedSymbol).Angle -= (float)5;
                txtRotation.Text = ((ISymbolTransformation)_selectedSymbol).Angle.ToString();
                DrawPreview();
            }
        }

        private void txtXOffset_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_selectedSymbol is ISymbolTransformation)
                {
                    if (txtXOffset.Text == "" || txtXOffset.Text == "-")
                    {
                        ((ISymbolTransformation)_selectedSymbol).HorizontalOffset = 0;
                    }
                    else
                    {
                        ((ISymbolTransformation)_selectedSymbol).HorizontalOffset = (float)Convert.ToDouble(txtXOffset.Text);
                    }

                    DrawPreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtYOffset_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_selectedSymbol is ISymbolTransformation)
                {
                    if (txtYOffset.Text == "" || txtYOffset.Text == "-")
                    {
                        ((ISymbolTransformation)_selectedSymbol).VerticalOffset = 0;
                    }
                    else
                    {
                        ((ISymbolTransformation)_selectedSymbol).VerticalOffset = (float)Convert.ToDouble(txtYOffset.Text);
                    }

                    DrawPreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtRotation_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_selectedSymbol is ISymbolTransformation)
                {
                    if (txtRotation.Text == "" || txtRotation.Text == "-")
                    {
                        ((ISymbolTransformation)_selectedSymbol).Angle = 0;
                    }
                    else
                    {
                        ((ISymbolTransformation)_selectedSymbol).Angle = (float)Convert.ToDouble(txtRotation.Text);
                    }

                    DrawPreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            DrawPreview();
        }

        private void panelPreview_Paint_1(object sender, PaintEventArgs e)
        {
            DrawPreview();
        }
    }
}
