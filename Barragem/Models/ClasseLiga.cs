using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("ClasseLiga")]
    public class ClasseLiga
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "nome da classe")]
        [Required(ErrorMessage = "Nome da classe obrigatório")]
        public string Nome { get; set; }

        public int CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public Categoria Categoria { get; set; }

        public int LigaId { get; set; }

        [ForeignKey("LigaId")]
        public Liga Liga { get; set; }
    }
}