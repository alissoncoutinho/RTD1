﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Barragem.Models
{
    public class ModalidadeTorneio
    {
        public ModalidadeTorneio()
        {
            this.CalendarioTorneio = new HashSet<CalendarioTorneio>();
        }

        public int Id { get; set; }

        public string Nome { get; set; }

        public virtual ICollection<CalendarioTorneio> CalendarioTorneio { get; set; }
    }
}