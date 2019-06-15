namespace demowebapi.Models {
    public class BookStoreDBSetting : IBookStoreDBSetting {
        public string CollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IBookStoreDBSetting {
        string CollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}