using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using QuizService.Model;
using Xunit;

namespace QuizService.Tests;

public class QuizzesControllerTest
{
    const string QuizApiEndPoint = "/api/quizzes/";

    [Fact]
    public async Task PostNewQuizAddsQuiz()
    {
        var quiz = new QuizCreateModel("Test title");
        using (var testHost = new TestServer(new WebHostBuilder().UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(quiz));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }
    }

    [Fact]
    public async Task AQuizExistGetReturnsQuiz()
    {
        using (var testHost = new TestServer(new WebHostBuilder().UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 1;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var quiz = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(quiz);
            Assert.Equal(quizId, quiz.Id);
            Assert.Equal("My first quiz", quiz.Title);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExistGetFails()
    {
        using (var testHost = new TestServer(new WebHostBuilder().UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExists_WhenPostingAQuestion_ReturnsNotFound()
    {
        const string QuizApiEndPoint = "/api/quizzes/999/questions";

        using (var testHost = new TestServer(new WebHostBuilder().UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            var question = new QuestionCreateModel("The answer to everything is what?");
            var content = new StringContent(JsonConvert.SerializeObject(question));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"), content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task PostNewAnswer()
    {
        using (var testHost = new TestServer(new WebHostBuilder().UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();

            var answerModel = new QuizResultModel(1, 1, 0, new List<QuizResponse> { new(1, 1), new(2, 5) });
            var content = new StringContent(JsonConvert.SerializeObject(answerModel));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var postResponse = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{answerModel.QuizId}/answers"), content);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.NotNull(postResponse.Headers.Location);

            var getResponse = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{answerModel.QuizId}/result/{answerModel.UserId}"));
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.NotNull(getResponse.Content);

            var quiz = JsonConvert.DeserializeObject<QuizResultModel>(await getResponse.Content.ReadAsStringAsync());
            Assert.NotNull(quiz);
            Assert.Equal(2, quiz.Score);
        }
    }
}