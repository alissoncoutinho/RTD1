using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace Barragem.Models
{
    [Table("Patrocinio")]
    public class Patrocinio
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Imagem")]
        public string UrlImagem { get; set; }

        [Display(Name = "Url")]
        public string UrlPatrocinador { get; set; }
    }

    public class PatrocinioModel
    {
        public int Id { get; set; }

        public string UrlImagem { get; set; }

        public string UrlPatrocinador { get; set; }

        public HttpPostedFileBase FileImage { get; set; }
    }
}