using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using QuizService.Model;
using QuizService.Model.Domain;
using System.Linq;
using Microsoft.Data.Sqlite;


namespace QuizService.Controllers;

[Route("api/quizzes")]
public class QuizController : Controller
{
    private readonly IDbConnection _connection;

    public QuizController(IDbConnection connection)
    {
        _connection = connection;
    }

    // GET api/quizzes
    [HttpGet]
    public IEnumerable<Quiz> Get()
    {
        const string sql = "SELECT * FROM Quiz;";
        var quizzes = _connection.Query<Quiz>(sql);
        return quizzes.Select(quiz =>
            new Quiz
            {
                Id = quiz.Id,
                Title = quiz.Title
            });
    }

    // GET api/quizzes/5
    [HttpGet("{id}")]
    public object Get(int id)
    {
        //TODO I prefer storing all queries in a struct and import the necessary query when needed.
        const string quizSql = "SELECT * FROM Quiz WHERE Id = @Id;";
        var quiz = _connection.QuerySingleOrDefault<Quiz>(quizSql, new { Id = id });
        if (quiz == null)
        {
            return NotFound();
        }

        //TODO A query with inner join (Quiz and Question) would be better.
        const string questionsSql = "SELECT * FROM Question WHERE QuizId = @QuizId;";
        var questions = _connection.Query<Question>(questionsSql, new { QuizId = id });

        const string answersSql = "SELECT a.Id, a.Text, a.QuestionId FROM Answer a INNER JOIN Question q ON a.QuestionId = q.Id WHERE q.QuizId = @QuizId;";
        var answers = _connection.Query<Answer>(answersSql, new { QuizId = id })
            .Aggregate(new Dictionary<int, IList<Answer>>(), (dict, answer) =>
            {
                if (!dict.ContainsKey(answer.QuestionId))
                    dict.Add(answer.QuestionId, new List<Answer>());
                dict[answer.QuestionId].Add(answer);
                return dict;
            });

        //TODO This method is large. I would rather create two methods and use them here.
        return new QuizResponseModel
        {
            Id = quiz.Id,
            Title = quiz.Title,
            Questions = questions.Select(question => new QuizResponseModel.QuestionItem
            {
                Id = question.Id,
                Text = question.Text,
                Answers = answers.ContainsKey(question.Id)
                    ? answers[question.Id].Select(answer => new QuizResponseModel.AnswerItem
                    {
                        Id = answer.Id,
                        Text = answer.Text
                    })
                    : Array.Empty<QuizResponseModel.AnswerItem>(),
                CorrectAnswerId = question.CorrectAnswerId
            }),

            //TODO Avoid hardcoded strings.
            Links = new Dictionary<string, string>
            {
                {"self", $"/api/quizzes/{id}"},
                {"questions", $"/api/quizzes/{id}/questions"}
            }
        };
    }

    // GET api/quizzes/1/result/1
    [HttpGet("{id}/result/{uid}")]
    public object Get(int id, int uid)
    {
        const string userQuizSql = "SELECT * FROM QuizResponse WHERE QuizId = @QuizId AND UserId = @UserId;";
        var responses = _connection.Query<QuizResponse>(userQuizSql, new { QuizId = id, UserId = uid }).ToList();

        const string correctAnswersSql = "SELECT a.QuestionId AS Id, a.Id AS CorrectAnswerId FROM QUESTION q INNER JOIN Answer a ON a.Id = q.CorrectAnswerId WHERE q.QuizId = @QuizId;";
        var questions = _connection.Query<Question>(correctAnswersSql, new { QuizId = id }).ToList();

        var score = responses.Sum(response => questions.Where(question => question.Id == response.QuestionId).Count(question => question.CorrectAnswerId == response.AnswerId));

        return new QuizResultModel(uid, id, score, responses.ToList());
    }

    // POST api/quizzes
    [HttpPost]
    public IActionResult Post([FromBody] QuizCreateModel value)
    {
        //TODO Make sure that the Title is valid.
        var sql = $"INSERT INTO Quiz (Title) VALUES('{value.Title}'); SELECT LAST_INSERT_ROWID();";
        var id = _connection.ExecuteScalar(sql);
        return Created($"/api/quizzes/{id}", null);
    }

    // PUT api/quizzes/5
    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] QuizUpdateModel value)
    {
        const string sql = "UPDATE Quiz SET Title = @Title WHERE Id = @Id";

        //TODO Redundant explicit property names. Dapper could figure them out.
        int rowsUpdated = _connection.Execute(sql, new { Id = id, Title = value.Title });
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        const string sql = "DELETE FROM Quiz WHERE Id = @Id";
        int rowsDeleted = _connection.Execute(sql, new { Id = id });
        if (rowsDeleted == 0)
            return NotFound();
        return NoContent();
    }

    // POST api/quizzes/5/questions
    [HttpPost]
    [Route("{id}/questions")]
    public IActionResult PostQuestion(int id, [FromBody] QuestionCreateModel value)
    {
        const string sql = "INSERT INTO Question (Text, QuizId) VALUES(@Text, @QuizId); SELECT LAST_INSERT_ROWID();";

        var questionId = 0;
        try
        {
            questionId = (int)_connection.ExecuteScalar(sql, new { Text = value.Text, QuizId = id });
        }
        catch (SqliteException e)
        {
            if (e.SqliteErrorCode == 19)
            {
                return NotFound();
            }
        }
        return Created($"/api/quizzes/{id}/questions/{questionId}", null);
    }

    // PUT api/quizzes/5/questions/6
    [HttpPut("{id}/questions/{qid}")]
    public IActionResult PutQuestion(int id, int qid, [FromBody] QuestionUpdateModel value)
    {
        const string sql = "UPDATE Question SET Text = @Text, CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
        int rowsUpdated = _connection.Execute(sql, new { QuestionId = qid, Text = value.Text, CorrectAnswerId = value.CorrectAnswerId });
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6
    [HttpDelete]
    [Route("{id}/questions/{qid}")]
    public IActionResult DeleteQuestion(int id, int qid)
    {
        const string sql = "DELETE FROM Question WHERE Id = @QuestionId";

        //TODO Use Execute instead of ExecuteScalar and check the number of deleted rows.
        _connection.ExecuteScalar(sql, new { QuestionId = qid });
        return NoContent();
    }

    // POST api/quizzes/5/questions/6/answers
    [HttpPost]
    [Route("{id}/questions/{qid}/answers")]
    public IActionResult PostAnswer(int id, int qid, [FromBody] AnswerCreateModel value)
    {
        const string sql = "INSERT INTO Answer (Text, QuestionId) VALUES(@Text, @QuestionId); SELECT LAST_INSERT_ROWID();";
        var answerId = _connection.ExecuteScalar(sql, new { Text = value.Text, QuestionId = qid });

        return Created($"/api/quizzes/{id}/questions/{qid}/answers/{answerId}", null);
    }

    // PUT api/quizzes/5/questions/6/answers/7
    [HttpPut("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult PutAnswer(int id, int qid, int aid, [FromBody] AnswerUpdateModel value)
    {
        const string sql = "UPDATE Answer SET Text = @Text WHERE Id = @AnswerId";

        //TODO Validate the value.Text. The same goes for other functions as well.
        int rowsUpdated = _connection.Execute(sql, new { AnswerId = qid, Text = value.Text });
        if (rowsUpdated == 0)
            return NotFound();

        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6/answers/7
    [HttpDelete]
    [Route("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult DeleteAnswer(int id, int qid, int aid)
    {
        const string sql = "DELETE FROM Answer WHERE Id = @AnswerId";

        _connection.ExecuteScalar(sql, new { AnswerId = aid });
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6/answers/7user/1
    [HttpPost]
    [Route("{id}/answers")]
    public IActionResult PostAsync(int id, [FromBody] QuizResultModel quizResultModel)
    {
        const string insertAnswer = "INSERT INTO QuizResponse (QuizId, QuestionId, AnswerId, UserId)  VALUES(@QuizId, @QuestionId, @AnswerId, @UserId); SELECT LAST_INSERT_ROWID();";

        try
        {
            foreach (var answerId in quizResultModel.QuizResponses.Select(quizAnswersModel => (long)_connection.ExecuteScalar(insertAnswer,
                         new
                         {
                             quizResultModel.QuizId,
                             quizAnswersModel.QuestionId,
                             quizAnswersModel.AnswerId,
                             quizResultModel.UserId
                         })))
            {
            }
        }
        catch (SqliteException e)
        {
            if (e.SqliteErrorCode == 19)
            {
                return NotFound();
            }
        }

        return Created($"/api/quizzes/{id}/answers", null);
    }
}