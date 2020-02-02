using Amazon.DynamoDBv2;
using JKang.EventSourcing.Persistence;
using JKang.EventSourcing.Persistence.CosmosDB;
using JKang.EventSourcing.Snapshotting.Persistence;
using JKang.EventSourcing.TestingFixtures;
using JKang.EventSourcing.TestingWebApp.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace JKang.EventSourcing.TestingWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services
                .AddScoped<IGiftCardRepository, GiftCardRepository>();

            // change the following value to switch persistence mode
            PersistenceMode persistenceMode = PersistenceMode.DynamoDB;

            switch (persistenceMode)
            {
                case PersistenceMode.FileSystem:
                    ConfigureServicesForFileSystem(services);
                    break;
                case PersistenceMode.CosmosDB:
                    ConfigureServicesForCosmosDB(services);
                    break;
                case PersistenceMode.DynamoDB:
                    ConfigureServicesForDynamoDB(services);
                    break;
                case PersistenceMode.EfCore:
                    ConfigureServicesForEfCore(services);
                    break;
                default:
                    break;
            }
        }

        public void ConfigureServicesForFileSystem(IServiceCollection services)
        {
            services.AddEventSourcing(builder =>
                builder
                    .UseTextFileEventStore<GiftCard, Guid>(x => x.Folder = "C:/Temp/GiftcardEvents")
                    .UseTextFileSnapshotStore<GiftCard, Guid>(x => x.Folder = "C:/Temp/GiftcardEvents"));
        }

        public void ConfigureServicesForCosmosDB(IServiceCollection services)
        {
            services
                .AddSingleton(_ =>
                    new CosmosClientBuilder(Configuration.GetConnectionString("CosmosDB"))
                        .WithConnectionModeDirect()
                        .WithCustomSerializer(new EventSourcingCosmosSerializer())
                        .Build())
                .AddEventSourcing(builder =>
                    builder
                        .UseCosmosDBEventStore<GiftCard, Guid>(x =>
                        {
                            x.DatabaseId = "EventSourcingTestingWebApp";
                            x.ContainerId = "GiftcardEvents";
                        })
                        .UseCosmosDBSnapshotStore<GiftCard, Guid>(x =>
                        {
                            x.DatabaseId = "EventSourcingTestingWebApp";
                            x.ContainerId = "GiftcardSnapshots";
                        }));
        }

        public void ConfigureServicesForDynamoDB(IServiceCollection services)
        {
#if DEBUG
            services.AddSingleton<IAmazonDynamoDB>(sp => new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8800"
            }));
#else
            services.AddAWSService<IAmazonDynamoDB>();
#endif

            services.AddEventSourcing(builder =>
            {
                builder
                    .UseDynamoDBEventStore<GiftCard, Guid>(x => x.TableName = "GiftcardEvents")
                    .UseDynamoDBSnapshotStore<GiftCard, Guid>(x => x.TableName = "GiftcardSnapshots")
                    ;
            });
        }

        public void ConfigureServicesForEfCore(IServiceCollection services)
        {
            services
                .AddDbContext<SampleDbContext>(x => x.UseInMemoryDatabase("eventstore"))
                .AddEventSourcing(builder =>
                {
                    builder.UseDbEventStore<SampleDbContext, GiftCard, Guid>();
                })
            ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env,
            IEventStoreInitializer<GiftCard, Guid> eventStoreInitializer,
            ISnapshotStoreInitializer<GiftCard, Guid> snapshotStoreInitializer)
        {
            eventStoreInitializer.EnsureCreatedAsync().Wait();
            snapshotStoreInitializer.EnsureCreatedAsync().Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
