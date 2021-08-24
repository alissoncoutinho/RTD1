using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Models
{
    [Table("Regra")]
    public class Regra
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "nome")]
        public string nome { get; set; }

        [Display(Name = "descricao")]
        [UIHint("tinymce_full_compressed"), AllowHtml]
        public string descricao { get; set; }
    }
    
}