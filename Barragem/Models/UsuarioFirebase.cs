using System;

namespace Barragem.Models
{
    public class UsuarioFirebase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime DataAtualizacao { get; set; }

        public virtual UserProfile UserProfile { get; set; }
    }
}