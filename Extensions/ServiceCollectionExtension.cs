using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mqtt.Client.AspNetCore.Services;
using Mqtt.Client.AspNetCore.Settings;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;

namespace Mqtt.Client.AspNetCore.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddMqttClientHostedService(this IServiceCollection services)
        {
            services.AddMqttClientServiceWithConfig(aspOptionBuilder =>
            {
                var clientSettinigs = AppSettingsProvider.ClientSettings;
                var brokerHostSettings = AppSettingsProvider.BrokerHostSettings;

                aspOptionBuilder
                .WithCredentials(clientSettinigs.UserName, clientSettinigs.Password)
                .WithClientId(clientSettinigs.Id)
                .WithTcpServer(brokerHostSettings.Host, brokerHostSettings.Port);
            });
            return services;
        }

        private static IServiceCollection AddMqttClientServiceWithConfig(this IServiceCollection services, Action<MqttClientOptionsBuilder> configure)
        {
            var currentDir = System.Environment.CurrentDirectory;
            var caCert = X509Certificate.CreateFromCertFile($"{currentDir}\\ca_1.crt");
            //var clientCert = new X509Certificate2(@"client-certificate.pfx", "ExportPasswordUsedWhenCreatingPfxFile");
            //var clientKey = X509Certificate2.CreateFromCertFile($"{currentDir}\\client.key");
            //var clientCert = X509Certificate.CreateFromCertFile($"{currentDir}\\client.crt");



            services.AddSingleton<MqttClientOptions>(serviceProvider =>
            {
                var optionBuilder = new MqttClientOptionsBuilder()
                           .WithTls(new MqttClientOptionsBuilderTlsParameters()
                           {
                               UseTls = true,
                               SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                               Certificates = new List<X509Certificate>()
                               {
                                    //clientCert, 
                                   caCert, 
                                   //clientKey
                                }
                           });
                configure(optionBuilder);
                return optionBuilder.Build();
            });
            services.AddSingleton<MqttClientService>();
            services.AddSingleton<IHostedService>(serviceProvider =>
            {
                return serviceProvider.GetService<MqttClientService>();
            });
            services.AddSingleton<MqttClientServiceProvider>(serviceProvider =>
            {
                var mqttClientService = serviceProvider.GetService<MqttClientService>();
                var mqttClientServiceProvider = new MqttClientServiceProvider(mqttClientService);
                return mqttClientServiceProvider;
            });
            return services;
        }
    }
}
