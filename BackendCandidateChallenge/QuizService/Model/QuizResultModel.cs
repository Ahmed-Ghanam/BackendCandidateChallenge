using System.Collections.Generic;

namespace QuizService.Model
{
    public class QuizResultModel
    {
        public QuizResultModel()
        {

        }

        public QuizResultModel(int userId, int quizId, int score, List<QuizResponse> questionAnswerModels)
        {
            Score = score;
            UserId = userId;
            QuizId = quizId;
            QuizResponses = questionAnswerModels;
        }

        public int Score { get; set; }

        public int UserId { get; set; }

        public int QuizId { get; set; }

        public List<QuizResponse> QuizResponses { get; set; }
    }
}
