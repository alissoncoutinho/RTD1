using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Models
{
    public class PaginaEspecialModel
    {
        
        public EnumPaginaEspecial TipoPaginaEspecial { get; set; }
        public int IdBarragem { get; set; }
        public string NomeBarragem { get; set; }
        public List<Patrocinio> Patrocinadores { get; set; }
        public string Regulamento { get; set; }
        public string Contato { get; set; }
        public string UrlLogo { get; set; }
        public string TextoFilieSeOuQuemSomos { get; set; }
        public string TituloFilieSeOuQuemSomos { get; set; }
        public string ImagemRodape { get; set; }
    }
}