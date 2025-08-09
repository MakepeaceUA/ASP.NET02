public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string ImagePath { get; set; } = "";
}

public static class ProductRepository
{
    private static List<Product> products = new();
    private static int nextId = 1;

    public static List<Product> GetAll() => products;

    public static Product? GetById(int id) => products.FirstOrDefault(p => p.Id == id);

    public static void Add(string name, decimal price, string imagePath)
    {
        products.Add(new Product
        {
            Id = nextId++,
            Name = name,
            Price = price,
            ImagePath = imagePath
        });
    }

    public static void Update(int id, string name, decimal price, string imagePath)
    {
        var prod = GetById(id);
        if (prod != null)
        {
            prod.Name = name;
            prod.Price = price;
            if (!string.IsNullOrWhiteSpace(imagePath))
                prod.ImagePath = imagePath;
        }
    }

    public static void Delete(int id)
    {
        var prod = GetById(id);
        if (prod != null) products.Remove(prod);
    }
}