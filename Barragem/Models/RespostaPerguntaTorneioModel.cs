namespace Barragem.Models
{
    public class RespostaPerguntaTorneioModel
    {
        public string Nome { get; set; }
        public string UserName { get; set; }
        public string TelefoneCelular { get; set; }
        public string Resposta { get; set; }

        public virtual string Linkwhatsapp
        {
            get
            {
                var i = TelefoneCelular.IndexOf("/");
                var dddcel = "";
                if (i < 1)
                {
                    dddcel = TelefoneCelular.Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
                }
                else
                {
                    dddcel = TelefoneCelular.Substring(0, i).Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
                }

                return "https://api.whatsapp.com/send?phone=55" + dddcel + "&text=Olá,%20" + Nome + " ";
            }
        }
    }
}