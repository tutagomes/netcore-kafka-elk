using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using MassTransit.KafkaIntegration;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Observador
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var elasticUri = Configuration["ElasticConfiguration:Uri"];
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
                {
                    AutoRegisterTemplate = true,
                })
            .CreateLogger();

            var config = Configuration.GetSection("Kafka");
            
            services.AddMassTransit(x =>
            {
                // Adicionando BUS em memória para organizar mensagens recebidas do Kafka
                x.UsingInMemory((context,config) => config.ConfigureEndpoints(context));

                x.AddRider(rider =>
                {
                    // Configurando o Namespace dos consumidores - necessário em todos os cenários
                    rider.AddConsumersFromNamespaceContaining<Observador.Consumidor.TopicConsumer>();
                    // Injetando configuração do Kafka
                    rider.UsingKafka((riderContext, kafkaConfigurator) =>
                    {
                        kafkaConfigurator.Host(config["Host"]);

                        // Configurando consumer group Consumidor
                        kafkaConfigurator.TopicEndpoint<Consumidor.Mensagem>(config["TopicName"], config["ConsumerName"], topicConfig =>
                        {
                            // Configura para recuperar sempre o OffSet mais antigo. Por padrão, pega o OffSet mais novo
                            topicConfig.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;

                            // Configuração de intervalo
                            topicConfig.MaxPollInterval = TimeSpan.FromMilliseconds(300000);

                            // Confiugração de publicação de erros
                            topicConfig.PublishFaults = false;

                            // Configurando consumo de Mensagem
                            topicConfig.ConfigureConsumer<Consumidor.TopicConsumer>(riderContext, consumerConfig => {

                                // Limitar a quantidade de mensagens concorrentes
                                consumerConfig.UseConcurrentMessageLimit(200);

                            });
                        });
                    });
                    rider.AddProducer<Consumidor.MensagemErro>(config["TopicError"], (registrationContext, producerConfigurator) =>
                    {
                        // Para garantir que todos os eventos emitidos são enviados na ordem original de produção
                        producerConfigurator.EnableIdempotence = true;

                        // Tempo de espera para envio de batch de mensagens - default: 0.5s
                        producerConfigurator.Linger = TimeSpan.FromSeconds(0.5);

                        // Formato de distribuição das mensagens em partições
                        producerConfigurator.Partitioner = Confluent.Kafka.Partitioner.ConsistentRandom;

                    });
                });
            });
            services.AddMassTransitHostedService(true);
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // Desabilitado para testes internos
                // app.UseHsts();
            }
            // Desabilitado para testes internos
            // app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            loggerFactory.AddSerilog();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
