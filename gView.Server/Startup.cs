using gView.Server.AppCode;
using gView.Server.AppCode.Configuration;
using gView.Server.Extensions;
using gView.Server.Extensions.DependencyInjection;
using gView.Server.Middleware;
using gView.Server.Services.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;

namespace gView.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration.BuildConfigParsers();
            Environment = environment;

            Framework.system.SystemInfo.RegisterGdal1_10_PluginEnvironment();
            Framework.system.SystemVariables.UseDiagnostic = 
            Framework.system.ContextVariables.UseMetrics =    
                "true".Equals(Configuration["diagnostics"], StringComparison.OrdinalIgnoreCase);

            #region Graphics Engine

            switch (Configuration["graphics:rendering"]?.ToString())
            {
                case "gdi":
                case "gdiplus":
                    GraphicsEngine.Current.Engine = new gView.GraphicsEngine.GdiPlus.GdiGraphicsEngine(96.0f);
                    break;
                default:
                    GraphicsEngine.Current.Engine = new GraphicsEngine.Skia.SkiaGraphicsEngine(96.0f);
                    break;
            }

            switch (Configuration["graphics:encoding"]?.ToString())
            {
                case "skia":
                case "skiasharp":
                    GraphicsEngine.Current.Encoder = new GraphicsEngine.Skia.SkiaBitmapEncoding();
                    break;
                //case "gdal":
                //    GraphicsEngine.Current.Encoder = new GraphicsEngine.Gdal.GdalBitmapEncoding();
                //    break;
                default:
                    GraphicsEngine.Current.Encoder = new GraphicsEngine.Default.BitmapEncoding();
                    break;
            }

            #endregion

            #region Globals

            if (Configuration["globals:CustomCursorTimeoutSeconds"] != null)
            {
                Framework.Db.Globals.CustomCursorTimeoutSeconds = int.Parse(Configuration["globals:CustomCursorTimeoutSeconds"]);
            }

            #endregion

            #region Create Folders

            Configuration.TryCreateDirectoryIfNotExistes("services-folder");
            Configuration.TryCreateDirectoryIfNotExistes("output-path");
            Configuration.TryCreateDirectoryIfNotExistes("tilecache-root");

            #endregion
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc(o =>
                {
                    o.EnableEndpointRouting = false;
                })
                .AddNewtonsoftJson(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

            services.AddMapServerService(
                config =>
                {
                    config.AppRootPath = Environment.ContentRootPath;
                    if (!String.IsNullOrWhiteSpace(Configuration["services-folder"]))
                    {
                        config.IsValid = true;

                        config.OutputPath = Configuration["output-path"];
                        config.OutputUrl = Configuration["output-url"];
                        config.OnlineResource = Configuration["onlineresource-url"];
                        config.TileCachePath = Configuration["tilecache-root"];

                        config.TaskQueue_MaxThreads = Math.Max(1, int.Parse(Configuration["task-queue:max-parallel-tasks"]));
                        config.TaskQueue_QueueLength = Math.Max(10, int.Parse(Configuration["task-queue:max-queue-length"]));

                        config.ServicesPath = $"{Configuration["services-folder"]}/services";
                        config.LoginManagerRootPath = $"{Configuration["services-folder"]}/_login";
                        config.LoggingRootPath = $"{Configuration["services-folder"]}/log";

                        config.LogServiceErrors = Configuration["Logging:LogServiceErrors"]?.ToLower() != "false";
                        config.LogServiceRequests = Configuration["Logging:LogServiceRequests"]?.ToLower() == "true";
                        config.LogServiceRequestDetails = Configuration["Logging:LogServerRequestDetails"]?.ToLower() == "true";

                        Globals.AllowFormsLogin = config.AllowFormsLogin = Configuration["allowFormsLogin"]?.ToLower() != "false";
                        config.ForceHttps = Configuration["force-https"]?.ToLower() == "true";

                        if (!String.IsNullOrWhiteSpace(Configuration["port"]))
                        {
                            config.Port = int.Parse(Configuration["port"]);
                        }
                    }
                    else
                    {
                        config.IsValid = false;
                    }
                });

            if (!String.IsNullOrEmpty(Configuration["external-auth-authority:url"]))
            {
                services.AddHttpContextAccessor();

                services.AddAccessTokenAuthService(config =>
                {
                    config.Authority = Configuration.GetParsedValue("external-auth-authority:url");
                    config.AllowAccessTokenAuthorization = Configuration.GetParsedValue("external-auth-authority:allow-access-token")?.ToLower() == "true";
                    config.AccessTokenParameterName = Configuration.GetParsedValue("external-auth-authority:access-token-url-parameter") ?? "access-token";
                });
            }

            services.AddPerformanceLoggerService(config =>
            {
                config.LoggerType = Configuration.GetParsedValue("performance-logger:loggertype");
                config.ConnectionString = Configuration.GetParsedValue("performance-logger:connectionstring");
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            #region Background Task

            services.AddHostedService<TaskQueueDequeueService>();
            services.AddHostedService<TimedHostedBackgroundService>();

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseForwardedHeaders();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                //app.ConfigureCustomExceptionMiddleware();
                app.UseForwardedHeaders();
                //app.UseHsts();
            }

            // Hack: app.UseForwardedHeaders() ... not working
            app.UseMiddleware<XForwardedMiddleware>();
            app.UseMiddleware<ArcMapPathDoubleSlashesMiddleware>();

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            //app.UseCookiePolicy();

            app.UseRouting();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "output-path",
                    template: "output/{id}",
                    defaults: new { controller = "Output", Action = "Index" }
                );

                #region GeoServices (MapServer)

                #region Export Map

                routes.MapRoute(
                    name: "geoservices_rest_exportmap",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver/export",
                    defaults: new { controller = "GeoServicesRest", Action = "ExportMap" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_exportmap2",
                    template: "geoservices/rest/services/{id}/mapserver/export",
                    defaults: new { controller = "GeoServicesRest", Action = "ExportMap" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_exportmap_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/export",
                    defaults: new { controller = "GeoServicesRest", Action = "ExportMap" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_exportmap2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver/export",
                    defaults: new { controller = "GeoServicesRest", Action = "ExportMap" }
                );

                #endregion

                #region Query

                routes.MapRoute(
                    name: "geoservices_rest_query",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "Query" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_query2",
                    template: "geoservices/rest/services/{id}/mapserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "Query" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_query_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "Query" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_query2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "Query" }
                );

                #endregion

                #region Identify

                routes.MapRoute(
                   name: "geoservices_rest_identify",
                   template: "geoservices/rest/services/{folder}/{id}/mapserver/identify",
                   defaults: new { controller = "GeoServicesRest", Action = "Identify" }
                );
                routes.MapRoute(
                   name: "geoservices_rest_identify2",
                   template: "geoservices/rest/services/{id}/mapserver/identify",
                   defaults: new { controller = "GeoServicesRest", Action = "Identify" }
                );

                // Experimental
                routes.MapRoute(
                   name: "geoservices_rest_identify_token",
                   template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/identify",
                   defaults: new { controller = "GeoServicesRest", Action = "Identify" }
                );
                routes.MapRoute(
                   name: "geoservices_rest_identify2_token",
                   template: "geoservices({urltoken})/rest/services/{id}/mapserver/identify",
                   defaults: new { controller = "GeoServicesRest", Action = "Identify" }
                );

                #endregion

                #region ServiceLayers

                routes.MapRoute(
                    name: "geoservices_rest_servicelayers",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver/layers",
                    defaults: new { controller = "GeoServicesRest", Action = "ServiceLayers" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_servicelayers2",
                    template: "geoservices/rest/services/{id}/mapserver/layers",
                    defaults: new { controller = "GeoServicesRest", Action = "ServiceLayers" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_servicelayers_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/layers",
                    defaults: new { controller = "GeoServicesRest", Action = "ServiceLayers" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_servicelayers2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver/layers",
                    defaults: new { controller = "GeoServicesRest", Action = "ServiceLayers" }
                );


                #endregion

                #region Legend

                routes.MapRoute(
                    name: "geoservices_rest_servicelegend",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver/legend",
                    defaults: new { controller = "GeoServicesRest", Action = "Legend" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_servicelegend2",
                    template: "geoservices/rest/services/{id}/mapserver/legend",
                    defaults: new { controller = "GeoServicesRest", Action = "Legend" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_servicelegend_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/legend",
                    defaults: new { controller = "GeoServicesRest", Action = "Legend" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_servicelegend2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver/legend",
                    defaults: new { controller = "GeoServicesRest", Action = "Legend" }
                );

                #endregion

                #region WmsServer 

                routes.MapRoute(
                    name: "geoservices_rest_service_wms",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver/wmsserver",
                    defaults: new { controller = "GeoServicesRest", Action = "WmsServer" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_service2_wms",
                    template: "geoservices/rest/services/{id}/mapserver/wmsserver",
                    defaults: new { controller = "GeoServicesRest", Action = "WmsServer" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_service_wms_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/wmsserver",
                    defaults: new { controller = "GeoServicesRest", Action = "WmsServer" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_service2_wms_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver/wmsserver",
                    defaults: new { controller = "GeoServicesRest", Action = "WmsServer" }
                );

                #endregion

                #region ServiceLayer

                routes.MapRoute(
                   name: "geoservices_rest_servicelayer",
                   template: "geoservices/rest/services/{folder}/{id}/mapserver/{layerId}",
                   defaults: new { controller = "GeoServicesRest", Action = "ServiceLayer" }
                );
                routes.MapRoute(
                   name: "geoservices_rest_servicelayer2",
                   template: "geoservices/rest/services/{id}/mapserver/{layerId}",
                   defaults: new { controller = "GeoServicesRest", Action = "ServiceLayer" }
                );

                // Experimental
                routes.MapRoute(
                   name: "geoservices_rest_servicelayer_token",
                   template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver/{layerId}",
                   defaults: new { controller = "GeoServicesRest", Action = "ServiceLayer" }
                );
                routes.MapRoute(
                   name: "geoservices_rest_servicelayer2_token",
                   template: "geoservices({urltoken})/rest/services/{id}/mapserver/{layerId}",
                   defaults: new { controller = "GeoServicesRest", Action = "ServiceLayer" }
                );

                #endregion

                #region Service

                routes.MapRoute(
                    name: "geoservices_rest_service",
                    template: "geoservices/rest/services/{folder}/{id}/mapserver",
                    defaults: new { controller = "GeoServicesRest", Action = "Service" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_service2",
                    template: "geoservices/rest/services/{id}/mapserver",
                    defaults: new { controller = "GeoServicesRest", Action = "Service" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_service_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/mapserver",
                    defaults: new { controller = "GeoServicesRest", Action = "Service" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_service2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/mapserver",
                    defaults: new { controller = "GeoServicesRest", Action = "Service" }
                );

                #endregion

                #endregion

                #region GeoServices (Folder/Services)

                #region Folder

                routes.MapRoute(
                    name: "geoservices_rest_folder",
                    template: "geoservices/rest/services/{id}",
                    defaults: new { controller = "GeoServicesRest", Action = "Folder" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_folder_token",
                    template: "geoservices({urltoken})/rest/services/{id}",
                    defaults: new { controller = "GeoServicesRest", Action = "Folder" }
                );

                #endregion

                #region Services

                routes.MapRoute(
                    name: "geoservices_rest_services",
                    template: "geoservices/rest/services",
                    defaults: new { controller = "GeoServicesRest", Action = "Services" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_services_token",
                    template: "geoservices({urltoken})/rest/services",
                    defaults: new { controller = "GeoServicesRest", Action = "Services" }
                );

                #endregion

                #endregion

                #region GeoServices (FeatureServer)

                #region FeatureServerQuery

                routes.MapRoute(
                    name: "geoservices_rest_featureserver_query",
                    template: "geoservices/rest/services/{folder}/{id}/featureserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerQuery" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_query2",
                    template: "geoservices/rest/services/{id}/featureserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerQuery" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_query_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerQuery" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_query2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/featureserver/{layerId}/query",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerQuery" }
                );

                #endregion

                #region FeatureServerAddFeatures

                routes.MapRoute(
                    name: "geoservices_rest_featureserver_addfeatures",
                    template: "geoservices/rest/services/{folder}/{id}/featureserver/{layerId}/addfeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerAddFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_addfeatures2",
                    template: "geoservices/rest/services/{id}/featureserver/{layerId}/addfeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerAddFeatures" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_addfeatures_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver/{layerId}/addfeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerAddFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_addfeatures2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/featureserver/{layerId}/addfeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerAddFeatures" }
                );

                #endregion

                #region FeatureServerUpdateFeatures

                routes.MapRoute(
                    name: "geoservices_rest_featureserver_updatefeatures",
                    template: "geoservices/rest/services/{folder}/{id}/featureserver/{layerId}/updatefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerUpdateFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_updatefeatures2",
                    template: "geoservices/rest/services/{id}/featureserver/{layerId}/updatefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerUpdateFeatures" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_updatefeatures_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver/{layerId}/updatefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerUpdateFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_updatefeatures2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/featureserver/{layerId}/updatefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerUpdateFeatures" }
                );

                #endregion

                #region FeatureServerDeleteFeatures

                routes.MapRoute(
                    name: "geoservices_rest_featureserver_deletefeatures",
                    template: "geoservices/rest/services/{folder}/{id}/featureserver/{layerId}/deletefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerDeleteFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_deletefeatures2",
                    template: "geoservices/rest/services/{id}/featureserver/{layerId}/deletefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerDeleteFeatures" }
                );

                // Experimental
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_deletefeatures_token",
                    template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver/{layerId}/deletefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerDeleteFeatures" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver_deletefeatures2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/featureserver/{layerId}/deletefeatures",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerDeleteFeatures" }
                );

                #endregion

                #region FeatureServerLayer

                routes.MapRoute(
                  name: "geoservices_rest_featureserverlayer",
                  template: "geoservices/rest/services/{folder}/{id}/featureserver/{layerId}",
                  defaults: new { controller = "GeoServicesRest", Action = "FeatureServerLayer" }
                );
                routes.MapRoute(
                  name: "geoservices_rest_featureserverlayer2",
                  template: "geoservices/rest/services/{id}/featureserver/{layerId}",
                  defaults: new { controller = "GeoServicesRest", Action = "FeatureServerLayer" }
                );

                // Experimental
                routes.MapRoute(
                  name: "geoservices_rest_featureserverlayer_token",
                  template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver/{layerId}",
                  defaults: new { controller = "GeoServicesRest", Action = "FeatureServerLayer" }
                );
                routes.MapRoute(
                  name: "geoservices_rest_featureserverlayer2_token",
                  template: "geoservices({urltoken})/rest/services/{id}/featureserver/{layerId}",
                  defaults: new { controller = "GeoServicesRest", Action = "FeatureServerLayer" }
                );

                #endregion

                #region FeatureServerService

                routes.MapRoute(
                   name: "geoservices_rest_featureserver",
                   template: "geoservices/rest/services/{folder}/{id}/featureserver",
                   defaults: new { controller = "GeoServicesRest", Action = "FeatureServerService" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver2",
                    template: "geoservices/rest/services/{id}/featureserver",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerService" }
                );

                // Experimental
                routes.MapRoute(
                   name: "geoservices_rest_featureserver_token",
                   template: "geoservices({urltoken})/rest/services/{folder}/{id}/featureserver",
                   defaults: new { controller = "GeoServicesRest", Action = "FeatureServerService" }
                );
                routes.MapRoute(
                    name: "geoservices_rest_featureserver2_token",
                    template: "geoservices({urltoken})/rest/services/{id}/featureserver",
                    defaults: new { controller = "GeoServicesRest", Action = "FeatureServerService" }
                );

                #endregion

                #endregion

                #region GeoServices (GenerateToken/Info)

                #region GenerateToken

                routes.MapRoute(
                   name: "geoservices_restgeneratetoken",
                   template: "geoservices/tokens/generateToken",
                   defaults: new { controller = "GeoServicesRest", Action = "GenerateToken" }
                );

                // Experimental
                routes.MapRoute(
                   name: "geoservices_restgeneratetoken_token",
                   template: "geoservices({urltoken})/tokens/generateToken",
                   defaults: new { controller = "GeoServicesRest", Action = "GenerateToken" }
                );

                #endregion

                #region RestInfo

                routes.MapRoute(
                  name: "geoservices_rest_info",
                  template: "geoservices/rest/info",
                  defaults: new { controller = "GeoServicesRest", Action = "RestInfo" }
                );

                // Experimental
                routes.MapRoute(
                  name: "geoservices_rest_info_token",
                  template: "geoservices({urltoken})/rest/info",
                  defaults: new { controller = "GeoServicesRest", Action = "RestInfo" }
                );

                #endregion

                #endregion

                // Ogc
                routes.MapRoute(
                    name: "ogc_request",
                    template: "ogc/{id}",
                    defaults: new { controller = "Ogc", Action = "OgcRequest" }
                );
                routes.MapRoute(
                    name: "ogc_tiles",
                    template: "tilewmts/{folder}/{name}/{cachetype}/{origin}/{epsg}/{style}/{level}/{row}/{col}",
                    defaults: new { controller = "Ogc", Action = "TileWmts" }
                );
                routes.MapRoute(
                    name: "ogc_tiles2",
                    template: "tilewmts/{name}/{cachetype}/{origin}/{epsg}/{style}/{level}/{row}/{col}",
                    defaults: new { controller = "Ogc", Action = "TileWmts" }
                );


                // ArcIMS
                routes.MapRoute(
                    name: "ags-servlet",
                    template: "servlet/com.esri.esrimap.Esrimap",
                    defaults: new { controller = "ArcIMS", Action = "Esrimap" }
                );
                routes.MapRoute(
                    name: "ags-servlet2",
                    template: "arcims/servlet/com.esri.esrimap.Esrimap",
                    defaults: new { controller = "ArcIMS", Action = "Esrimap" }
                );

                // MapServer
                routes.MapRoute(
                    name: "mapserver-catelog",
                    template: "catalog",
                    defaults: new { controller = "MapServer", Action = "Catalog" }
                );
                routes.MapRoute(
                    name: "mapserver-maprequest-wmts-comp",
                    template: "MapRequest/wmts/{id}",
                    defaults: new { controller = "Ogc", Action = "OgcRequest" }
                    );
                routes.MapRoute(
                    name: "mapserver-maprequest",
                    template: "MapRequest/{guid}/{folder}/{name}",
                    defaults: new { controller = "MapServer", Action = "MapRequest" }
                );
                routes.MapRoute(
                    name: "mapserver-maprequest2",
                    template: "MapRequest/{guid}/{name}",
                    defaults: new { controller = "MapServer", Action = "MapRequest" }
                );
                routes.MapRoute(
                    name: "mapserver-addmap",
                    template: "AddMap/{folder}/{name}",
                    defaults: new { controller = "MapServer", Action = "AddMap" }
                );
                routes.MapRoute(
                    name: "mapserver-addmap2",
                    template: "AddMap/{name}",
                    defaults: new { controller = "MapServer", Action = "AddMap" }
                );
                routes.MapRoute(
                    name: "mapserver-removemap2",
                    template: "RemoveMap/{folder}/{name}",
                    defaults: new { controller = "MapServer", Action = "RemoveMap" }
                );
                routes.MapRoute(
                    name: "mapserver-removemap",
                    template: "RemoveMap/{name}",
                    defaults: new { controller = "MapServer", Action = "RemoveMap" }
                );
                routes.MapRoute(
                    name: "mapserver-getmetadata",
                    template: "GetMetadata/{folder}/{name}",
                    defaults: new { controller = "MapServer", Action = "GetMetadata" }
                );
                routes.MapRoute(
                    name: "mapserver-getmetadata2",
                    template: "GetMetadata/{name}",
                    defaults: new { controller = "MapServer", Action = "GetMetadata" }
                );
                routes.MapRoute(
                    name: "mapserver-setmetadata",
                    template: "SetMetadata/{folder}/{name}",
                    defaults: new { controller = "MapServer", Action = "SetMetadata" }
                );
                routes.MapRoute(
                    name: "mapserver-setmetadata2",
                    template: "SetMetadata/{name}",
                    defaults: new { controller = "MapServer", Action = "SetMetadata" }
                );

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            //InternetMapServer.Init(env.ContentRootPath).Wait();
        }
    }
}
