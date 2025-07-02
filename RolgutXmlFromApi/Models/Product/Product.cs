using RolgutXmlFromApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Component = RolgutXmlFromApi.Models.Component;

namespace APIDataSyncXMLGenerator.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string CodeGaska { get; set; }
        public string CodeCustomer { get; set; }
        public string Name { get; set; }
        public string SupplierName { get; set; }
        public string SupplierLogo { get; set; }
        public float InStock { get; set; }
        public string CurrencyPrice { get; set; }
        public decimal PriceNet { get; set; }
        public decimal PriceGross { get; set; }

        public virtual ICollection<Package> Packages { get; set; }
        public virtual ICollection<CrossNumber> CrossNumbers { get; set; }
        public virtual ICollection<Component> Components { get; set; }
        public virtual ICollection<RecommendedPart> RecommendedParts { get; set; }
        public virtual ICollection<Application> Applications { get; set; }
        public virtual ICollection<ProductParameter> Parameters { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; }
        public virtual ICollection<ProductFile> Files { get; set; }
        public virtual ICollection<ProductCategory> Categories { get; set; }
    }
}