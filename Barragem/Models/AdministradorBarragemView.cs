using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barragem.Models
{
    [Table("AdministradorBarragemView")]
    public class AdministradorBarragemView
    {
        [Key]
        public int idBarragem { get; set; }
        public string nomeBarragem { get; set; }
        public int userId { get; set; }
        public string userName { get; set; }
        public string telefone { get; set; }
    }
}