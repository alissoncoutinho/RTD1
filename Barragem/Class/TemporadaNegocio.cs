using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;
using System.Data.Entity;
using System.Web.Http;
using System.Text;

namespace Barragem.Class
{
    public class TemporadaNegocio
    {
        private BarragemDbContext db = new BarragemDbContext();

        [HttpGet]
        public void GerarRodadasAutomaticas()
        {
            DateTime hoje = DateTime.Now.Date;
            hoje = new DateTime(2019, 09, 29).Date;
            RodadaNegocio rodadaNegocio = new RodadaNegocio();
            Dictionary<Temporada, string> temporadasComErro = new Dictionary<Temporada, string>();

            List<Temporada> temporadasQueIniciamHoje = db.Temporada.Where(t => t.isAutomatico
                       && t.dataInicio == hoje).ToList();
            //para cada temporada
            //  1. tratar zeramento do ranking se for o caso(a implementar)
            //  2.fechar rodada
            //  3.criar nova rodada
            //  4.sortear jogos da rodada
            //  5.enviar email
            foreach (Temporada temporada in temporadasQueIniciamHoje)
            {
                try
                {
                    List<Rodada> rodadas = db.Rodada.Where(r => r.temporadaId == temporada.Id).ToList();
                    if (rodadas.Count == 0)
                    {
                        criarNovaRodadaComJogos(rodadaNegocio, temporada, hoje);
                    }
                }catch(Exception e)
                {
                    temporadasComErro.Add(temporada, e.Message);
                }
                
            }

            List<Rodada> rodadasQueEncerramHoje = db.Rodada.Where(r => DbFunctions.TruncateTime(r.dataFim)==hoje && r.isAberta).ToList();
            foreach(Rodada rodada in rodadasQueEncerramHoje)
            {
                Temporada temporada = rodada.temporada;
                try
                {
                    if (temporada.isAutomatico)
                    {
                        if (hoje.AddDays(temporada.tamanhoRodada.Value) < temporada.dataFim)
                        {
                            rodadaNegocio.FecharRodada(rodada.Id);
                            criarNovaRodadaComJogos(rodadaNegocio, temporada, hoje);
                        }
                        else
                        {
                            temporadasComErro.Add(temporada, "Neste caso, a rodada terminaria depois da temporada. Ajustar o tamanho da rodada ou o fim da temporada");
                        }
                    }
                }catch(Exception e)
                {
                    temporadasComErro.Add(temporada, e.Message);
                }
            }

            if(temporadasComErro.Count > 0)
            {
                notificarErros(temporadasComErro);
            }
        }

        private void criarNovaRodadaComJogos(RodadaNegocio rodadaNegocio, Temporada temporada, DateTime hoje)
        {
            Rodada novaRodada = new Rodada();
            novaRodada.temporadaId = temporada.Id;
            novaRodada.barragemId = temporada.barragemId;
            novaRodada.dataInicio = hoje;
            novaRodada.dataFim = hoje.AddDays(temporada.tamanhoRodada.Value);
            rodadaNegocio.Create(novaRodada);
            rodadaNegocio.SortearJogos(novaRodada.Id, temporada.barragemId);
            notificarOrganizadorPorEmail(temporada, novaRodada, null);
        }

        private void notificarOrganizadorPorEmail(Temporada temporada , Rodada rodada, string mensagemErro)
        {
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                var barragem = temporada.barragem;
                if (barragem.email.Equals(""))
                {
                    mail.para = "barragemdocerrado@gmail.com";
                }
                else
                {
                    mail.para = barragem.email;
                }
                if (mensagemErro != null)
                {
                    mail.assunto = "Houve um erro ao gerar a rodada";
                    mail.conteudo = barragem.nomeResponsavel 
                        + ",<br>Um email foi enviado ao administrador.<br>Houve um erro ao gerar a rodada:<br>" + mensagemErro;
                }
                else
                {
                    mail.assunto = "Uma nova rodada foi gerada!";
                    mail.conteudo = barragem.nomeResponsavel + ",<br> Uma nova rodada com início em " + rodada.dataInicio
                            + "e fim em " + rodada.dataFim + " foi gerada.";
                }
                mail.formato = Tipos.FormatoEmail.Html;
                mail.EnviarMail();
            }catch(Exception e)
            {
                //deixa rolar...
            }
            
        }

        private void notificarErros(Dictionary<Temporada, string> temporadasComErro)
        {
            StringBuilder mensagemParaAdm = new StringBuilder("Erros");
            foreach (KeyValuePair<Temporada, string> temp in temporadasComErro)
            {
                string erro = "<br>Barragem " + temp.Key + " com o erro: " + temp.Value;
                notificarOrganizadorPorEmail(temp.Key, null, erro);
                mensagemParaAdm.Append(erro);
            }
            notificaAdministradorPorEmail(mensagemParaAdm.ToString());
        }

        private void notificaAdministradorPorEmail(string mensagem)
        {
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                mail.para = "barragemdocerrado@gmail.com";
                mail.assunto = "Atenção: houve erros ao gerar rodadas automaticamente.";
                mail.conteudo = mensagem;
                mail.formato = Tipos.FormatoEmail.Html;
                mail.EnviarMail();
            }catch(Exception e)
            {
                //deixa rolar...
            }
            
        }        
    }
}