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
    public class Component
    {
        [Key]
        public int Id { get; set; }

        public int TwrID { get; set; }
        public string CodeGaska { get; set; }
        public float Qty { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }
    }
}