using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ProblemsResponse
{
    public class AIService
    {
        private readonly HttpClient _client;
        private readonly string _aiUrl;
        private readonly string _aiKey;
        private readonly string _aiModel;
        private readonly List<JObject> _conversationHistory;

        public AIService(string aiUrl, string aiKey, string aiModel)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiKey);
            _aiUrl = aiUrl;
            _aiKey = aiKey;
            _aiModel = aiModel;
            _conversationHistory = new List<JObject>
            {
                new JObject(
                    new JProperty("role", "system"),
                    new JProperty("content", DefaultConst.DefaultSystemPrompt)
                )
            };
        }

        public async Task<string> GetAIResponseAsync(string userMessage)
        {
            // 添加用户消息到对话历史
            _conversationHistory.Add(new JObject(
                new JProperty("role", "user"),
                new JProperty("content", userMessage)
            ));

            var requestBody = new JObject(
                new JProperty("model", _aiModel),
                new JProperty("enable_thinking", false),
                new JProperty("messages", new JArray(_conversationHistory.ToArray()))
            );

            string jsonContent = requestBody.ToString();

            try
            {
                var aiContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var aiResponse = await _client.PostAsync(_aiUrl, aiContent);

                if (!aiResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"AI请求失败: {aiResponse.StatusCode}");
                }

                var aiResponseContent = await aiResponse.Content.ReadAsStringAsync();
                var aiResult = JsonSerializer.Deserialize<AiResponse>(aiResponseContent);

                var aiResponseMessage = aiResult?.Choices?.FirstOrDefault()?.Message?.Content;// ?? "已收到";

                // 添加用户消息到对话历史
                _conversationHistory.Add(new JObject(
                    new JProperty("role", "assistant"),
                    new JProperty("content", aiResponseMessage)
                ));

                return aiResponseMessage;
            }
            catch (Exception ex)
            {
                throw new Exception($"AI请求异常: {ex.Message}");
            }
        }
    }

    public class AiChoice
    {
        [JsonPropertyName("message")]
        public AiMessage? Message { get; set; }
    }

    public class AiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class AiResponse
    {
        [JsonPropertyName("choices")]
        public List<AiChoice>? Choices { get; set; }
    }
}
