using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Barragem.Models
{
    [Table("PagamentoBarragem")]
    public class PagamentoBarragem
    {
        [Key]
        public int Id { get; set; }

        public int barragemId { get; set; }

        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        public int? qtddUsuario { get; set; }

        public string status { get; set; }

        public int pagamentoId { get; set; }

        public bool? cobrar { get; set; }

        public double? valor { get; set; }

        public string linkBoleto { get; set; }

        public string digitableLine { get; set; }
    }
}