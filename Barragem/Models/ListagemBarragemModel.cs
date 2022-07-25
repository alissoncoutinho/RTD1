using Barragem.Class;
using System.ComponentModel.DataAnnotations;

namespace Barragem.Models
{
    public class ListagemBarragemModel
    {
        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Display(Name = "Esporte")]
        public string TipoBarragem { get; set; }

        [Display(Name = "Situação")]
        public bool IsAtiva { get; set; }

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