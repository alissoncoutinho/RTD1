using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class PaginaEspecial
    {
        public PaginaEspecial()
        {
            this.Barragem = new HashSet<Barragens>();
        }

        public int Id { get; set; }
        public string Nome { get; set; }

        public virtual ICollection<Barragens> Barragem { get; set; }
    }
}