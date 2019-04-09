﻿using gView.Framework.Carto;
using gView.Framework.Editor.Core;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Plugins.Modules
{
    [gView.Framework.system.RegisterPlugIn("45713F48-0D81-4a54-A422-D0E6F397BC95")]
    public class EditorModule : IMapApplicationModule, IPersistable
    {
        private List<EditLayer> _editLayers = new List<EditLayer>();

        internal void AddEditLayer(EditLayer editLayer)
        {
            _editLayers.Add(editLayer);
        }

        internal IEnumerable<EditLayer> EditLayers => _editLayers;

        public IEditLayer GetEditLayer(int id)
        {
            return _editLayers
                .Where(l => l.LayerId == id)
                .Select(l => new EditLayer(l))
                .FirstOrDefault();
        }

        #region IMapApplicationModule

        public void OnCreate(object hook)
        {
            
        }

        #endregion

        #region IPersistable

        public Task<bool> Load(IPersistStream stream)
        {
            _editLayers.Clear();
            if (stream == null)
                return Task.FromResult(true); ;

            MapEditLayerPersist mapEditLayers;
            while ((mapEditLayers = (MapEditLayerPersist)stream.Load("MapEditLayers", null, new MapEditLayerPersist(this))) != null)
            {
            }

            return Task.FromResult(true);
        }

        public Task<bool> Save(IPersistStream stream)
        {
            stream.Save("MapEditLayers", new MapEditLayerPersist(this));

            return Task.FromResult(true);
        }

        #endregion

        #region Classes

        internal class MapEditLayerPersist : IPersistable
        {
            private EditorModule _module;

            public MapEditLayerPersist(EditorModule module)
            {
                _module = module;
            }

            public EditorModule Module { get { return _module; } }

            #region IPersistable Member

            public Task<bool> Load(IPersistStream stream)
            {
                if (stream == null ||
                    _module == null)
                    return Task.FromResult(true);

                EditLayer eLayer;
                while ((eLayer = (EditLayer)stream.Load("EditLayer", null, new EditLayer())) != null)
                {
                    _module.AddEditLayer(eLayer);
                }

                return Task.FromResult(true);
            }

            public Task<bool> Save(IPersistStream stream)
            {
                if (stream == null ||
                    _module == null)
                    return Task.FromResult(true);

                stream.Save("index", 0);
                foreach (IEditLayer editLayer in _module.EditLayers)
                {
                    if (editLayer == null) continue;
                    stream.Save("EditLayer", editLayer);
                }

                return Task.FromResult(true);
            }

            #endregion
        }

        internal class EditLayer : IPersistable, IEditLayer
        {
            public EditLayer()
            {
                this.Statements = EditStatements.NONE;
            }

            public EditLayer(EditLayer editLayer)
            {
                this.LayerId = editLayer.LayerId;
                this.ClassName = editLayer.ClassName;
                this.Statements = editLayer.Statements;
            }

            public int LayerId { get; private set; }
            public string ClassName { get; private set; }
            public EditStatements Statements { get; private set; }

            #region IPersistable

            public Task<bool> Load(IPersistStream stream)
            {
                LayerId = (int)stream.Load("id", -1);
                ClassName = (string)stream.Load("class", String.Empty);
                Statements = (EditStatements)stream.Load("statement", (int)EditStatements.NONE);

                return Task.FromResult(true);
            }

            public Task<bool> Save(IPersistStream stream)
            {
                stream.Save("id", LayerId);
                stream.Save("class", ClassName);
                stream.Save("statement", (int)Statements);

                return Task.FromResult(true);
            }

            #endregion
        }

        #endregion  
    }
}
