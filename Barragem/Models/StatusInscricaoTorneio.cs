using System.Collections.Generic;

namespace Barragem.Models
{
    public class StatusInscricaoTorneio
    {
        public StatusInscricaoTorneio()
        {
            this.CalendarioTorneio = new HashSet<CalendarioTorneio>();
        }

        public int Id { get; set; }
        public string Nome { get; set; }

        public virtual ICollection<CalendarioTorneio> CalendarioTorneio { get; set; }
    }
}