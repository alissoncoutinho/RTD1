using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Barragem.Models
{
    [Table("Pagamento")]
    public class Pagamento
    {
        [Key]
        public int Id { get; set; }
        public int ano { get; set; }
        public int mes { get; set; }
        public string status { get; set; }
        public double? arrecadado { get; set; }
        public double? areceber { get; set; }
    }
}