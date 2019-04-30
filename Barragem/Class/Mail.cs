using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;

namespace Barragem.Class
{
    public class Mail
    {


        private string HostIP { get; set; }
        private string SMTPPort { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }

        public Mail()
        {
            Usuario = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            Senha = System.Configuration.ConfigurationManager.AppSettings.Get("SenhaMail");
            SMTPPort = System.Configuration.ConfigurationManager.AppSettings.Get("PortaSMTP");
            HostIP = "mail.rankingdetenis.com"; //System.Configuration.ConfigurationManager.AppSettings.Get("ServidorSMTP");
        }

        public Mail(string de, string para, string assunto, string conteudo, Tipos.FormatoEmail formato, List<string> bcc=null)
        {
            this.de = de;
            this.para = para;
            this.assunto = assunto;
            this.conteudo = conteudo;
            this.formato = formato;
            this.bcc = bcc;

            Usuario = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            Senha = System.Configuration.ConfigurationManager.AppSettings.Get("SenhaMail");
            SMTPPort = System.Configuration.ConfigurationManager.AppSettings.Get("PortaSMTP");
            HostIP = "mail.rankingdetenis.com"; //System.Configuration.ConfigurationManager.AppSettings.Get("ServidorSMTP");
        }

        public string de { get; set; }
        public string para { get; set; }
        public string assunto { get; set; }
        public string conteudo { get; set; }
        public Tipos.FormatoEmail formato { get; set; }

        public List<string> bcc { get; set; }
        public void EnviarMail()
        {
            try
            {
                MailMessage m = new MailMessage();
                SmtpClient scMail = new SmtpClient();
                scMail.Host = HostIP;
                scMail.Port = int.Parse(SMTPPort);
                scMail.EnableSsl = false;
                scMail.Credentials = new System.Net.NetworkCredential(Usuario, Senha);
                if (de == string.Empty || de == null)
                    de = this.Usuario;
                MailMessage message = new MailMessage();
                message.From = new MailAddress(de);
                message.To.Add(para);

                if (formato == Tipos.FormatoEmail.Html){
                    message.IsBodyHtml = true;
                }else{
                    message.IsBodyHtml = false;
                }

                message.Body = conteudo;
                message.Subject = assunto;

                if (bcc != null)
                {
                    foreach (string bccEmail in bcc){
                        message.Bcc.Add(new MailAddress(bccEmail)); //Adding Multiple BCC email Id
                    }
                }
                scMail.Send(message);

                message.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        public void SendEmail(string para, string assunto, string conteudo, Tipos.FormatoEmail formato, List<string> bcc = null)
        {
            MailMessage m = new MailMessage();
            SmtpClient sc = new SmtpClient();
            m.From = new MailAddress("postmaster@rankingdetenis.com");
            sc.Host = "mail.rankingdetenis.com";
            sc.Port = 25;
            sc.EnableSsl = false;
            m.To.Add(para);
            m.Subject = assunto;
            m.Body = conteudo;
            if (formato == Tipos.FormatoEmail.Html){
                m.IsBodyHtml = true;
            }else{
                m.IsBodyHtml = false;
            }
            sc.Credentials = new System.Net.NetworkCredential("postmaster@rankingdetenis.com", "@abc5826");
            try{
                sc.Send(m);
            } catch (Exception ex){
                throw ex;
            }
        }
    }
}