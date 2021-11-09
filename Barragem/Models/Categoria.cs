using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("Categoria")]
    public class Categoria
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Categoria")]
        [Required(ErrorMessage = "Categoria")]
        public string Nome { get; set; }

        public bool isDupla { get; set; }

        public int ordemExibicao { get; set; }

        public int rankingId { get; set; }
    }
    // ALTER TABLE Categoria ADD rankingId int NOT NULL DEFAULT(0);
    public class CategoriaDeLiga
    {
        public int Id { get; set; }

        public string Nome { get; set; }

        public List<String> Ligas { get; set; }

        
    }
}