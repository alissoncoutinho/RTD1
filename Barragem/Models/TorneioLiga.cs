using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("TorneioLiga")]
    public class TorneioLiga
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TorneioId { get; set; }

        [ForeignKey("TorneioId")]
        public Torneio Torneio { get; set; }

        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga Liga { get; set; }

        public int? snapshotId { get; set; }
                
    }
}