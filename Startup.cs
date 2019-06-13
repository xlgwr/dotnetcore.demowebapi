using System.Net.Http;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using demowebapi.Filters;
using Polly;
using Polly.Utilities;
using Microsoft.AspNetCore.Http;
using EasyCaching.Core;
using EasyCaching.Redis;

namespace demowebapi
{
    public class Startup
    {
        public ILogger Logger { get; }

        public Startup(IConfiguration configuration, ILoggerFactory factory)
        {
            Configuration = configuration;
            Logger = factory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var fallbackResponse = new HttpResponseMessage();
            fallbackResponse.Content = new StringContent("fallback 回调报错：");
            fallbackResponse.StatusCode = System.Net.HttpStatusCode.TooManyRequests;

            //redis
            //Important step for Redis Caching
            services.AddEasyCaching(option =>
            {
                option.UseRedis(Configuration,"redis1");
            });

            //httpclient
            services.AddHttpClient();
            services.AddHttpClient("baidu", c =>
            {
                c.BaseAddress = new Uri("http://www.baidu.com/");
            })
            .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<Exception>().FallbackAsync(fallbackResponse, b =>
           {
               Logger.LogError($"fallback here 回调报错：{b.Exception.Message}");
               return TaskHelper.EmptyTask;
           }))
            //降级
            .AddPolicyHandler(Policy<HttpResponseMessage>.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromSeconds(4), (ex, ts) =>
            {
                Logger.LogError($"break here {ts.TotalMilliseconds}");
            }, () =>
            {
                Logger.LogError($"reset here ");
            }))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
            ;

            services.AddHttpClient("cnblogs", c =>
           {
               c.BaseAddress = new Uri("https://www.cnblogs.com/");
           });
            services.AddMvc(
                Options =>
                {
                    Options.Filters.Add(new LogHttpRequestAttribute());
                    Options.Filters.Add(typeof(GlobalExceptions));

                }

            ).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
