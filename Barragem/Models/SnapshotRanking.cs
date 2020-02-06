using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("SnapshotRanking")]
    public class SnapshotRanking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SnapshotId { get; set; }

        [ForeignKey("SnapshotId")]
        public Snapshot Snapshot { get; set; }

        [Display(Name = "Jogador")]
        public int UserId { get; set; }

        [Display(Name = "Jogador")]
        [ForeignKey("UserId")]
        public virtual UserProfile Jogador { get; set; }

        public int CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public Categoria Categoria { get; set; }
        
        public int Posicao { get; set; }

        public int Pontuacao { get; set; }
    }
}