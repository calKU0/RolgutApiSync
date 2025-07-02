using APIDataSyncXMLGenerator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolgutXmlFromApi.Models
{
    public class ProductCategory
    {
        [Key]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }
    }
}