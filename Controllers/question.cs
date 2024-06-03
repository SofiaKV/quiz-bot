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
        private static List<TriviaQuestion> _triviaQuestions = new List<TriviaQuestion>();

        public TriviaController()
        {
            _httpClient = new HttpClient();
            _triviaQuestions.Add(new TriviaQuestion
            {
                Id = 1,
                Type = "multiple",
                Difficulty = "easy",
                Category = "General Knowledge",
                QuestionText = "What is the capital of France?",
                CorrectAnswer = "Paris",
                IncorrectAnswers = new List<string> { "Berlin", "Madrid", "Rome" }
            });
        }

        [HttpGet]
        public async Task<ActionResult<TriviaResponse>> GetTriviaQuestions(
            [FromQuery] int amount = 5,
            [FromQuery] string difficulty = "easy"
        )
        {
            try
            {
                var apiUrl = $"https://opentdb.com/api.php?amount={amount}&difficulty={difficulty}&type=multiple";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to fetch trivia questions.");
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<TriviaResponse>(content);

                if (json != null && json.Results.Any())
                {
                    int currentMaxId = _triviaQuestions.Any() ? _triviaQuestions.Max(q => q.Id) : 0;
                    for (int i = 0; i < json.Results.Count; i++)
                    {
                        json.Results[i].Id = currentMaxId + i + 1;
                    }

                    _triviaQuestions.AddRange(json.Results);
                }

                return Ok(json);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<TriviaQuestion> GetTriviaQuestion(int id)
        {
            try
            {
                var question = _triviaQuestions.FirstOrDefault(q => q.Id == id);
                if (question == null)
                {
                    return NotFound("Question not found.");
                }
                return Ok(question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("add")]
        public ActionResult<TriviaQuestion> AddTriviaQuestion([FromBody] TriviaQuestionRequest newQuestion)
        {
            try
            {
                if (newQuestion == null || string.IsNullOrWhiteSpace(newQuestion.QuestionText) || string.IsNullOrWhiteSpace(newQuestion.CorrectAnswer) || newQuestion.IncorrectAnswers == null || !newQuestion.IncorrectAnswers.Any())
                {
                    return BadRequest("Invalid question data.");
                }

                int newId = _triviaQuestions.Any() ? _triviaQuestions.Max(q => q.Id) + 1 : 1;
                var triviaQuestion = new TriviaQuestion
                {
                    Id = newId,
                    QuestionText = newQuestion.QuestionText,
                    CorrectAnswer = newQuestion.CorrectAnswer,
                    IncorrectAnswers = newQuestion.IncorrectAnswers,
                    Category = newQuestion.Category,
                    Difficulty = newQuestion.Difficulty,
                    Type = newQuestion.Type
                };
                _triviaQuestions.Add(triviaQuestion);

                return CreatedAtAction(nameof(GetTriviaQuestion), new { id = newId }, triviaQuestion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public ActionResult<TriviaQuestion> UpdateTriviaQuestion(int id, [FromBody] TriviaQuestion updatedQuestion)
        {
            try
            {
                var question = _triviaQuestions.FirstOrDefault(q => q.Id == id);
                if (question == null)
                {
                    return NotFound("Question not found.");
                }

                question.QuestionText = updatedQuestion.QuestionText;
                question.IncorrectAnswers = updatedQuestion.IncorrectAnswers;
                question.CorrectAnswer = updatedQuestion.CorrectAnswer;
                question.Category = updatedQuestion.Category;
                question.Difficulty = updatedQuestion.Difficulty;
                question.Type = updatedQuestion.Type;

                return Ok(question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTriviaQuestion(int id)
        {
            try
            {
                var question = _triviaQuestions.FirstOrDefault(q => q.Id == id);
                if (question == null)
                {
                    return NotFound("Question not found.");
                }

                _triviaQuestions.Remove(question);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

