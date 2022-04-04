using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Barragem.Models
{
    public class ModalidadeTorneio
    {
        public ModalidadeTorneio()
        {
            this.CalendarioTorneio = new HashSet<CalendarioTorneio>();
            this.Liga = new HashSet<Liga>();
        }

        public int Id { get; set; }

        public string Nome { get; set; }

        public virtual ICollection<CalendarioTorneio> CalendarioTorneio { get; set; }
        public virtual ICollection<Liga> Liga { get; set; }
    }

    public enum EnumModalidadeTorneio
    {
        SELECIONE = -1,
        TENIS = 1,
        BEACH_TENNIS = 2,
        KIDS = 3
    }
}