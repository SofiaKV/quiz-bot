using Newtonsoft.Json;


namespace Bot_Quiz.Models
{
    public class TriviaResponse
    {
        [JsonProperty("response_code")]
        public int ResponseCode { get; set; }

        [JsonProperty("results")]
        public List<TriviaQuestion> Results { get; set; }
    }

    public class TriviaQuestion
    {
        public int Id { get; set; }  

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("question")]
        public string QuestionText { get; set; }

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }

    public class TriviaQuestionRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("question")]
        public string QuestionText { get; set; }

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }

    public class QuizState
    {
        public List<QuizQuestion> Questions { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public List<string> UserAnswers { get; set; }
    }

    public class QuizData
    {
        [JsonProperty("results")]
        public List<QuizQuestionData> Results { get; set; }
    }

    public class QuizQuestionData
    {
        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }

    public class QuizQuestion
    {
        public string Question { get; set; }
        public string[] Answers { get; set; }
        public string CorrectAnswer { get; set; }
    }

    public class QuizResult
    {
        public long UserId { get; set; }
        public string QuizId { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
    }
}
