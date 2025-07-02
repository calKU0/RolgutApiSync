using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIDataSyncXMLGenerator.Models
{
    public class Package
    {
        [Key]
        public int Id { get; set; }

        public string PackUnit { get; set; }
        public float PackQty { get; set; }
        public float PackNettWeight { get; set; }
        public float PackGrossWeight { get; set; }
        public string PackEan { get; set; }
        public int PackRequired { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }
    }
}