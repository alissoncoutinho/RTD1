using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    [Table("JogoCabecaChave")]
    public class JogoCabecaChave
    {
        [Key]
        public int Id { get; set; }

        public int ordemJogo { get; set; }

        public int cabecaChave { get; set; }

        public int chaveamento { get; set; }

        public bool? temRepescagem { get; set; }

        public bool isFaseGrupo { get; set; }


    }
}