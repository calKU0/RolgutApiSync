using RolgutXmlFromApi.Models;
using System.Data.Entity;

namespace RolgutXmlFromApi.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext() : base("name=MyDbContext")
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<CrossNumber> CrossNumbers { get; set; }
        public DbSet<Component> Components { get; set; }
        public DbSet<RecommendedPart> RecommendedParts { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ProductParameter> ProductParameters { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductFile> ProductFiles { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
    }
}