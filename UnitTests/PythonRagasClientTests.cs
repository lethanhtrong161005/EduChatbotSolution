using Business.Services.AI.Experiments;
using Domain.Contracts;
using System.Net;
using System.Text;

namespace UnitTests;

[TestFixture]
public sealed class PythonRagasClientTests
{
    [Test]
    public async Task EvaluateAsync_SendsRequestIdAsCorrelationIdAndDeserializesResponse()
    {
        var request = Request();
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(message =>
        {
            captured = message;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                    {"request_id":"{{request.RequestId}}","contract_version":"v1","service_version":"1.0.0","ragas_version":"0.4.3","prompt_version":"vi-v1","results":[{"metric_name":"answer_relevancy","status":"completed","score":0.75,"duration_ms":12}]}
                    """, Encoding.UTF8, "application/json"),
            };
        });

        var result = await new PythonRagasClient(new HttpClient(handler) { BaseAddress = new Uri("http://ragas/") }).EvaluateAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Headers.GetValues("X-Correlation-ID").Single(), Is.EqualTo(request.RequestId.ToString()));
            Assert.That(result.RequestId, Is.EqualTo(request.RequestId));
            Assert.That(result.Results.Single().MetricName, Is.EqualTo(RagasMetricName.AnswerRelevancy));
            Assert.That(result.Results.Single().Score, Is.EqualTo(.75));
        }
    }

    private static PythonRagasEvaluationRequest Request() => new()
    {
        RequestId = Guid.NewGuid(), ContractVersion = "v1", Language = "vi", Question = "Question", Reference = "Reference", Response = "Response", PromptContexts = ["Prompt context"], RetrievalContexts = ["Prompt context", "Other context"], Metrics = [RagasMetricName.AnswerRelevancy], LlmProvider = "gemini", LlmModel = "gemini-2.5-flash", EmbeddingProvider = "gemini", EmbeddingModel = "gemini-embedding-001", PromptVersion = "vi-v1",
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }
}
