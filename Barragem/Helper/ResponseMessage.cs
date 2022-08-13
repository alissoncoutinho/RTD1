namespace Barragem.Helper
{
    public class ResponseMessage
    {
        public int retorno { get; set; }
        public string erro { get; set; }

    }

    public class ResponseMessageWithStatus
    {
        public dynamic retorno { get; set; }
        public string erro { get; set; }
        public string status { get; set; }
    }
}