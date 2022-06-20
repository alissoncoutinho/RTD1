namespace Barragem.Models
{
    public class ClasseGrupoSeguidoMataMataModel
    {
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
        public string NomeClasse { get; set; }
        public int QtdeInscricoes { get; set; }
    }

    public class ClasseGrupoSeguidoMataMataRequestModel
    {
        public int IdTorneio { get; set; }
        public int IdClasse { get; set; }
        public int ConfigSelecionadaClasse { get; set; }
    }

    
}