using System.Net;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot_Quiz
{
    public class Telegram_Bot
    {
        private static readonly string BotToken = "";
        private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);
        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static readonly ReceiverOptions ReceiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        private Dictionary<long, QuizState> userQuizStates = new Dictionary<long, QuizState>();
        private readonly Database database;

        public Telegram_Bot(Database database)
        {
            this.database = database;
        }

        public async Task Start()
        {
            try
            {
                var botMe = await BotClient.GetMeAsync();
                Console.WriteLine($"Bot {botMe.Username} is running");
                BotClient.StartReceiving(HandlerUpdateAsync, HandlerErrorAsync, ReceiverOptions, Cts.Token);
                Console.WriteLine("StartReceiving called");
            }
            catch (ApiRequestException ex)
            {
                Console.WriteLine($"Error in Telegram API: {ex.ErrorCode}\n{ex.Message}");
            }
        }

        private Task HandlerErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Error in Telegram API: {apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update == null) return;

            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            try
            {
                switch (message.Text.ToLower())
                {
                    case "/start":
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Welcome to Quiz Bot! Use /quiz to start a quiz.");
                        break;

                    case "/quiz":
                        await StartQuiz(message);
                        break;

                    default:
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Command not recognized. Please use /start or /quiz.");
                        break;
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "An error occurred: " + ex.Message);
            }
        }

        private async Task StartQuiz(Message message)
        {
            var questions = await FetchQuizQuestions(5);

            var quizState = new QuizState
            {
                Questions = questions,
                CurrentQuestionIndex = 0,
                UserAnswers = new List<string>()
            };

            userQuizStates[message.Chat.Id] = quizState;
            await SendNextQuestion(message.Chat.Id);
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message.Chat.Id;

            if (userQuizStates.TryGetValue(chatId, out var quizState))
            {
                quizState.UserAnswers.Add(callbackQuery.Data);

                await botClient.EditMessageReplyMarkupAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, null);

                if (quizState.CurrentQuestionIndex < quizState.Questions.Count - 1)
                {
                    quizState.CurrentQuestionIndex++;
                    await SendNextQuestion(chatId);
                }
                else
                {
                    await ShowQuizResults(chatId, quizState);
                    userQuizStates.Remove(chatId);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Please start a new quiz with /quiz.");
            }
        }

        private async Task SendNextQuestion(long chatId)
        {
            var quizState = userQuizStates[chatId];
            var question = quizState.Questions[quizState.CurrentQuestionIndex];
            var inlineKeyboardButtons = question.Answers.Select(answer => InlineKeyboardButton.WithCallbackData(WebUtility.HtmlDecode(answer), WebUtility.HtmlDecode(answer))).ToArray();
            var inlineKeyboard = new InlineKeyboardMarkup(inlineKeyboardButtons);

            await BotClient.SendTextMessageAsync(
                chatId: chatId,
                text: WebUtility.HtmlDecode(question.Question),
                replyMarkup: inlineKeyboard
            );
        }

        private async Task ShowQuizResults(long chatId, QuizState quizState)
        {
            var correctAnswers = quizState.Questions.Select(q => q.CorrectAnswer).ToList();
            var userAnswers = quizState.UserAnswers;
            int correctAnswerCount = 0;

            var results = "Quiz Finished! Here are your results:\n\n";

            for (int i = 0; i < correctAnswers.Count; i++)
            {
                results += $"Q{i + 1}: {WebUtility.HtmlDecode(quizState.Questions[i].Question)}\n";
                results += $"Your answer: {WebUtility.HtmlDecode(userAnswers[i])}\n";
                results += $"Correct answer: {WebUtility.HtmlDecode(correctAnswers[i])}\n\n";

                if (userAnswers[i] == correctAnswers[i])
                {
                    correctAnswerCount++;
                }
            }

            await BotClient.SendTextMessageAsync(chatId, results);
            await BotClient.SendTextMessageAsync(chatId, $"You got {correctAnswerCount} out of {correctAnswers.Count} questions correct!");

            var previousCorrectAnswers = await database.GetPreviousCorrectAnswersAsync(chatId);

            string comparisonMessage = previousCorrectAnswers.HasValue
                ? $"Previous best correct answers: {previousCorrectAnswers.Value}\n" +
                  $"Current correct answers: {correctAnswerCount}\n" +
                  $"Difference in correct answers: {correctAnswerCount - previousCorrectAnswers.Value}"
                : "This is the first record for this user and quiz.";

            await BotClient.SendTextMessageAsync(chatId, comparisonMessage);

            if (previousCorrectAnswers.HasValue || previousCorrectAnswers.GetValueOrDefault() < correctAnswerCount)
            {
                await BotClient.SendTextMessageAsync(chatId, "Congratulations! This is you new record");
                await database.SaveQuizResultAsync(chatId, correctAnswerCount, correctAnswers.Count);
            }
        }

        private async Task<List<QuizQuestion>> FetchQuizQuestions(int amount)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync($"https://opentdb.com/api.php?amount={amount}&type=multiple");
                var quizData = JsonConvert.DeserializeObject<QuizData>(response);

                if (quizData != null && quizData.Results != null && quizData.Results.Count > 0)
                {
                    return quizData.Results.Select(questionData =>
                    {
                        var allAnswers = new List<string>(questionData.IncorrectAnswers) { questionData.CorrectAnswer };
                        allAnswers = allAnswers.OrderBy(a => Guid.NewGuid()).ToList();

                        return new QuizQuestion
                        {
                            Question = WebUtility.HtmlDecode(questionData.Question),
                            Answers = allAnswers.Select(WebUtility.HtmlDecode).ToArray(),
                            CorrectAnswer = WebUtility.HtmlDecode(questionData.CorrectAnswer)
                        };
                    }).ToList();
                }
                else
                {
                    throw new Exception("No quiz questions available.");
                }
            }
        }
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
}
