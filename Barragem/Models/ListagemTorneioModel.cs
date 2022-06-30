using Barragem.Class;
using System;
using System.ComponentModel.DataAnnotations;

namespace Barragem.Models
{
    public class ListagemTorneioModel
    {
        public int Id { get; set; }

        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Display(Name = "Data Início")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [DataType(DataType.Date)]
        public DateTime DataInicio { get; set; }

        [Display(Name = "Ranking")]
        public string NomeBarragem { get; set; }

        [Display(Name = "Esporte")]
        public string TipoBarragem { get; set; }

        public string TelefoneCelular { get; set; }
        public string NomeUsuarioAdmin { get; set; }
        public string LinkWhatsApp
        {
            get
            {
                return "https://api.whatsapp.com/send?phone=55" + TelefoneCelular.OnlyDigits() + "&text=Olá,%20" + NomeUsuarioAdmin + " ";
            }
        }
    }
}