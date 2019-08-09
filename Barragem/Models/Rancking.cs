using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
        
    [Table("Rancking")]
    public class Rancking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Rodada")]
        [Required(ErrorMessage = "Campo rodada obrigatório")]
        public int rodada_id { get; set; }

        [Display(Name = "Pontuação")]
        [Required(ErrorMessage = "Campo pontuação obrigatório")]
        public double pontuacao { get; set; }

        [Display(Name = "Total de Pontos")]
        public double totalAcumulado { get; set; }

        [Display(Name = "Posição")]
        public int posicao { get; set; }

        [Display(Name = "Participante")]
        public int userProfile_id { get; set; }

        [Display(Name = "Participante")]
        [ForeignKey("userProfile_id")]
        public virtual UserProfile userProfile { get; set; }

        [Display(Name = "Classe")]
        public int? classeId { get; set; }

        [Display(Name = "Classe")]
        [ForeignKey("classeId")]
        public virtual Classe classe { get; set; }

        [Display(Name = "Posição Classe")]
        public int? posicaoClasse { get; set; }

        [Display(Name = "Rodada")]
        [ForeignKey("rodada_id")]
        public virtual Rodada rodada { get; set; }


    }

    public class Classificacao
    {
        public double pontuacao { get; set; }
        public double totalAcumulado { get; set; }
        public int? posicaoUser { get; set; }

        public int userId { get; set; }
        public string nomeUser { get; set; }
        public string rodada { get; set; }
        public int rodadaId { get; set; }
        public DateTime dataRodada { get; set; }
        public string jogoAtrasado { get; set; }
        public string foto { get; set; }

    }

    public class Cabecalho
    {
        public string rodada { get; set; }
        public DateTime dataRodada { get; set; }
        public string temporada { get; set; }
        public IList<Classe> classes { get; set; }
        public int classeUserId { get; set; }

    }

    public class MinhaPontuacao
    {
        public string nomeUser { get; set; }
        public int? posicao { get; set; }
        public double pontuacaoAtual { get; set; }
        public IList<Classificacao> classificacao { get; set; }
    }
}