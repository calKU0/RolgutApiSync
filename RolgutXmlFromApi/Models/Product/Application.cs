using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIDataSyncXMLGenerator.Models
{
    public class Application
    {
        [Key]
        public int Id { get; set; }

        public int ParentID { get; set; }
        public string Name { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }
    }
}