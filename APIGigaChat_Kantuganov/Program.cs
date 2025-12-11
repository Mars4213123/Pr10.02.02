using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using APIGigaChat_Kantuganov.Models.Response;
using Newtonsoft.Json;
using APIGigaChat_Kantuganov.Models;

namespace APIGigaChat_Kantuganov
{
    public class Program
    {
        public static string ClientId = "7879c628-132f-4ec9-b371-309d6472aa56";
        public static string AuthorizationKey = "Nzg3OWM2MjgtMTMyZi00ZWM5LWIzNzEtMzA5ZDY0NzJhYTU2Ojg3ZWQ0YjYyLWVlOGUtNGFjMC1hNjE3LTMwY2U3YmRiNmQ4Mg==";

        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AuthorizationKey);

            if (Token == null)
            {
                return;
            }

            List<Request.Message> messageHistory = new List<Request.Message>();

            while (true)
            {
                Console.Write("Сообщение: ");
                string Message = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(Message) ||
                    Message.ToLower() == "exit" ||
                    Message.ToLower() == "выход")
                {
                    break;
                }

                messageHistory.Add(new Request.Message
                {
                    role = "user",
                    content = Message
                });

                ResponseMessage answer = await GetAnswer(Token, messageHistory);

                if (answer != null && answer.choices != null && answer.choices.Count > 0)
                {
                    string assistantResponse = answer.choices[0].message.content;
                    Console.WriteLine("Ответ: " + assistantResponse);

                    messageHistory.Add(new Request.Message
                    {
                        role = "assistant",
                        content = assistantResponse
                    });
                }
            }
        }

        public static async Task<string> GetToken(string rqUID, string bearer)
        {
            string ReturnToken = null;
            string Uri = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Uri);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("RqUID", rqUID);
                    Request.Headers.Add("Authorization", $"Bearer {bearer}");

                    var Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                    };

                    Request.Content = new FormUrlEncodedContent(Data);

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);
                        ReturnToken = Token.access_token;
                    }
                }
            }

            return ReturnToken;
        }

        public static async Task<ResponseMessage> GetAnswer(string token, List<Request.Message> messageHistory)
        {
            ResponseMessage responseMessage = null;
            string Uri = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Uri);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Bearer {token}");

                    var DataRequest = new Request()
                    {
                        model = "GigaChat",
                        stream = false,
                        repetition_penalty = 1,
                        messages = messageHistory
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<ResponseMessage>(ResponseContent);
                    }
                }
            }

            return responseMessage;
        }
    }
}