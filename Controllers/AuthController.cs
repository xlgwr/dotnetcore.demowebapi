using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using demowebapi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EasyCaching.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

using demowebapi.Config;
using System.Security.Claims;
using System.Text;

namespace demowebapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private IHttpClientFactory _httpClientFactory;

        private readonly IEasyCachingProvider _provider;
        private readonly IRedisCachingProvider _redisProvider;


        public AuthController(
            ILogger<AuthController> logger,
        IHttpClientFactory httpClientFactory,
        IEasyCachingProvider provider,
        IEasyCachingProviderFactory providerFactory
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            this._provider = provider;
            this._redisProvider = providerFactory.GetRedisProvider("redis1");
        }
        // GET api/Auth
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Get(string userName, string pwd)
        {
            _logger.LogInformation("正在调用接口 GET api/Auth ");
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(pwd))
            {
                return BadRequest(new { message = "username or password is incorrect." });
            }
            //每次登陆动态刷新
            Consts.ValidAudience = userName + pwd + DateTime.Now.ToString();

            var claims = new[]{
                    new Claim(JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}") ,
                    new Claim (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddMinutes(30)).ToUnixTimeSeconds()}"),
                    new Claim(ClaimTypes.NameIdentifier, userName),
                    new Claim("Role","1")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Consts.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jtoken = new JwtSecurityToken(
                issuer: Consts.Domain,
                audience: Consts.ValidAudience,
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: creds,
                claims: claims
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jtoken);
            _redisProvider.StringSet(userName, token,TimeSpan.FromMinutes(2));
            return Ok(
                new
                {
                    token = token
                }
            );
        }
    }
}
