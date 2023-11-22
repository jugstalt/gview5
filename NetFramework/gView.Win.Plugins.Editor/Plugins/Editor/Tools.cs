using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Editor.Core;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.IO;
using gView.Framework.LinAlg;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.Editor
{
    [gView.Framework.system.RegisterPlugIn("FE2AE24C-73F8-4da3-BBC7-45C2FCD3FE75", Obsolete = true)]
    public class EditorMenu : ITool, IToolItem
    {
        ToolStripMenuItem _startEditing = new ToolStripMenuItem();
        ToolStripMenuItem _stopEditing = new ToolStripMenuItem();
        ToolStripMenuItem _saveEdits = new ToolStripMenuItem();

        #region ITool Members

        public string Name
        {
            get { return "Editor Menu"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {

        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Members

        public System.Windows.Forms.ToolStripItem ToolItem
        {
            get
            {
                ToolStripDropDownButton button = new ToolStripDropDownButton();
                button.Text = "Editor";

                _startEditing.Text = "Start Editing";
                _stopEditing.Text = "Stop Editing";
                _saveEdits.Text = "Save Edits";

                button.DropDownItems.Add(_startEditing);
                button.DropDownItems.Add(_stopEditing);
                button.DropDownItems.Add(new ToolStripSeparator());
                button.DropDownItems.Add(_saveEdits);

                return button;
            }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("19396559-C13C-486c-B5F7-73DD5B12D5A8", Obsolete = true)]
    public class TaskText : ITool, IToolItem
    {

        #region ITool Members

        public string Name
        {
            get { return "Editor Task"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {

        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Members

        public ToolStripItem ToolItem
        {
            get
            {
                return new ToolStripLabel(LocalizedResources.GetResString("String.Task", "Task"));
            }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("9B7D5E0E-88A5-40e2-977B-8A2E21875221", Obsolete = true)]
    public class TaskCombo : ITool, IToolItem, IPersistable, IToolItemLabel
    {
        private IMapDocument _doc = null;
        private Module _module = null;
        private ToolStripComboBox _combo = null;

        public TaskCombo()
        {
            _combo = new ToolStripComboBox();
            _combo.DropDownStyle = ComboBoxStyle.DropDownList;
            _combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);
        }

        #region ITool Members

        public string Name
        {
            get { return "Editor Task Combobox"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }

                if (_module != null)
                {
                    _module.OnChangeSelectedFeature += new Module.OnChangeSelectedFeatureEventHandler(_module_OnChangeSelectedFeature);
                }
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Members

        public ToolStripItem ToolItem
        {
            get
            {
                return _combo;
            }
        }

        #endregion

        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_module == null || !(_combo.SelectedItem is ItemClass))
            {
                return;
            }

            _module.ActiveEditTask = ((ItemClass)_combo.SelectedItem).Task;

            if (_doc != null && _doc.Application is IGUIApplication)
            {
                ((IGUIApplication)_doc.Application).ValidateUI();
            }
        }

        void _module_OnChangeSelectedFeature(Module sender, IFeature feature)
        {
            if (sender == null)
            {
                return;
            }

            Module.EditTask aktive = sender.ActiveEditTask;

            _combo.Items.Clear();
            if (sender.SelectedEditLayer == null)
            {
                return;
            }

            if (Bit.Has(sender.SelectedEditLayer.Statements, EditStatements.INSERT))
            {
                _combo.Items.Add(new ItemClass(Module.EditTask.CreateNewFeature));
            }

            if (Bit.Has(sender.SelectedEditLayer.Statements, EditStatements.UPDATE) ||
                Bit.Has(sender.SelectedEditLayer.Statements, EditStatements.DELETE))
            {
                _combo.Items.Add(new ItemClass(Module.EditTask.ModifyFeature));
            }

            foreach (ItemClass item in _combo.Items)
            {
                if (item.Task == aktive)
                {
                    _combo.SelectedItem = item;
                }
            }

            if (_combo.SelectedIndex == -1 && _combo.Items.Count > 0)
            {
                _combo.SelectedIndex = 0;
            }
        }

        #region ItemClasses
        private class ItemClass
        {
            private Module.EditTask _task;
            private string _text = String.Empty;

            public ItemClass(Module.EditTask task)
            {
                _task = task;

                switch (_task)
                {
                    case Module.EditTask.CreateNewFeature:
                        _text = LocalizedResources.GetResString("String.CreateNewFeatures", "Create New Features");
                        break;
                    case Module.EditTask.ModifyFeature:
                        _text = LocalizedResources.GetResString("String.ModifyFeatures", "Modify Features");
                        break;
                }
            }

            public Module.EditTask Task
            {
                get { return _task; }
            }

            public override string ToString()
            {
                return _text;
            }
        }
        #endregion

        #region IPersistable Member

        public void Load(IPersistStream stream)
        {
            try
            {
                _combo.SelectedIndex = (int)stream.Load("selectedIndex", 0);
            }
            catch { }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("selectedIndex", _combo.SelectedIndex);
        }

        #endregion

        #region IToolItemLabel Member

        public string Label
        {
            get { return LocalizedResources.GetResString("String.Task", "Task"); }
        }

        public ToolItemLabelPosition LabelPosition
        {
            get { return ToolItemLabelPosition.top; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("784148EB-04EA-413d-B11A-1A0F9A7EA4A0", Obsolete = true)]
    public class TargetText : ITool, IToolItem
    {
        #region ITool Member

        public string Name
        {
            get { return "Editor.Target"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return String.Empty; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {

        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get
            {
                return new ToolStripLabel(LocalizedResources.GetResString("String.Edit", "Edit"));
            }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("3C8A7ABC-B535-43d8-8F2D-B220B298CB17", Obsolete = true)]
    public class TargetCombo : ITool, IToolItem, IPersistable, IToolItemLabel
    {
        private IMapDocument _doc = null;
        private ToolStripComboBox _combo;
        private Module _module = null;

        public TargetCombo()
        {
            _combo = new ToolStripComboBox();
            ((ToolStripComboBox)_combo).DropDownStyle = ComboBoxStyle.DropDownList;
            ((ToolStripComboBox)_combo).Width = 300;
        }

        #region ITool Member

        public string Name
        {
            get { return "Editor.TargetCombo"; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return String.Empty; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;

                if (_doc is IMapDocumentEvents)
                {
                    ((IMapDocumentEvents)_doc).AfterSetFocusMap += new AfterSetFocusMapEvent(_doc_AfterSetFocusMap);
                }

                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;

                    if (_module != null)
                    {
                        _module.OnEditLayerCollectionChanged += new OnEditLayerCollectionChangedEventHandler(_module_OnEditLayerCollectionChanged);
                    }
                }

                _combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);
                FillCombo();
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get
            {
                return _combo as ToolStripComboBox;
            }
        }

        #endregion

        #region MapDocument events
        void _doc_AfterSetFocusMap(gView.Framework.Carto.IMap map)
        {
            FillCombo();
        }
        #endregion

        #region Module Events
        void _module_OnEditLayerCollectionChanged(object sender)
        {
            FillCombo();
        }
        #endregion

        #region Helper
        private delegate void FillComboCallback();
        public void FillCombo()
        {
            //return;
            if (_combo == null || _combo.Owner == null)
            {
                return;
            }

            if (_combo.Owner.InvokeRequired)
            {
                FillComboCallback d = new FillComboCallback(FillCombo);
                _combo.Owner.BeginInvoke(d);
                return;
            }

            string selectedName = (_combo.SelectedItem is FeatureClassesItem) ? _combo.SelectedItem.ToString() : "";

            _combo.SelectedIndexChanged -= new EventHandler(combo_SelectedIndexChanged);
            _combo.Items.Clear();

            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.TOC == null ||
                _module == null || _module.EditLayers == null)
            {
                return;
            }

            IMap map = _doc.FocusMap;

            foreach (IEditLayer editLayer in _module.EditLayers)
            {
                if (editLayer == null ||
                    editLayer.FeatureLayer == null ||
                    editLayer.FeatureLayer.FeatureClass == null ||
                    editLayer.Statements == EditStatements.NONE)
                {
                    continue;
                }

                if (map[editLayer.FeatureLayer] == null)
                {
                    continue;
                }
                // Besser layer als layer.Class verwendenden, weil Class von mehrerenen Layern
                // verwendet werden kann zB bei gesplitteten Layern...
                //ITOCElement tocElement = map.TOC.GetTOCElement(editLayer.FeatureLayer.FeatureClass);
                ITocElement tocElement = map.TOC.GetTOCElement(editLayer.FeatureLayer);
                if (tocElement == null)
                {
                    continue;
                }

                _combo.Items.Add(new FeatureClassesItem(
                    editLayer,
                    tocElement.Name));
                if (tocElement.Name == selectedName)
                {
                    _combo.SelectedIndex = _combo.Items.Count - 1;
                }
            }
            _combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);

            if (_persistedSelIndex >= -0 && _persistedSelIndex < _combo.Items.Count)
            {
                _combo.SelectedIndex = _persistedSelIndex;
            }

            if (_combo.SelectedIndex == -1 && _combo.Items.Count > 0)
            {
                _combo.SelectedIndex = 0;
            }

            _persistedSelIndex = -1;

            if (_combo.SelectedIndex == -1 && _module != null)
            {
                _module.SelectedEditLayer = null;
            }

            using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1))
            {
                using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bm))
                {
                    _combo.DropDownWidth = _combo.Size.Width;
                    foreach (object obj in _combo.Items)
                    {
                        System.Drawing.SizeF size = gr.MeasureString(obj.ToString(), _combo.Font);

                        if (size.Width + 20 > _combo.DropDownWidth)
                        {
                            _combo.DropDownWidth = (int)size.Width + 20;
                        }
                    }
                }
            }
        }

        #endregion

        #region ItemClasses
        private class FeatureClassesItem
        {
            private string _name;
            private IEditLayer _eLayer;

            public FeatureClassesItem(IEditLayer eLayer, string name)
            {
                _eLayer = eLayer;
                _name = name;
            }

            public IEditLayer EditLayer
            {
                get { return _eLayer; }
            }
            public override string ToString()
            {
                return _name;
            }
        }
        #endregion

        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_module != null)
            {
                if (_combo.SelectedItem is FeatureClassesItem)
                {
                    FeatureClassesItem item = (FeatureClassesItem)_combo.SelectedItem;
                    _module.SelectedEditLayer = item.EditLayer;
                }
                else
                {
                    _module.SelectedEditLayer = null;
                }
            }
        }

        #region IPersistable Member

        private int _persistedSelIndex = -1;
        public void Load(IPersistStream stream)
        {
            _persistedSelIndex = (int)stream.Load("selectedIndex", 0);
            if (_persistedSelIndex < _combo.Items.Count)
            {
                _combo.SelectedIndex = _persistedSelIndex;
            }
        }

        public void Save(IPersistStream stream)
        {
            stream.Save("selectedIndex", _combo.SelectedIndex);
        }

        #endregion

        #region IToolItemLabel Member

        public string Label
        {
            get { return LocalizedResources.GetResString("String.Edit", "Edit"); }
        }

        public ToolItemLabelPosition LabelPosition
        {
            get { return ToolItemLabelPosition.top; }
        }

        #endregion
    }

    //public class TargetComboWpf : TargetCombo
    //{
    //    public TargetComboWpf()
    //    {
    //        _combo = new System.Windows.Controls.ComboBox();

    //    }
    //}

    [gView.Framework.system.RegisterPlugIn("91392106-2C28-429c-8100-1E4E927D521C", Obsolete = true)]
    public class EditTool : gView.Framework.Snapping.Core.SnapTool, ITool, IToolWindow, IToolMouseActions, IToolKeyActions, IToolContextMenu
    {
        private IMapDocument _doc = null;
        private Module _module = null;
        private ContextMenuStrip _addVertexMenu, _removeVertexMenu, _currentMenu = null;

        public EditTool()
        {
            _addVertexMenu = new ContextMenuStrip();
            ToolStripMenuItem addvertex = new ToolStripMenuItem(
                Globalisation.GetResString("AddVertex"));
            addvertex.Click += new EventHandler(addvertex_Click);
            _addVertexMenu.Items.Add(addvertex);

            _removeVertexMenu = new ContextMenuStrip();
            ToolStripMenuItem removevertex = new ToolStripMenuItem(
                Globalisation.GetResString("RemoveVertex"));
            removevertex.Click += new EventHandler(removevertex_Click);
            _removeVertexMenu.Items.Add(removevertex);
        }

        #region ITool Members

        public string Name
        {
            get { return "Editor.ModifyFeature"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module == null)
                {
                    return false;
                }

                switch (_module.ActiveEditTask)
                {
                    case Module.EditTask.None:
                        return false;
                    case Module.EditTask.CreateNewFeature:
                        if (_module.Sketch != null)
                        {
                            return true;
                        }

                        break;
                    case Module.EditTask.ModifyFeature:
                        return true;
                }
                return false;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.ModifyFeature", "Modify Feature/Sketch"); }
        }

        public ToolType toolType
        {
            get { return ToolType.userdefined; }
        }

        public object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.edit_modify;
            }
        }

        override public void OnCreate(object hook)
        {
            base.OnCreate(hook);
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                    //_module.OnChangeEditTask += new Module.OnChangeEditTaskEventHandler(Module_OnChangeEditTask);
                }

            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolWindow Member

        public IDockableWindow ToolWindow
        {
            get
            {
                if (_module != null)
                {
                    return _module.AttributeEditorWindow;
                }

                return null;
            }
        }

        #endregion

        #region IToolMouseActions Member
        double _oX = 0, _oY = 0;
        bool _mousePressed = false;
        IHitPositions _hit = null;

        public void MouseDown(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_module == null || _doc == null || _doc.FocusMap == null || !(_doc.Application is IMapApplication))
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _mousePressed = true;

            switch (_module.ActiveEditTask)
            {
                case Module.EditTask.None:
                case Module.EditTask.CreateNewFeature:
                    return;
            }
            _oX = world.X;
            _oY = world.Y;

            //if (_hit == null)
            //{
            //    RemoveSketch();
            //}

            if (/*_module.Sketch == null*/_hit == null)
            {
                #region Query Feature
                if (_module.SelectedEditLayer == null ||
                    _module.SelectedEditLayer.FeatureLayer == null ||
                    _module.SelectedEditLayer.FeatureLayer.FeatureClass == null)
                {
                    return;
                }

                IFeatureClass fc = _module.SelectedEditLayer.FeatureLayer.FeatureClass;

                double tolerance = 5.0;
                double tol = tolerance * _doc.FocusMap.Display.MapScale / (96 / 0.0254);  // [m]
                if (_doc.FocusMap.Display.SpatialReference != null &&
                     _doc.FocusMap.Display.SpatialReference.SpatialParameters.IsGeographic)
                {
                    tol = (180.0 * tol / Math.PI) / 6370000.0;
                }
                IEnvelope envelope = new Envelope(_oX - tol / 2.0, _oY - tol / 2.0, _oX + tol / 2.0, _oY + tol / 2.0);

                SpatialFilter filter = new SpatialFilter();
                filter.Geometry = envelope;
                if (_doc.FocusMap.Display.SpatialReference != null)
                {
                    filter.FilterSpatialReference = _doc.FocusMap.Display.SpatialReference.Clone() as ISpatialReference;
                    filter.FeatureSpatialReference = _doc.FocusMap.Display.SpatialReference.Clone() as ISpatialReference;
                }
                filter.SubFields = "*";

                IFeature feature = null;
                using (IFeatureCursor cursor = fc.GetFeatures(filter).Result)
                {
                    if (cursor == null)
                    {
                        return;
                    }

                    feature = cursor.NextFeature().Result;
                    if (feature != null)
                    {
                        if (_module.Feature == null || _module.Feature.OID != feature.OID)
                        {
                            RemoveSketch();
                            _module.Feature = feature;
                            return;
                        }
                    }
                }
                #endregion
            }
        }

        public void MouseUp(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {
            _mousePressed = false;

            if (_hit != null && _doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }

        public void MouseClick(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        public void MouseDoubleClick(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        public void MouseMove(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_module == null || _doc == null || _doc.FocusMap == null)
            {
                return;
            }

            double x1 = world.X, y1 = world.Y;
            _displayPoint = new Point(x1, y1);

            if (!_mousePressed && _module.Sketch != null)
            {
                _currentMenu = null;
                _hit = _module.Sketch.HitTest(_doc.FocusMap.Display, _displayPoint);
                if (_hit != null)
                {
                    if (_doc.Application is IGUIApplication)
                    {
                        ((IGUIApplication)_doc.Application).SetCursor(_hit.Cursor as Cursor);
                    }

                    if (gView.Plugins.Editor.EditSketch.HitPosition.VertexCursor.Equals(_hit?.Cursor))
                    {
                        _currentMenu = _removeVertexMenu;
                    }
                    else if (_hit.HitID == -1)
                    {
                        _currentMenu = _addVertexMenu;
                    }
                }
                else
                {
                    if (_doc.Application is IGUIApplication)
                    {
                        ((IGUIApplication)_doc.Application).SetCursor(Cursors.Default);
                    }
                }
            }
            else if (_mousePressed && _hit != null)
            {
                if (_module.Sketch != null)
                {
                    _module.Sketch.Design(display, _hit, world.X, world.Y);
                }

                _module.RedrawhSketch();
            }
        }

        public void MouseWheel(gView.Framework.Carto.IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        #endregion

        #region IToolKeyActions Member

        public void KeyDown(gView.Framework.Carto.IDisplay display, KeyEventArgs e)
        {

        }

        public void KeyPress(gView.Framework.Carto.IDisplay display, KeyPressEventArgs e)
        {

        }

        public void KeyUp(gView.Framework.Carto.IDisplay display, KeyEventArgs e)
        {

        }

        #endregion

        #region Helper
        private void RemoveSketch()
        {
            if (_module == null || _doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            _module.Feature = null;
        }
        #endregion

        public override void Snap(ref double X, ref double Y)
        {
            if (_hit != null && _mousePressed)
            {
                base.Snap(ref X, ref Y);
            }
        }

        public override bool ShowSnapMarker
        {
            get
            {
                if (_hit != null && _mousePressed)
                {
                    return true;
                }

                return false;
            }
        }

        #region IToolContextMenu Member

        public ContextMenuStrip ContextMenu
        {
            get { return _currentMenu; }
        }

        #endregion

        void removevertex_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null ||
                _hit == null || _module.Sketch == null ||
                !_hit.Cursor.Equals(gView.Plugins.Editor.EditSketch.HitPosition.VertexCursor))
            {
                return;
            }

            _module.Sketch.RemoveVertex(_doc.FocusMap.Display, _hit.HitID);

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
        private IPoint _displayPoint = null;
        void addvertex_Click(object sender, EventArgs e)
        {
            if (_doc == null || _doc.FocusMap == null ||
                _hit == null || _module.Sketch == null ||
                _displayPoint == null)
            {
                return;
            }

            _module.Sketch.AddVertex(_doc.FocusMap.Display,
                new Point(_displayPoint.X, _displayPoint.Y));

            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("3B64107F-00C8-4f4a-B781-163FE9DA2D4B", Obsolete = true)]
    public class EditPenTool : gView.Framework.Snapping.Core.SnapTool, ITool, IToolMouseActions, IToolMouseActions2, IToolKeyActions, IToolContextMenu
    {
        protected IMapDocument _doc = null;
        internal Module _module = null;
        private bool _finished = false;
        private ParentPenToolMenuItem _penToolItem;
        private System.Drawing.Image _image;

        public EditPenTool()
        {
            _image = global::gView.Win.Plugins.Editor.Properties.Resources.edit_blue;
            _penToolItem = new ParentPenToolMenuItem();
            _penToolItem.SelectedPenToolChanged += new ParentPenToolMenuItem.SelectedPenToolChangedEventHandler(penToolItem_SelectedPenToolChanged);
        }

        #region ITool Members

        virtual public string Name
        {
            get { return "Editor.NewFeature"; }
        }

        virtual public bool Enabled
        {
            get
            {
                if (_module == null)
                {
                    return false;
                }

                switch (_module.ActiveEditTask)
                {
                    case Module.EditTask.None:
                        return false;
                    case Module.EditTask.CreateNewFeature:
                        return true;
                    case Module.EditTask.ModifyFeature:
                        if (_module.Feature != null &&
                            _module.Feature.OID > 0)
                        {
                            return true;
                        }

                        break;
                }
                return false;
            }
        }

        virtual public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.NewFeature", "New Feature/Vertex"); }
        }

        virtual public ToolType toolType
        {
            get { return ToolType.click; }
        }

        virtual public object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.edit_blue;
            }
        }

        override public void OnCreate(object hook)
        {
            base.OnCreate(hook);
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                    if (_module != null)
                    {
                        _module.OnChangeSelectedFeature += new Module.OnChangeSelectedFeatureEventHandler(Module_OnChangeSelectedFeature);
                    } ((IMapApplication)_doc.Application).ActiveMapToolChanged += new ActiveMapToolChangedEvent(EditNew_ActiveMapToolChanged);

                    _penToolItem.OnCreate(_module);
                }

            }
        }

        virtual public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolMouseActions Member
        private bool _mousePressed = false;
        protected double _X, _Y;

        virtual public void MouseDown(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _module == null || _module.FeatureClass == null)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _mousePressed = true;

            if (_module.Feature == null ||
                _module.Sketch == null)
            {
                _module.CreateStandardFeature();
                _finished = false;
            }
            if (_finished || _module.Sketch == null)
            {
                return;
            }

            CalcXY(e.X, e.Y, world);
            if (!double.IsNaN(_X) && !double.IsNaN(_Y))
            {
                _module.Sketch.AddPoint(new Point(_X, _Y));
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                    Thread.Sleep(200);
                }
            }

            _penToolItem.PerformClick();

        }

        virtual public void MouseUp(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (!_mousePressed)
            {
                return;
            }

            _mousePressed = false;
        }

        virtual public void MouseClick(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        virtual public void MouseDoubleClick(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        virtual public void MouseMove(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _module == null)
            {
                return;
            }

            if (_finished)
            {
                return;
            }

            CalcXY(e.X, e.Y, world);
            DrawMover();
        }

        virtual public void MouseWheel(IDisplay display, MouseEventArgs e, IPoint world)
        {

        }

        #endregion

        #region IToolMouseActions2 Member

        public void BeforeShowContext(IDisplay display, MouseEventArgs e, IPoint world)
        {
            if (_penToolItem != null &&
                _module != null &&
                _module.MapDocument != null &&
                _module.MapDocument.FocusMap != null &&
                _module.MapDocument.FocusMap.Display != null)
            {
                double x = e.X, y = e.Y;
                _module.MapDocument.FocusMap.Display.Image2World(ref x, ref y);
                _penToolItem.ContextVertex = _penToolItem.CalcPoint(e.X, e.Y, world);
                _penToolItem.MouseWorldPoint = new Point(x, y);
                _penToolItem.ContextPoint = world;
            }
        }

        #endregion

        #region IToolKeyActions Member

        virtual public void KeyDown(IDisplay display, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                //_finished = true;
                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                    ((IMapApplication)_doc.Application).ActiveTool = ((IMapApplication)_doc.Application).Tool(new Guid("91392106-2C28-429c-8100-1E4E927D521C"));
                }
            }
        }

        virtual public void KeyPress(IDisplay display, KeyPressEventArgs e)
        {

        }

        virtual public void KeyUp(IDisplay display, KeyEventArgs e)
        {

        }

        #endregion

        #region Events Handler
        void Module_OnChangeSelectedFeature(Module sender, IFeature feature)
        {
            if (_module == null || _doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null || _doc.FocusMap.Display.GraphicsContainer == null)
            {
                return;
            }

            if (_doc.Application is IMapApplication &&
               ((IMapApplication)_doc.Application).ActiveTool == this)
            {

            }
        }

        void EditNew_ActiveMapToolChanged(ITool OldTool, ITool NewTool)
        {
            //if (OldTool != this && NewTool == this && _module != null &&
            //    _doc!=null && _doc.Application is IMapApplication)
            //{
            //    TargetCombo combo = ((IMapApplication)_doc.Application).Tool(new Guid("3C8A7ABC-B535-43d8-8F2D-B220B298CB17")) as TargetCombo;
            //    List<IFeatureClass> fcs = combo.SelectedFeatureclasses;
            //    if (fcs == null || fcs.Count != 1)
            //    {
            //        _module.SetFeatureClassAndFeature(null, null);
            //    }
            //    else
            //    {
            //        _module.SetFeatureClassAndFeature(fcs[0], CreateFeature());
            //    }
            //}
            if (OldTool == this && NewTool != this)
            {
                if (_doc != null && _doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                }
            }
        }
        #endregion

        #region HelperClasses
        private class EditPartNumberMenuItem : ToolStripMenuItem
        {
            private EditSketch _sketch;
            private int _partNr;

            public EditPartNumberMenuItem(EditSketch sketch, int partNr, string text)
            {
                _sketch = sketch;
                _partNr = partNr;

                base.Text = text;
                base.Click += new EventHandler(EditPartNumberMenuItem_Click);

                if (sketch.ActivePartNumber == partNr)
                {
                    base.Checked = true;
                }
            }

            void EditPartNumberMenuItem_Click(object sender, EventArgs e)
            {
                if (_sketch != null)
                {
                    _sketch.ActivePartNumber = _partNr;
                }
            }
        }
        #endregion

        #region IToolContextMenu Member

        virtual public ContextMenuStrip ContextMenu
        {
            get
            {
                ContextMenuStrip strip = new ContextMenuStrip();

                if (_module != null && _module.Sketch != null && _module.Sketch.PartCount != 0)
                {
                    ToolStripMenuItem editPartItem = new ToolStripMenuItem(
                        Globalisation.GetResString("EditPart"));
                    for (int i = 0; i < _module.Sketch.PartCount; i++)
                    {
                        editPartItem.DropDownItems.Add(new EditPartNumberMenuItem(
                            _module.Sketch, i, (i + 1).ToString()));
                    }
                    editPartItem.DropDownItems.Add(new EditPartNumberMenuItem(
                        _module.Sketch, _module.Sketch.PartCount,
                        Globalisation.GetResString("New")));
                    strip.Items.Add(editPartItem);

                    if (_module.Sketch.Part.PointCount > 2)
                    {
                        ToolStripMenuItem closePart = new ToolStripMenuItem(
                            Globalisation.GetResString("ClosePart"));
                        closePart.Click += new EventHandler(closePart_Click);
                        strip.Items.Add(closePart);
                    }
                }

                bool first = true;
                foreach (ToolStripItem item in _penToolItem.MenuItems)
                {
                    if (first && strip.Items.Count != 0)
                    {
                        strip.Items.Add(new ToolStripSeparator());
                    }
                    strip.Items.Add(item);
                    first = false;
                }

                return strip;
            }
        }

        #endregion

        virtual protected void CalcXY(int mouseX, int mouseY, IPoint world)
        {
            //_X = world.X;
            //_Y = world.Y;

            IPoint point = _penToolItem.CalcPoint(mouseX, mouseY, world);
            if (point != null)
            {
                _X = point.X;
                _Y = point.Y;
            }
            else
            {
                _X = _Y = double.NaN;
            }
        }
        protected void DrawMover()
        {
            if (_penToolItem.DrawMover)
            {
                if (!double.IsNaN(_X) && !double.IsNaN(_Y))
                {
                    _module.Mover = new Point(_X, _Y);
                }
            }
        }

        void penToolItem_SelectedPenToolChanged(object sender, IPenTool penTool)
        {
            if (_doc == null || !(_doc.Application is IMapApplication) ||
                ((IMapApplication)_doc.Application).StatusBar == null)
            {
                return;
            }

            if (penTool != null && penTool.Image is System.Drawing.Image)
            {
                _image = (System.Drawing.Image)penTool.Image;
            }
            else
            {
                _image = global::gView.Win.Plugins.Editor.Properties.Resources.edit_blue;
            }

            ((IMapApplication)_doc.Application).StatusBar.Image = _image;
        }

        void closePart_Click(object sender, EventArgs e)
        {
            if (_module != null && _module.Sketch != null)
            {
                _module.Sketch.ClosePart();
            }

            if (_doc != null && _doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);
                Thread.Sleep(200);
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("4F4A6AA1-89A6-498c-819A-0E52EF9AEA61", Obsolete = true)]
    public class EditOrthoPenTool : EditPenTool
    {
        #region ITool
        public override string Name
        {
            get
            {
                return "Edit.Ortho.PenTool";
            }
        }

        public override object Image
        {
            get
            {
                return global::gView.Win.Plugins.Editor.Properties.Resources.ortho;
            }
        }
        #endregion

        protected override void CalcXY(int mouseX, int mouseY, IPoint world)
        {
            if (_module == null)
            {
                return;
            }

            EditSketch sketch = _module.Sketch;
            IPointCollection part = (sketch != null) ? sketch.Part : null;
            if (sketch == null || part == null || part.PointCount < 2)
            {
                base.CalcXY(mouseX, mouseY, world);
            }
            else
            {
                IPoint p1 = part[part.PointCount - 2];
                IPoint p2 = part[part.PointCount - 1];

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double alpha = Math.Atan2(dy, dx);

                IPoint resP1 = null;
                IPoint resP2 = null;

                Point r = new Point(Math.Sin(alpha), -Math.Cos(alpha));
                Point r_ = new Point(-r.Y, r.X);

                #region Ortho
                LinearEquation2 linarg = new LinearEquation2(
                    world.X - p2.X,
                    world.Y - p2.Y,
                    r.X, r_.X,
                    r.Y, r_.Y);

                if (linarg.Solve())
                {
                    double t1 = linarg.Var1;
                    double t2 = linarg.Var2;

                    resP1 = new Point(p2.X + r.X * t1, p2.Y + r.Y * t1);
                }
                #endregion

                #region Otrho 2
                r = new Point(Math.Cos(alpha), Math.Sin(alpha));
                r_ = new Point(-r.Y, r.X);

                linarg = new LinearEquation2(
                    world.X - p2.X,
                    world.Y - p2.Y,
                    r.X, r_.X,
                    r.Y, r_.Y);

                if (linarg.Solve())
                {
                    double t1 = linarg.Var1;
                    double t2 = linarg.Var2;

                    resP2 = new Point(p2.X + r.X * t1, p2.Y + r.Y * t1);
                }
                #endregion

                if (resP1 != null || resP2 != null)
                {
                    if (gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(world, resP1) <=
                        gView.Framework.SpatialAlgorithms.Algorithm.PointDistance(world, resP2))
                    {
                        _X = resP1.X; _Y = resP1.Y;
                    }
                    else
                    {
                        _X = resP2.X; _Y = resP2.Y;
                    }
                }
                else if (resP1 != null)
                {
                    _X = resP1.X; _Y = resP1.Y;
                }
                else
                {
                    _X = resP2.X; _Y = resP2.Y;
                }
            }
        }
    }

    [gView.Framework.system.RegisterPlugIn("B576D3F9-F7C9-46d5-8A8C-16B3974F1BD7", Obsolete = true)]
    public class EditAttributes : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Editor.Attributes"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module != null && _module.Feature != null)
                {
                    return true;
                }

                return false;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.Attributes", "Attribute Editor"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.application_form_edit; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (_module != null && _module.AttributeEditorWindow != null &&
                _doc != null && _doc.Application is IMapApplication)
            {
                bool found = false;
                foreach (IDockableWindow win in ((IMapApplication)_doc.Application).DockableWindows)
                {
                    if (win == _module.AttributeEditorWindow)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ((IMapApplication)_doc.Application).AddDockableWindow(_module.AttributeEditorWindow, "");
                } ((IMapApplication)_doc.Application).ShowDockableWindow(_module.AttributeEditorWindow);
            }

            return Task.FromResult(true);
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("96099E8C-163E-46ec-BA33-41696BFAE4D5", Obsolete = true)]
    public class StoreFeature : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Editor.Store"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module == null ||
                    _module.SelectedEditLayer == null ||
                    _module.Feature == null)
                {
                    return false;
                }

                if (_module.Feature.OID > 0 &&
                    !Bit.Has(_module.SelectedEditLayer.Statements, EditStatements.UPDATE))
                {
                    return false;
                }

                if (_module.Feature.OID <= 0 &&
                    !Bit.Has(_module.SelectedEditLayer.Statements, EditStatements.INSERT))
                {
                    return false;
                }

                return true;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.Store", "Store Feature"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.database_save; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }
            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (_module == null || _module.FeatureClass == null || _module.Feature == null)
            {
                return false;
            }

            bool ret = false;
            if (_module.Feature.OID > 0)
            {
                ret = await _module.PerformUpdateFeature(_module.FeatureClass, _module.Feature);
            }
            else
            {
                ret = await _module.PerformInsertFeature(_module.FeatureClass, _module.Feature);
            }

            if (!ret)
            {
                if (!String.IsNullOrEmpty(_module.LastMessage))
                {
                    MessageBox.Show(_module.LastMessage, "Message");
                }
            }
            else
            {
                _module.Feature = null;
                await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }

            return ret;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("AC4620D4-3DE4-49ea-A902-0B267BA46BBF", Obsolete = true)]
    public class DeleteFeature : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Editor.Delete"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module == null || _module.SelectedEditLayer == null ||
                    _module.Feature == null ||
                    _module.Feature.OID <= 0 ||
                    !Bit.Has(_module.SelectedEditLayer.Statements, EditStatements.DELETE))
                {
                    return false;
                }

                return true;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.Delete", "Delete Feature"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.database_delete; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }
            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (_module == null || _module.FeatureClass == null || _module.Feature == null)
            {
                return false;
            }

            bool ret = false;
            if (_module.Feature.OID > 0)
            {
                ret = await _module.PerformDeleteFeature(_module.FeatureClass, _module.Feature);
            }
            else
            {
                ret = true;
            }

            if (!ret)
            {
                if (!String.IsNullOrEmpty(_module.LastMessage))
                {
                    MessageBox.Show(_module.LastMessage, "Message");
                }
            }
            else
            {
                _module.Feature = null;
                await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }

            return ret;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("11DEE52F-F241-406e-BB40-9F247532E43D", Obsolete = true)]
    public class DeleteSelectedFeatures : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Editor.DeleteSelected"; }
        }

        public bool Enabled
        {
            get
            {
                if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.TOC == null ||
                    _module == null || _module.SelectedEditLayer == null ||
                    _module.SelectedEditLayer.FeatureLayer == null)
                {
                    return false;
                }

                IFeatureSelection fSel = _module.SelectedEditLayer.FeatureLayer as IFeatureSelection;
                if (fSel == null || fSel.SelectionSet == null)
                {
                    return false;
                }

                return fSel.SelectionSet.Count > 0;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.DeleteSelected", "Delete Selected Feature"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.database_delete_selected; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }
            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.TOC == null ||
                    _module == null || _module.SelectedEditLayer == null ||
                    _module.SelectedEditLayer.FeatureLayer == null)
            {
                return false;
            }

            IFeatureClass fClass = _module.SelectedEditLayer.FeatureLayer.FeatureClass;
            if (fClass == null)
            {
                return false;
            }

            IFeatureSelection fSel = _module.SelectedEditLayer.FeatureLayer as IFeatureSelection;
            if (fSel == null || fSel.SelectionSet == null)
            {
                return false;
            }

            ISelectionSet selectionSet = fSel.SelectionSet;

            if (selectionSet.Count == 0)
            {
                return false;
            }

            IQueryFilter filter = null;
            //List<int> IDs=new List<int>();  // Sollte nicht null sein...
            if (selectionSet is ISpatialIndexedIDSelectionSet)
            {
                List<int> IDs = ((ISpatialIndexedIDSelectionSet)selectionSet).IDs;
                filter = new RowIDFilter(fClass.IDFieldName, IDs);
            }
            else if (selectionSet is IIDSelectionSet)
            {
                List<int> IDs = ((IIDSelectionSet)selectionSet).IDs;
                filter = new RowIDFilter(fClass.IDFieldName, IDs);
            }
            else if (selectionSet is ISpatialIndexedGlobalIDSelectionSet)
            {
                List<long> IDs = ((ISpatialIndexedGlobalIDSelectionSet)selectionSet).IDs;
                filter = new GlobalRowIDFilter(fClass.IDFieldName, IDs);
            }
            else if (selectionSet is IGlobalIDSelectionSet)
            {
                List<long> IDs = ((IGlobalIDSelectionSet)selectionSet).IDs;
                filter = new GlobalRowIDFilter(fClass.IDFieldName, IDs);
            }
            else if (selectionSet is IQueryFilteredSelectionSet)
            {
                filter = ((IQueryFilteredSelectionSet)selectionSet).QueryFilter.Clone() as IQueryFilter;
            }

            if (filter == null)
            {
                return false;
            }

            List<IFeature> features = new List<IFeature>();
            using (IFeatureCursor fCursor = await fClass.GetFeatures(filter))
            {
                IFeature feature;
                while ((feature = await fCursor.NextFeature()) != null)
                {
                    features.Add(feature);
                }
            }

            if (features.Count != selectionSet.Count)
            {
                MessageBox.Show("Queried features are not the same than in selectionset!");
                return false;
            }

            if (MessageBox.Show("Delete " + features.Count + " selected features from " + _module.SelectedEditLayer.FeatureLayer.Title + "?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (IFeature feature in features)
                {
                    bool ret = await _module.PerformDeleteFeature(fClass, feature);
                    if (!ret)
                    {
                        if (!String.IsNullOrEmpty(_module.LastMessage))
                        {
                            MessageBox.Show(_module.LastMessage, "Message");
                        }
                    }
                }
            }

            if (_doc.Application is IMapApplication)
            {
                await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }

            return true;
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("FD340DE3-0BC1-4b3e-99D2-E8DCD55A46F2", Obsolete = true)]
    public class DeleteSketch : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Editor.DeleteSktech"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module == null || _module.FeatureClass == null || _module.Feature == null ||
                    _module.Sketch == null)
                {
                    return false;
                }

                return true;
            }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.Editor.DeleteSktech", "Remove Sketch"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return global::gView.Win.Plugins.Editor.Properties.Resources.cross; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                if (_doc.Application is IMapApplication)
                {
                    _module = ((IMapApplication)_doc.Application).IMapApplicationModule(Globals.ModuleGuid) as Module;
                }
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (_module == null || _module.Sketch == null)
            {
                return Task.FromResult(false);
            }

            _module.Feature = null;
            //((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.Graphics);

            return Task.FromResult(true);
        }

        #endregion
    }


}
