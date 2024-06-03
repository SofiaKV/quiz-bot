using Bot_Quiz.Models;
using Npgsql;

namespace Bot_Quiz
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    await connection.OpenAsync();
                    return connection.State == System.Data.ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to database: {ex.Message}");
                return false;
            }
        }

        public async Task<int?> GetPreviousCorrectAnswersAsync(long userId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    "SELECT CorrectAnswers FROM QuizResults WHERE UserId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                var result = await command.ExecuteScalarAsync();
                return result != null ? (int?)Convert.ToInt32(result) : null;
            }
        }

        public async Task SaveQuizResultAsync(long userId, int correctAnswers, int totalQuestions)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                var command = new NpgsqlCommand(
                    "INSERT INTO QuizResults (UserId, CorrectAnswers, TotalQuestions) " +
                    "VALUES (@UserId, @CorrectAnswers, @TotalQuestions) " +
                    "ON CONFLICT (UserId) " +
                    "DO UPDATE SET CorrectAnswers = EXCLUDED.CorrectAnswers, TotalQuestions = EXCLUDED.TotalQuestions", connection);

                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CorrectAnswers", correctAnswers);
                command.Parameters.AddWithValue("@TotalQuestions", totalQuestions);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<QuizResult>> GetAllQuizResultsAsync()
        {
            var results = new List<QuizResult>();
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand("SELECT UserId, CorrectAnswers, TotalQuestions FROM QuizResults", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new QuizResult
                        {
                            UserId = reader.GetInt64(0),
                            CorrectAnswers = reader.GetInt32(2),
                            TotalQuestions = reader.GetInt32(3)
                        });
                    }
                }
            }
            return results;
        }

        public async Task CreateTableAsync()
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(
                    "CREATE TABLE IF NOT EXISTS QuizResults (" +
                    "UserId BIGINT NOT NULL, " +
                    "CorrectAnswers INT, " +
                    "TotalQuestions INT, " +
                    "PRIMARY KEY (UserId))", connection);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
