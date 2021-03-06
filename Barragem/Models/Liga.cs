﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Barragem.Models
{
    [Table("Liga")]
    public class Liga
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "Nome da liga")]
        [Required(ErrorMessage = "Nome obrigatório")]
        public string Nome { get; set; }
    }
}