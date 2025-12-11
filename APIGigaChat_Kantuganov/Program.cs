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
        public static string GigaChatClientId = "";
        public static string GigaChatAuthorizationKey = "";
        public static string YandexOAuthToken = "";
        public static string YandexFolderId = "";

        static async Task Main(string[] args)
        {
            string GigaChatToken = await GetGigaChatToken(GigaChatClientId, GigaChatAuthorizationKey);

            if (GigaChatToken == null)
            {
                return;
            }

            Console.WriteLine("Выберите API:");
            Console.WriteLine("1. GigaChat");
            Console.WriteLine("2. YandexGPT");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                await RunChat(GigaChatToken, "gigachat");
            }
            else
            {
                await RunChat("", "yandexgpt");
            }
        }

        static async Task RunChat(string token, string apiType)
        {
            var gigachatHistory = new List<Request.Message>();
            var yandexHistory = new List<YandexGPTRequest.Message>();

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

                if (apiType == "gigachat")
                {
                    gigachatHistory.Add(new Request.Message
                    {
                        role = "user",
                        content = Message
                    });

                    ResponseMessage answer = await GetGigaChatAnswer(token, gigachatHistory);

                    if (answer != null && answer.choices != null && answer.choices.Count > 0)
                    {
                        string assistantResponse = answer.choices[0].message.content;
                        Console.WriteLine("Ответ: " + assistantResponse);

                        gigachatHistory.Add(new Request.Message
                        {
                            role = "assistant",
                            content = assistantResponse
                        });
                    }
                }
                else
                {
                    yandexHistory.Add(new YandexGPTRequest.Message
                    {
                        role = "user",
                        text = Message
                    });

                    YandexGPTResponse answer = await GetYandexGPTAnswer(YandexOAuthToken, YandexFolderId, yandexHistory);

                    if (answer != null && answer.result != null && answer.result.alternatives != null && answer.result.alternatives.Count > 0)
                    {
                        string assistantResponse = answer.result.alternatives[0].message.text;
                        Console.WriteLine("Ответ: " + assistantResponse);

                        yandexHistory.Add(new YandexGPTRequest.Message
                        {
                            role = "assistant",
                            text = assistantResponse
                        });
                    }
                }
            }
        }

        public static async Task<string> GetGigaChatToken(string rqUID, string bearer)
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

        public static async Task<ResponseMessage> GetGigaChatAnswer(string token, List<Request.Message> messageHistory)
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

        public static async Task<YandexGPTResponse> GetYandexGPTAnswer(string iamToken, string folderId, List<YandexGPTRequest.Message> messageHistory)
        {
            YandexGPTResponse responseMessage = null;
            string Uri = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Uri);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"Api-Key {iamToken}");

                    var DataRequest = new YandexGPTRequest()
                    {
                        modelUri = $"gpt://{folderId}/yandexgpt-lite",
                        completionOptions = new YandexGPTRequest.CompletionOptions
                        {
                            stream = false,
                            temperature = 0.6,
                            maxTokens = 2000
                        },
                        messages = messageHistory
                    };

                    string JsonContent = JsonConvert.SerializeObject(DataRequest);
                    Request.Content = new StringContent(JsonContent, Encoding.UTF8, "application/json");

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        responseMessage = JsonConvert.DeserializeObject<YandexGPTResponse>(ResponseContent);
                    }
                }
            }

            return responseMessage;
        }
    }
}