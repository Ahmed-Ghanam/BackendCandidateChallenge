namespace QuizService.Model
{
    public class QuizResponse
    {
        public QuizResponse()
        {

        }

        public QuizResponse(int questionId, int answerId)
        {
            AnswerId = answerId;
            QuestionId = questionId;
        }

        public int AnswerId { get; set; }

        public int QuestionId { get; set; }
    }
}
