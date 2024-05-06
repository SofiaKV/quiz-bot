using Bot_Quiz.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TriviaApi.Controllers
{
    [Route("api/trivia")]
    [ApiController]
    public class TriviaController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public TriviaController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public async Task<TriviaResponse?> GetTriviaQuestions(
            [FromQuery] int amount = 5,
            [FromQuery] string difficulty = "easy"
            )
        {
            var apiUrl = $"https://opentdb.com/api.php?amount={amount}&difficulty={difficulty}&type=multiple"; 
            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<TriviaResponse>(content);
            return json;
        }
    }
}
