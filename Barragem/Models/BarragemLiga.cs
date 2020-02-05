using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class BarragemLiga
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BarragemId { get; set; }

        [ForeignKey("BarragemId")]
        public virtual Barragens Barragem { get; set; }

        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga Liga { get; set; }

        public string TipoTorneio { get; set; }
    }
}