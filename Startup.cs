using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using demowebapi.Config;
using demowebapi.Filters;
using demowebapi.Models;
using demowebapi.Services;
using EasyCaching.Core;
using EasyCaching.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Utilities;
using Swashbuckle.AspNetCore.Swagger;

namespace demowebapi {
    public class Startup {
        public ILogger Logger { get; }

        public Startup (IConfiguration configuration, ILoggerFactory factory) {
            Configuration = configuration;
            Logger = factory.CreateLogger<Startup> ();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            var fallbackResponse = new HttpResponseMessage ();
            fallbackResponse.Content = new StringContent ("fallback 回调报错：");
            fallbackResponse.StatusCode = System.Net.HttpStatusCode.TooManyRequests;
            //add mongodb
            services.Configure<BookStoreDBSetting> (
                Configuration.GetSection (nameof (BookStoreDBSetting))
            );
            services.AddSingleton<IBookStoreDBSetting> (sp =>
                sp.GetRequiredService<IOptions<BookStoreDBSetting>> ().Value);

            services.AddSingleton<BookServices> ();
            //jwt
            services.AddAuthentication (JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer (
                    options => {
                        options.TokenValidationParameters = new TokenValidationParameters {
                            ValidateLifetime = true, //是否验证失效时间
                                ClockSkew = TimeSpan.FromSeconds (60),

                                ValidateAudience = true, //是否验证Audience
                                //ValidAudience = Const.GetValidudience(),//Audience
                                //这里采用动态验证的方式，在重新登陆时，刷新token，旧token就强制失效了
                                AudienceValidator = (m, n, z) => {
                                    return m != null && m.FirstOrDefault ().Equals (Consts.ValidAudience);
                                },
                                ValidateIssuer = true, //是否验证Issuer
                                ValidIssuer = Consts.Domain, //Issuer，这两项和前面签发jwt的设置一致

                                ValidateIssuerSigningKey = true, //是否验证SecurityKey
                                IssuerSigningKey = new SymmetricSecurityKey (Encoding.UTF8.GetBytes (Consts.SecurityKey)) //拿到SecurityKey

                        };
                    }
                );
            //redis
            //Important step for Redis Caching
            services.AddEasyCaching (option => {
                option.UseRedis (Configuration, "redis1");
            });

            //httpclient
            services.AddHttpClient ();
            services.AddHttpClient ("baidu", c => {
                    c.BaseAddress = new Uri ("http://www.baidu.com/");
                })
                .AddPolicyHandler (Policy<HttpResponseMessage>.Handle<Exception> ().FallbackAsync (fallbackResponse, b => {
                    Logger.LogError ($"fallback here 回调报错：{b.Exception.Message}");
                    return TaskHelper.EmptyTask;
                }))
                //降级
                .AddPolicyHandler (Policy<HttpResponseMessage>.Handle<Exception> ().CircuitBreakerAsync (2, TimeSpan.FromSeconds (4), (ex, ts) => {
                    Logger.LogError ($"break here {ts.TotalMilliseconds}");
                }, () => {
                    Logger.LogError ($"reset here ");
                }))
                .AddPolicyHandler (Policy.TimeoutAsync<HttpResponseMessage> (1));;

            services.AddHttpClient ("cnblogs", c => {
                c.BaseAddress = new Uri ("https://www.cnblogs.com/");
            });
            services.AddMvc (
                Options => {
                    Options.Filters.Add (new LogHttpRequestAttribute ());
                    Options.Filters.Add (typeof (GlobalExceptions));

                }

            ).SetCompatibilityVersion (CompatibilityVersion.Version_2_2);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen (c => {
                c.SwaggerDoc ("v1", new Info { Title = "My API", Version = "v1" });
                c.OperationFilter<AuthTokenHeaderParameter> ();
            });
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IHostingEnvironment env) {
            Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);
            app.UseStaticFiles ();
            app.UseAuthentication ();

            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            } else {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts ();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger ();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI (c => {
                c.SwaggerEndpoint ("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection ();
            app.UseMvc ();
        }
    }
}