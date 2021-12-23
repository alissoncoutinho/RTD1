using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;
using System.Text;

namespace Barragem.Class
{
    public static class Funcoes
    {

        private static bool EmailInvalid = false;

        public static Uri UrlOriginal(this HttpRequestBase request)
        {
            string hostHeader = request.Headers["host"];

            return new Uri(string.Format("{0}://{1}{2}",
               request.Url.Scheme,
               hostHeader,
               request.RawUrl));
        }

        public static bool isCPF(string cpf)
        {
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;
            tempCpf = cpf.Substring(0, 9);
            soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;
            digito = digito + resto.ToString();
            return cpf.EndsWith(digito);
        }

        public static bool ValidateEmail(string email)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            return match.Success;
        }

        /// <summary>
        /// reference: http://msdn.microsoft.com/en-us/library/01escwtf.aspx
        /// </summary>
        /// <param name="email"></param>
        /// <returns>true se email valido</returns>
        public static bool IsValidEmail(string email)
        {
            EmailInvalid = false;
            if (String.IsNullOrEmpty(email))
                return false;

            // Use IdnMapping class to convert Unicode domain names. 
            try
            {
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200)).Trim(' ');
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }

            if (EmailInvalid)
                return false;

            // Return true if email is in valid e-mail format. 
            try
            {
                return Regex.IsMatch(email,
                      @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
        private static string DomainMapper(Match match)
        {
            IdnMapping idn = new IdnMapping();

            string domainName = match.Groups[2].Value;
            try
            {
                domainName = idn.GetAscii(domainName);
            }
            catch (ArgumentException)
            {
                EmailInvalid = true;
            }
            return match.Groups[1].Value + domainName;
        }

        public static void CriarCookieBarragem(HttpResponseBase Response,HttpServerUtilityBase Server, int id, string nome, bool beachTennis=false) {
            //Cria a estancia do obj HttpCookie passando o nome do mesmo
            HttpCookie cookie = new HttpCookie("_barragemId");
            cookie.Value = id + "";
            cookie.Expires = DateTime.Now.AddDays(20);
            Response.Cookies.Add(cookie);

            HttpCookie cookieNome = new HttpCookie("_barragemNome");
            cookieNome.Value = Server.UrlEncode(nome);
            cookieNome.Expires = DateTime.Now.AddDays(20);
            Response.Cookies.Add(cookieNome);

            HttpCookie cookieBeachTennis = new HttpCookie("_barragemBeach");
            if (beachTennis) {
                cookieBeachTennis.Value = Server.UrlEncode("1");
            } else {
                cookieBeachTennis.Value = Server.UrlEncode("0");
            }
            
            cookieBeachTennis.Expires = DateTime.Now.AddDays(20);
            Response.Cookies.Add(cookieBeachTennis);
        }

        public static string RemoveAcentosEspacosMaiusculas(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 255)
                    sb.Append(text[i]);
                else
                    sb.Append(s_Diacritics[text[i]]);
            }

            return removerEspacos(sb.ToString()).ToLower();
        }

        private static readonly char[] s_Diacritics = GetDiacritics();
        private static char[] GetDiacritics()
        {
            char[] accents = new char[256];

            for (int i = 0; i < 256; i++)
                accents[i] = (char)i;

            accents[(byte)'á'] = accents[(byte)'à'] = accents[(byte)'ã'] = accents[(byte)'â'] = accents[(byte)'ä'] = 'a';
            accents[(byte)'Á'] = accents[(byte)'À'] = accents[(byte)'Ã'] = accents[(byte)'Â'] = accents[(byte)'Ä'] = 'A';

            accents[(byte)'é'] = accents[(byte)'è'] = accents[(byte)'ê'] = accents[(byte)'ë'] = 'e';
            accents[(byte)'É'] = accents[(byte)'È'] = accents[(byte)'Ê'] = accents[(byte)'Ë'] = 'E';

            accents[(byte)'í'] = accents[(byte)'ì'] = accents[(byte)'î'] = accents[(byte)'ï'] = 'i';
            accents[(byte)'Í'] = accents[(byte)'Ì'] = accents[(byte)'Î'] = accents[(byte)'Ï'] = 'I';

            accents[(byte)'ó'] = accents[(byte)'ò'] = accents[(byte)'ô'] = accents[(byte)'õ'] = accents[(byte)'ö'] = 'o';
            accents[(byte)'Ó'] = accents[(byte)'Ò'] = accents[(byte)'Ô'] = accents[(byte)'Õ'] = accents[(byte)'Ö'] = 'O';

            accents[(byte)'ú'] = accents[(byte)'ù'] = accents[(byte)'û'] = accents[(byte)'ü'] = 'u';
            accents[(byte)'Ú'] = accents[(byte)'Ù'] = accents[(byte)'Û'] = accents[(byte)'Ü'] = 'U';

            accents[(byte)'ç'] = 'c';
            accents[(byte)'Ç'] = 'C';

            accents[(byte)'ñ'] = 'n';
            accents[(byte)'Ñ'] = 'N';

            accents[(byte)'ÿ'] = accents[(byte)'ý'] = 'y';
            accents[(byte)'Ý'] = 'Y';

            return accents;
        }

        private static string removerEspacos(string texto)
        {
            string semEspaco = ""; // Declaramos o futuro resultado
            foreach (char c in texto.ToCharArray()) // Para cada 'letra' na frase
            {
                if (c != ' ') // Se a letra não for um espaço
                    semEspaco += c; // É adicionada a string final
            }
            return semEspaco;
        }
    }
}