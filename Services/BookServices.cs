using System.Collections.Generic;
using System.Linq;
using demowebapi.Models;
using MongoDB.Driver;

namespace demowebapi.Services {
    public class BookServices {
        private readonly IMongoCollection<Book> _books;

        public BookServices (IBookStoreDBSetting settings) {
            var client = new MongoClient (settings.ConnectionString);
            var db = client.GetDatabase (settings.DatabaseName);
            _books = db.GetCollection<Book> (settings.CollectionName);
        }

        public List<Book> Get () =>
            _books.Find (book => true).ToList ();

        public Book Get (string id) =>
            _books.Find<Book> (book => book.Id == id).FirstOrDefault ();

        public Book Create (Book book) {
            _books.InsertOne (book);
            return book;
        }
        public void Update (string id, Book bookIn) {
            _books.ReplaceOne (book => book.Id == id, bookIn);
        }

        public void Remove (Book bookIn) =>
            _books.DeleteOne (book => book.Id == bookIn.Id);

        public void Remove (string id) =>
            _books.DeleteOne (book => book.Id == id);
    }
}