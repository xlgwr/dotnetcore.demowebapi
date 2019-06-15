using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using demowebapi.Filters;
using demowebapi.Models;
using demowebapi.Services;
using EasyCaching.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace demowebapi.Controllers {
    [Authorize]
    [Route ("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase {
        private readonly ILogger<ValuesController> _logger;
        private IHttpClientFactory _httpClientFactory;

        private readonly IEasyCachingProvider _provider;
        private readonly IRedisCachingProvider _redisProvider;

        private readonly BookServices _bookService;

        public ValuesController (
            ILogger<ValuesController> logger,
            IHttpClientFactory httpClientFactory,
            IEasyCachingProvider provider,
            IEasyCachingProviderFactory providerFactory,
            BookServices bookService
        ) {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            this._provider = provider;
            this._redisProvider = providerFactory.GetRedisProvider ("redis1");
            _bookService = bookService;
        }
        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get () {
            _logger.LogInformation ("正在调用接口 GET api/values ");

            var client = _httpClientFactory.CreateClient ("cnblogs");
            var result = await client.GetStringAsync ("xlgwr");

            //Remove
            _provider.Remove ("demo");

            //Set
            _provider.Set ("demo", result, TimeSpan.FromMinutes (1));

            return new string[] { "value1", "value2", result };
        }

        // GET api/values/5
        [HttpGet ("{id}")]
        public async Task<ActionResult<object>> Get (int id) {
            //get 

            var client = _httpClientFactory.CreateClient ("baidu");
            var content = new string ("s?ie=utf-8&f=8&rsv_bp=1&rsv_idx=1&tn=baidu&wd=" + id);
            var result = await client.GetStringAsync (content);

            //Set
            _provider.Set (id + "", result, TimeSpan.FromMinutes (1));

            Thread.Sleep (id);
            return new { id, result };
        }

        // POST api/values
        [HttpPost]
        public void Post ([FromBody] string value) { }

        // PUT api/values/5
        [HttpPut ("{id}")]
        public void Put (int id, [FromBody] string value) { }

        // DELETE api/values/5
        [HttpDelete ("{id}")]
        public void Delete (int id) { }

        #region books
        [HttpGet ("/books")]
        public ActionResult<List<Book>> GetBooks () =>
            _bookService.Get ();

        [HttpGet ("/books/{id:length(24)}", Name = "GetBook")]
        public ActionResult<Book> GetBook (string id) {
            var book = _bookService.Get (id);

            if (book == null) {
                return NotFound ();
            }
            return book;
        }

        [HttpPost ("/books")]
        public ActionResult<Book> CreateBook (Book book) {
            if (book.Id.IsNullToString ().Length != 24) {
                book.Id = "";
            }
            _bookService.Create (book);

            return CreatedAtRoute ("GetBook", new { id = book.Id.ToString () }, book);
        }

        [HttpPut ("/books/{id:length(24)}")]
        public IActionResult UpdateBook (string id, Book bookIn) {
            var book = _bookService.Get (id);

            if (book == null) {
                return NotFound ();
            }

            _bookService.Update (id, bookIn);

            return NoContent ();
        }

        [HttpDelete ("/books/{id:length(24)}")]
        public IActionResult DeleteBook (string id) {
            var book = _bookService.Get (id);

            if (book == null) {
                return NotFound ();
            }

            _bookService.Remove (book.Id);

            return NoContent ();
        }
        #endregion
    }
}