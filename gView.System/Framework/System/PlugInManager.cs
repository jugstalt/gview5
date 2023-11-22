using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace gView.Framework.system
{
    public class KnownObjects
    {
        static public Guid Tools_DynamicZoomIn { get { return new Guid("09007afa-b255-4864-ac4f-965df330bfc4"); } }
        static public Guid Tools_DynamicZoomOut { get { return new Guid("51d04e6f-a13e-40b6-bf28-1b8e7c24493d"); } }
        static public Guid Tools_Pan { get { return new Guid("2680f0fd-31ee-48c1-b0f7-6674bad0a688"); } }
        static public Guid Tools_SmartNavigation { get { return new Guid("3e2e9f8c-24fb-48f6-b80e-1b0a54e8c309"); } }
        static public Guid Tools_Identify { get { return new Guid("f13d5923-70c8-4c6b-9372-0760d3a8c08c"); } }
        static public Guid Tools_QueryThemeCombo { get { return new Guid("51A2CF81-E343-4c58-9A42-9207C8DFBC01"); } }
        static public Guid Tools_Zoom2Extent { get { return new Guid("58AE3C1D-40CD-4f61-8C5C-0A955C010CF4"); } }
        static public Guid Tools_TOC { get { return new Guid("0728E12C-AC12-4264-9B47-ECE6BB0CFFA9"); } }
        static public Guid Tools_StaticZoomIn { get { return new Guid("6351BCBE-809A-43cb-81AA-6414ED3FA459"); } }
        static public Guid Tools_StaticZoomOut { get { return new Guid("E1C01E9D-8ADC-477b-BCD1-6B7BBA756D44"); } }


        static public Guid Carto_SimpleRenderer { get { return new Guid("646386e4-d010-4c7d-98aa-8c1903a3d5e4"); } }
        static public Guid Carto_SimpleLabelRenderer { get { return new Guid("92650f7d-cec9-4418-9ea0-a8b09436aa7a"); } }
        static public Guid Carto_ValueMapRenderer { get { return new Guid("c7a92674-0120-4f3d-bc03-f1210136b5c6"); } }
        static public Guid Carto_UniversalGeometryRenderer { get { return new Guid("48EDC5DB-18B6-44cc-8646-461B388F2D94"); } }

        static public Guid Symbology_SimplePointSymbol { get { return new Guid("F73F40DD-BA55-40b1-B372-99F08B66D2D4"); } }
        static public Guid Symbology_SimpleLineSymbol { get { return new Guid("91CC3F6F-0EC5-42b7-AA34-9C89803118E7"); } }
        static public Guid Symbology_SimpleFillSymbol { get { return new Guid("1496A1A8-8087-4eba-86A0-23FB91197B22"); } }
        static public Guid Symbology_HatchSymbol { get { return new Guid("E37D7D86-DF11-410f-ADD1-EA89C1E89605"); } }
        static public Guid Symbology_PolygonMask { get { return new Guid("48177A8B-1B3F-480a-87DF-9F7E1DE57D7B"); } }
        static public Guid Symbology_SimpleTextSymbol { get { return new Guid("A5DA4D8D-879F-41a5-9795-F22BE5B85877"); } }

        static public Guid Persistable_Dictionary { get { return new Guid("5CF9FD50-16B2-4650-A3E3-229E60848CA6"); } }
        static public Guid Persistable_Metadata { get { return new Guid("24373762-127A-4125-B87F-AC0D091D947E"); } }
    }
    /// <summary>
    /// Zusammenfassung f�r AssemblyExplorer.
    /// </summary>

    public class PlugInManager
    {
        private static Dictionary<Guid, Type> _pluginTypes;

        public PlugInManager()
        {
            Init();
        }

        static public PluginUsage Usage = PluginUsage.Server;

        public delegate void ParseAssemblyDelegate(string assemblyName);

        public static event ParseAssemblyDelegate OnParseAssembly = null;
        public static event ParseAssemblyDelegate OnAddPluginType = null;

        public static bool InitSilent = false;

        public static void Init()
        {
            var currentEngine = GraphicsEngine.Current.Engine;
            if (GraphicsEngine.Current.Engine == null)
            {
                SystemInfo.RegisterDefaultGraphicEnginges();
            }

            if (_pluginTypes != null)
            {
                return;
            }

            string currentDll = String.Empty;
            var error = new StringBuilder();

            try
            {
                _pluginTypes = new Dictionary<Guid, Type>();
                FileInfo entryAssembly = new FileInfo(Assembly.GetEntryAssembly().Location);

                Console.WriteLine($"Init PluginManager: {entryAssembly.Directory.FullName}");

                foreach (FileInfo dll in entryAssembly.Directory.GetFiles("*.dll").Where(f => f.Name.ToLower().StartsWith("gview.")))
                {
                    currentDll = dll.Name;

                    OnParseAssembly?.Invoke(dll.Name);

                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(dll.FullName);

                        foreach (var pluginType in assembly.GetTypes())
                        {
                            var registerPluginAttribute = pluginType.GetCustomAttribute<RegisterPlugInAttribute>();
                            if (registerPluginAttribute == null)
                            {
                                continue;
                            }

                            if (!registerPluginAttribute.Usage.HasFlag(PlugInManager.Usage))
                            {
                                continue;
                            }

                            if (registerPluginAttribute.Obsolete == true)
                            {
                                continue;
                            }

                            _pluginTypes.Add(registerPluginAttribute.Value, pluginType);

                            OnAddPluginType?.Invoke(pluginType.ToString());

                            if (InitSilent == false)
                            {
                                Console.WriteLine($"added {pluginType.ToString()}");
                            }
                        }
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (Exception ex)
                    {
                        error.Append(currentDll + ": " + ex.Message);
                        error.Append(Environment.NewLine);

                        //while(ex.InnerException!=null)
                        //{
                        //    ex = ex.InnerException;
                        //    error.Append("Inner: " + ex.Message);
                        //    error.Append(Environment.NewLine);
                        //}
                    }
                }

                if (InitSilent == true)
                {
                    Console.WriteLine($"PluginManager: added {_pluginTypes.Count} plugins...");
                }
            }
            catch (Exception ex)
            {
                error.Append(currentDll + ": " + ex.Message);
                error.Append(Environment.NewLine);

                Console.WriteLine("PluginManager Error:");
                Console.WriteLine(error);
            }

            if (error.Length > 0)
            {
                //throw new Exception("PluginMananger.Init() " + Environment.NewLine + error.ToString());
            }

            OnParseAssembly?.Invoke(null);

            GraphicsEngine.Current.Engine = currentEngine;
        }

        public IEnumerable<Type> GetPlugins(Type interfaceType)
        {
            Init();

            List<Type> pluginTypes = new List<Type>();

            foreach (var pluginType in _pluginTypes.Values)
            {
                if (interfaceType.IsAssignableFrom(pluginType))
                {
                    pluginTypes.Add(pluginType);
                }
            }

            return pluginTypes;
        }

        public IEnumerable<Type> GetPlugins(Plugins.Type type)
        {
            Init();

            List<Type> pluginTypes = new List<Type>();

            foreach (var pluginType in _pluginTypes.Values)
            {
                foreach (var interfaceType in pluginType.GetInterfaces()
                    .Where(t => t.ToString().ToLower().StartsWith("gview.framework.")))
                {
                    if (interfaceType.ToString().Substring(interfaceType.ToString().LastIndexOf(".") + 1).ToLower() == type.ToString().ToLower())
                    {
                        pluginTypes.Add(pluginType);
                    }
                }
            }

            return pluginTypes;
        }

        public IEnumerable<object> GetPluginInstances(Type interfaceType)
        {
            return GetPlugins(interfaceType).Select(i => Activator.CreateInstance(i));
        }

        public IEnumerable<Type> GetPluginTypes
        {
            get
            {
                Init();

                List<Type> types = new List<Type>();
                foreach (var pluginType in _pluginTypes.Values)
                {
                    foreach (var interfaceType in pluginType.GetInterfaces()
                        .Where(t => t.ToString().ToLower().StartsWith("gview.framework.")))
                    {
                        if (!types.Contains(interfaceType))
                        {
                            types.Add(interfaceType);
                        }
                    }

                }

                return types;
            }
        }

        public object CreateInstance(Guid guid)
        {
            return PlugInManager.Create(guid);
        }
        public T CreateInstance<T>(Type type)
        {
            return (T)Activator.CreateInstance(type);
        }

        public T TryCreateInstance<T>(Type type)
        {
            try
            {
                return CreateInstance<T>(type);
            }
            catch
            {
                return default(T);
            }
        }

        public object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public object CreateInstance(object plugin)
        {
            return PlugInManager.Create(plugin);
        }

        public static object Create(XmlNode node)
        {
            try
            {
                if (node == null)
                {
                    return null;
                }

                if (node.Attributes["fullname"] == null ||
                    node.Attributes["assembly"] == null)
                {
                    return null;
                }

                string path = node.Attributes["assembly"].Value;
                if (path.Contains("{APP_PATH}"))
                {
                    path = path.Replace("{APP_PATH}", SystemVariables.ApplicationDirectory);
                }

                if (path.Contains("[APP_PATH]"))
                {
                    path = path.Replace("[APP_PATH]", SystemVariables.ApplicationDirectory);
                }

                Assembly assembly = Assembly.LoadFrom(path);
                return assembly.CreateInstance(node.Attributes["fullname"].Value, false);
            }
            catch
            {
                return null;
            }
        }
        public static object Create(Guid guid)
        {

            try
            {
                Init();

                if (_pluginTypes.ContainsKey(guid))
                {
                    var pluginType = _pluginTypes[guid];
                    return Activator.CreateInstance(pluginType);
                }

                return null;
            }
            catch /*(Exception ex)*/
            {
                return null;
            }

        }
        public static object Create(object plugin)
        {
            if (IsPlugin(plugin))
            {
                return Create(PlugInID(plugin));
            }

            return null;
        }
        public static object Create(Guid guid, object parameter)
        {
            IPlugInParameter plugin = PlugInManager.Create(guid) as IPlugInParameter;
            if (plugin != null)
            {
                plugin.Parameter = parameter;
            }

            return plugin;
        }
        public static object Create(object PlugIn, object parameter)
        {
            IPlugInParameter plugin = PlugInManager.Create(PlugIn) as IPlugInParameter;
            if (plugin != null)
            {
                plugin.Parameter = parameter;
            }

            return plugin;
        }
        static public bool IsPlugin(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            RegisterPlugInAttribute plugin = (RegisterPlugInAttribute)Attribute.GetCustomAttribute(obj is Type ? (Type)obj : obj.GetType(), typeof(RegisterPlugInAttribute));
            return (plugin != null);
        }

        static public Guid PluginIDFromType(Type type)
        {
            return PlugInID(new PlugInManager().CreateInstance((Type)type));
        }

        static public Guid PlugInID(object obj)
        {
            if (obj == null)
            {
                return new Guid();
            }

            if (obj is IPlugInWrapper)
            {
                return PlugInID(((IPlugInWrapper)obj).WrappedPlugIn);
            }
            else
            {
                RegisterPlugInAttribute plugin = (RegisterPlugInAttribute)Attribute.GetCustomAttribute(obj is Type ? (Type)obj : obj.GetType(), typeof(RegisterPlugInAttribute));
                if (plugin == null)
                {
                    return new Guid();
                }

                return plugin.Value;
            }
        }

        #region Classes

        public class PlugInType
        {
            public Type Type { get; internal set; }
            public Guid PluginGuid { get; internal set; }

            public PlugInType(object plugin)
            {
                if (plugin == null)
                {
                    throw new ArgumentException();
                }

                Type = plugin.GetType();
                PluginGuid = PlugInManager.PlugInID(plugin);
            }

            public object CreateInstance()
            {
                return PlugInManager.Create(PluginGuid);
            }
        }

        public class PlugInTypeList : List<PlugInType>
        {
            public bool ContainsType(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentException();
                }

                foreach (PlugInType t in this)
                {
                    if (type.Equals(t.Type))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion
    }

    public class SortByIOrder : IComparer<IOrder>
    {
        #region IComparer<IOrder> Members

        public int Compare(IOrder x, IOrder y)
        {
            if (x.SortOrder < y.SortOrder)
            {
                return -1;
            }

            if (x.SortOrder > y.SortOrder)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }

    public class OrderedPluginList<T>
    // where T : IOrder
    {
        public static List<T> Sort(IEnumerable<Type> pluginNodes)
        {
            List<T> ret = new List<T>();
            foreach (var node in pluginNodes)
            {
                try
                {
                    T t = (T)PlugInManager.Create(node);
                    if (t == null)
                    {
                        continue;
                    }

                    ret.Add(t);
                }
                catch { }
            }
            ret.Sort(new Sorter());
            return ret;
        }

        public static List<T> Sort(IEnumerable<T> plugins)
        {
            List<T> ret = new List<T>(plugins);
            ret.Sort(new Sorter());
            return ret;
        }

        private class Sorter : IComparer<T>
        {
            #region IComparer<IOrder> Members

            public int Compare(T x, T y)
            {
                if (!(x is IOrder) || !(y is IOrder))
                {
                    return 0;
                }

                if (((IOrder)x).SortOrder < ((IOrder)y).SortOrder)
                {
                    return -1;
                }

                if (((IOrder)x).SortOrder > ((IOrder)y).SortOrder)
                {
                    return 1;
                }

                return 0;
            }

            #endregion
        }
    }

    public class Plugins
    {

        public enum Type
        {
            IDataset,
            ITool,
            IExTool,
            ICartoTool,
            IExplorerTool,
            IExplorerToolCommand,
            IDatasetElementContextMenuItem,
            IMapContextMenuItem,
            IFeatureRenderer,
            ILabelRenderer,
            ISymbol,
            IDockableWindowContainer,
            IExplorerObject,
            IExplorerFileObject,
            IExplorerCommand,
            IExplorerTabPage,
            IServiceRequestInterpreter,
            IMapOptionPage,
            IExplorerOptionPage,
            ILayerPropertyPage,
            IGraphicElement2,
            IAutoField,
            IFieldDomain,
            IServiceableDataset,
            IFileFeatureDatabase,
            IMetadataProvider,
            IPersistable,
            IMapApplicationModule,
            IActivity,
            ISimpleNumberCalculation,
            INetworkTracer,
            IFeatureLayerJoin,
            IFeatureDatabase,
            ICartoRibbonTab,
            IExplorerRibbonTab,
            IPerpertyPageDefinition
        };

        //public static string TypeName(Plugins.Type type) 
        //{
        //    switch (type)
        //    {
        //        case Type.IDataset:
        //            return typeof(IDataset).ToString(); 
        //        case Type.ITool:
        //            return typeof(ITool).ToString(); 
        //        case Type.IExTool:
        //            return typeof(IExTool).ToString();
        //        case Type.IDatasetElementContextMenuItem:
        //            return typeof(IDatasetElementContextMenuItem).ToString();
        //        case Type.IMapContextMenuItem:
        //            return typeof(IMapContextMenuItem).ToString();
        //        case Type.IFeatureRenderer:
        //            return typeof(IFeatureRenderer).ToString();
        //        case Type.ILabelRenderer:
        //            return typeof(ILabelRenderer).ToString();
        //        case Type.ISymbol:
        //            return typeof(ISymbol).ToString();
        //        case Type.IDockableWindowContainer:
        //            return typeof(IDockableWindowContainer).ToString();
        //        case Type.IExplorerObject:
        //            return typeof(IExplorerObject).ToString();
        //        case Type.IExplorerCommand:
        //            return typeof(IExplorerCommand).ToString();
        //        case Type.IExplorerTabPage:
        //            return "gView.Framework.UI.IExplorerTabPage";
        //        case Type.IServiceRequestInterpreter:
        //            return typeof(IServiceRequestInterpreter).ToString();
        //        case Type.IMapOptionPage:
        //            return "gView.Framework.UI.IMapOptionPage";
        //        case Type.IExplorerOptionPage:
        //            return "gView.Framework.UI.IExplorerOptionPage";
        //        case Type.ILayerPropertyPage:
        //            return "gView.Framework.UI.ILayerPropertyPage";
        //        case Type.IGraphicElement2:
        //            return typeof(IGraphicElement2).ToString();
        //        case Type.IAutoField:
        //            return typeof(IAutoField).ToString();
        //        case Type.IFieldDomain:
        //            return typeof(IFieldDomain).ToString();
        //        case Type.IServiceableDataset:
        //            return typeof(IServiceableDataset).ToString();
        //        case Type.IFileFeatureDatabase:
        //            return typeof(IFileFeatureDatabase).ToString();
        //        case Type.IMetadataProvider:
        //            return typeof(IMetadataProvider).ToString();
        //        case Type.IPersistable:
        //            return typeof(IPersistable).ToString();
        //        case Type.IMapApplicationModule:
        //            return typeof(IMapApplicationModule).ToString();
        //        case Type.IActivity:
        //            return typeof(IActivity).ToString();
        //        case Type.ISimpleNumberCalculation:
        //            return typeof(ISimpleNumberCalculation).ToString();
        //        case Type.INetworkTracer:
        //            return typeof(INetworkTracer).ToString();
        //        case Type.IFeatureLayerJoin:
        //            return typeof(IFeatureLayerJoin).ToString();
        //        case Type.IFeatureDatabase:
        //            return typeof(IFeatureDatabase).ToString();
        //        case Type.ICartoRibbonTab:
        //            return "gView.Framework.UI.ICartoRibbonTab";
        //        case Type.IExplorerRibbonTab:
        //            return "gView.Framework.UI.IExplorerRibbonTab";
        //    }
        //    return String.Empty;
        //}
    }
}
