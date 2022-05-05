namespace Barragem.Class
{
    public class FaseTorneio
    {
        public static string ObterAbreviacao(int? grupoFaseGrupo, int? faseTorneio, int rodadaFaseGrupo) 
        {
            if (grupoFaseGrupo != null)
            {
                return rodadaFaseGrupo + "ªR: GR" + grupoFaseGrupo;
            }
            if (faseTorneio == null)
            {
                return "";
            }
            if (faseTorneio == 101)
            {
                return "Fase 1";
            }
            if (faseTorneio == 100)
            {
                return "Repescagem";
            }
            if (faseTorneio == 6)
            {
                return "R1";
            }
            if (faseTorneio == 5)
            {
                return "R2";
            }
            if (faseTorneio == 4)
            {
                return "OF";
            }
            if (faseTorneio == 3)
            {
                return "QF";
            }
            if (faseTorneio == 2)
            {
                return "SF";
            }
            if (faseTorneio == 1)
            {
                return "Final";
            }
            return "";
        }
    }
}