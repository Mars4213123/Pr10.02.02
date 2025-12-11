using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChat_Kantuganov.Models
{
    public class YandexGPTRequest
    {
        public string modelUri { get; set; }
        public CompletionOptions completionOptions { get; set; }
        public List<Message> messages { get; set; }

        public class CompletionOptions
        {
            public bool stream { get; set; }
            public double temperature { get; set; }
            public int maxTokens { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string text { get; set; }
        }
    }
}
