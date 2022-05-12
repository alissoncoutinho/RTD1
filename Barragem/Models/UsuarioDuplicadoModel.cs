namespace Barragem.Models
{
    public class UsuarioDuplicadoModel
    {
        public string Email { get; set; }
        public int UsuarioBarragemId { get; set; }
        public int UsuarioTorneioId { get; set; }
        public string NomeUsuarioBarragem { get; set; }
        public string NomeUsuarioTorneio { get; set; }
        public string NomeBarragemUsuarioBarragem { get; set; }
        public string NomeBarragemUsuarioTorneio { get; set; }
    }
}