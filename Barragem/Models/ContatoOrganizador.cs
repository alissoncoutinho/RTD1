using System.Runtime.Serialization;

namespace Barragem.Models
{
    [DataContract]
    public class ContatoOrganizador
    {
        public ContatoOrganizador()
        {}

        public ContatoOrganizador(string tipoContato, string telefone)
        {
            TipoContato = tipoContato;
            Telefone = telefone;
        }

        [DataMember]
        public string TipoContato { get; private set; }

        [DataMember]
        public string Telefone { get; private set; }
    }
}