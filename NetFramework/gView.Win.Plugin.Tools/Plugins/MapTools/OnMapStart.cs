﻿using gView.Framework.UI;
using gView.Plugins.MapTools.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace gView.Plugins.Tools.MapTools
{
    [gView.Framework.system.RegisterPlugIn("209c1c3a-f3ad-4a70-a1e6-0cd57a3e855d")]
    public class OnMapStart : ITool
    {
        private IMapDocument _doc = null;
        private Window _dialog = null;

        #region ITool Member

        public string Name
        {
            get { return "On Carto Start..."; }
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
            if (hook is IMapDocument && ((IMapDocument)hook).Application is IMapApplication)
            {
                _doc = (MapDocument)hook;
                ((IMapApplication)_doc.Application).OnApplicationStart += new EventHandler(OnMapStart_OnApplicationStart);
            }
        }

        void OnMapStart_OnApplicationStart(object sender, EventArgs e)
        {
            _dialog = new Window();
            NewToolControl control = new NewToolControl(_doc);
            _dialog.Content = control;
            _dialog.Width = 500;
            _dialog.Height = 400;
            _dialog.VerticalAlignment = VerticalAlignment.Center;
            _dialog.HorizontalAlignment = HorizontalAlignment.Center;
            _dialog.Title = "Application Start";

            control.OnButtonClick += new EventHandler(control_OnButtonClick);
            _dialog.ShowDialog();
        }

        void control_OnButtonClick(object sender, EventArgs e)
        {
            if (_dialog != null)
            {
                _dialog.Close();
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion
    }
}
