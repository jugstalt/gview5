﻿using gView.Server.Services.Hosting;
using gView.Server.Services.Logging;
using gView.Server.Services.MapServer;
using gView.Server.Services.Security;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace gView.Server.Extensions.DependencyInjection
{
    static public class ServiceCollectionExtensions
    {
        static public IServiceCollection AddMapServerService(this IServiceCollection services,
                                                             Action<MapServerManagerOptions> configAction)
        {
            services.Configure<MapServerManagerOptions>(configAction);
            services.AddSingleton<MapServiceManager>();
            services.AddSingleton<EncryptionCertificateService>();
            services.AddSingleton<MapServiceDeploymentManager>();

            services.AddTransient<UrlHelperService>();
            services.AddTransient<LoginManager>();
            services.AddTransient<AccessControlService>();

            services.AddTransient<MapServicesEventLogger>();

            return services;
        }

        static public IServiceCollection AddAccessTokenAuthService(this IServiceCollection services,
                                                                   Action<AccessTokenAuthServiceOptions> configAction)
        {
            services.Configure<AccessTokenAuthServiceOptions>(configAction);
            return services.AddTransient<AccessTokenAuthService>();
        }

        static public IServiceCollection AddPerformanceLoggerService(this IServiceCollection services,
                                                                     Action<PerformanceLoggerServiceOptions> configAction)
        {
            services.Configure<PerformanceLoggerServiceOptions>(configAction);
            return services.AddSingleton<PerformanceLoggerService>();
        }
    }
}
