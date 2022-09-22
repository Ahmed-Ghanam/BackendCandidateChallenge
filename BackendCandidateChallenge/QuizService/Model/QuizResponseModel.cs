using System.Collections.Generic;

namespace QuizService.Model;

public class QuizResponseModel
{
    // TODO I like to have every class in its own file.
    public class AnswerItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    public class QuestionItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public IEnumerable<AnswerItem> Answers { get; set; }
        public int CorrectAnswerId { get; set; }
    }

    public long Id { get; set; }
    public string Title { get; set; }
    public IEnumerable<QuestionItem> Questions { get; set; }
    public IDictionary<string, string> Links { get; set; }
}