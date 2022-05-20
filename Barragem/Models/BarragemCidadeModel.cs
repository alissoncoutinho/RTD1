using System.Collections.Generic;

namespace Barragem.Models
{
    public class BarragemCidadeModel
    {
        public int IdBarragem { get; set; }
        public string NomeBarragem { get; set; }
        public List<Classe> Classes { get; set; }
    }
}