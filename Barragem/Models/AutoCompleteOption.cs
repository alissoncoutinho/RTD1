using Newtonsoft.Json;

namespace Barragem.Models
{
    public class AutoCompleteOption
    {
        public AutoCompleteOption(string group, string text, string value)
        {
            this.group = group;
            this.text = text;
            this.value = value;
        }

        public AutoCompleteOption(string image, string title, string text, string value)
        {
            this.image = image;
            this.title = title;
            this.text = text;
            this.value = value;
        }

        public string image { get; private set; }
        public string title { get; private set; }

        public string group { get; private set; }
        public string text { get; private set; }
        public string value { get; private set; }
    }
}