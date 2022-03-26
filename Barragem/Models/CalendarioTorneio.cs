using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Barragem.Models
{
    public class CalendarioTorneio
    {
        public int Id { get; set; }
        public System.DateTime DataInicial { get; set; }
        public System.DateTime DataFinal { get; set; }
        public string Nome { get; set; }
        public int ModalidadeTorneioId { get; set; }
        public string Local { get; set; }
        public int Pontuacao { get; set; }
        public int StatusInscricaoTorneioId { get; set; }
        public string LinkInscricao { get; set; }

        public virtual ModalidadeTorneio ModalidadeTorneio { get; set; }
        public virtual StatusInscricaoTorneio StatusInscricaoTorneio { get; set; }
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