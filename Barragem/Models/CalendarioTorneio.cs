using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Barragem.Models
{
    public class CalendarioTorneio
    {
        public int Id { get; set; }

        [Display(Name = "Inicio")]
        [Required(ErrorMessage = "O campo Inicio é obrigatório")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public System.DateTime DataInicial { get; set; }

        [Display(Name = "Fim")]
        [Required(ErrorMessage = "O campo Fim é obrigatório")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public System.DateTime DataFinal { get; set; }

        [Display(Name = "Nome torneio")]
        [Required(ErrorMessage = "O campo Nome torneio é obrigatório")]
        [MaxLength(150, ErrorMessage = "O campo Nome torneio só pode conter 150 caracteres")]
        public string Nome { get; set; }

        [Display(Name = "Modalidade")]
        [Required(ErrorMessage = "O campo Modalidade é obrigatório")]
        public int ModalidadeTorneioId { get; set; }

        [Display(Name = "Local")]
        [Required(ErrorMessage = "O campo Local é obrigatório")]
        [MaxLength(300, ErrorMessage = "O campo local só pode conter 300 caracteres")]
        public string Local { get; set; }

        [Display(Name = "Pontuação")]
        [Required(ErrorMessage = "O campo Pontuação é obrigatório")]
        [Range(0, 9999999, ErrorMessage = "Pontuação inválida")]
        public int Pontuacao { get; set; }

        [Display(Name = "Inscrição")]
        [Required(ErrorMessage = "O campo Inscrição é obrigatório")]
        public int StatusInscricaoTorneioId { get; set; }

        [Display(Name = "Link Inscrição")]
        public string LinkInscricao
        {
            get { return _LinkInscricao; }
            set
            {
                _LinkInscricao = string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        private string _LinkInscricao;
        
        public int BarragemId { get; set; }

        public virtual ModalidadeTorneio ModalidadeTorneio { get; set; }
        public virtual StatusInscricaoTorneio StatusInscricaoTorneio { get; set; }
        public virtual Barragens Barragem { get; set; }
    }

    public class CalendarioTorneioModel
    {
        public class CalendarioTorneioItens
        {
            public int Id { get; set; }

            [Display(Name = "Mês")]
            public string Mes { get; set; }

            [Display(Name = "Nome")]
            public string Nome { get; set; }

            [Display(Name = "Modalidade")]
            public string ModalidadeTorneio { get; set; }

            [Display(Name = "Pontuação")]
            public int Pontuacao { get; set; }

            [Display(Name = "Inscrição")]
            public string StatusInscricaoTorneio { get; set; }
        }

        public List<CalendarioTorneioItens> Registros { get; set; }
        public List<SelectListItem> FiltroAno { get; set; }
    }
}