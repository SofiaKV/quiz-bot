using Bot_Quiz.Models;
using TriviaApi;

namespace Bot_Quiz
{
    public class Program
    {
        private static readonly string connectionString = "Host=localhost;Database=quiz;Username=quizuser;Password=quizpassword;";
        private static readonly Database database = new Database(connectionString);

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Start Telegram Bot");
            Console.WriteLine("2. Start Swagger");
            Console.WriteLine("3. Check Database Connection");
            Console.WriteLine("4. View All Quiz Results");
            Console.WriteLine("5. Create QuizResults Table");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    var bot = new Telegram_Bot(database);
                    await bot.Start();
                    Console.ReadKey();
                    break;

                case "2":
                    CreateHostBuilder(args).Build().Run();
                    break;

                case "3":
                    bool isConnected = await database.CheckConnectionAsync();
                    Console.WriteLine(isConnected ? "Database connection successful." : "Database connection failed.");
                    break;

                case "4":
                    await ViewAllQuizResults();
                    break;

                case "5":
                    await database.CreateTableAsync();
                    Console.WriteLine("QuizResults table created successfully.");
                    break;

                default:
                    Console.WriteLine("Invalid option selected.");
                    break;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static async Task ViewAllQuizResults()
        {
            List<QuizResult> results = await database.GetAllQuizResultsAsync();
            Console.WriteLine("All Quiz Results:");
            foreach (var result in results)
            {
                Console.WriteLine($"UserId: {result.UserId}, QuizId: {result.QuizId}, CorrectAnswers: {result.CorrectAnswers}, TotalQuestions: {result.TotalQuestions}");
            }
        }
    }
}
