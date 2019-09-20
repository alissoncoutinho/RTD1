using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Temporada")]
    public class Temporada
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string nome { get; set; }

        [Display(Name = "Ativo")]
        public bool isAtivo { get; set; }

        public int barragemId { get; set; }

        [ForeignKey("barragemId")]
        public virtual BarragemView barragem { get; set; }

        [Display(Name = "Quantidade de rodadas")]
        public int? qtddRodadas { get; set; }

        [Display(Name = "Data de início")]
        public DateTime? dataInicio { get; set; }

        [Display(Name = "Data de fim")]
        public DateTime? dataFim { get; set; }

        [Display(Name = "Iniciar Temporada do zero")]
        public bool iniciarZerada { get; set; }

        [Display(Name = "Automatizar sorteio")]
        public bool isAutomatico { get; set; }

        [Display(Name = "Frequencia das rodadas: A cada ")]
        public int? frequencia { get; set; }

        [Display(Name = "Dia de geração das rodadas")]
        public int? diaDeGeracao { get; set; }
    }

    
}