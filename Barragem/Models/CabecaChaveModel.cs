namespace Barragem.Models
{
    public class CabecaChaveModel
    {
        public int IdInscricao { get; set; }
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
        public int IdParticipante { get; set; }
        public string UserNameParticipante { get; set; }
        public string NomeParticipante { get; set; }
        public bool InscricaoParticipantePaga { get; set; }
        public bool EhDupla { get; set; }
        public int IdParceiroDupla { get; set; }
        public string UserNameParceiroDupla { get; set; }
        public string NomeParceiroDupla { get; set; }
        public bool InscricaoParceiroDuplaPaga { get; set; }
        public bool TodaInscricaoPaga { get; set; }
        public int? CabecaChave { get; set; }
    }
}