namespace Barragem.Models
{
    public class InscricaoModel
    {
        public int UserId { get; set; }
        public int TorneioId { get; set; }

        public int IdCategoria1 { get; set; }
        public int IdInscricaoParceiroDupla1 { get; set; }

        public int IdCategoria2 { get; set; }
        public int IdInscricaoParceiroDupla2 { get; set; }

        public int IdCategoria3 { get; set; }
        public int IdInscricaoParceiroDupla3 { get; set; }

        public int IdCategoria4 { get; set; }
        public int IdInscricaoParceiroDupla4 { get; set; }

        public string Observacao { get; set; }
        public bool IsSocio { get; set; }
        public bool IsFederado { get; set; }
    }
}