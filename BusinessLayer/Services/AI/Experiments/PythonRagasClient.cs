using Domain.Contracts;
using System.Net.Http.Json;

namespace Business.Services.AI.Experiments;

public sealed class PythonRagasClient(HttpClient httpClient) : IPythonRagasClient
{
    private readonly HttpClient _httpClient = httpClient;

    public Task<PythonRagasHealth> GetHealthAsync(CancellationToken cxlTkn = default) => GetAsync<PythonRagasHealth>("health", cxlTkn);
    public Task<PythonRagasCapabilities> GetCapabilitiesAsync(CancellationToken cxlTkn = default) => GetAsync<PythonRagasCapabilities>("v1/capabilities", cxlTkn);

    public async Task<PythonRagasEvaluationResponse> EvaluateAsync(PythonRagasEvaluationRequest request, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var message = new HttpRequestMessage(HttpMethod.Post, "v1/evaluations") { Content = JsonContent.Create(request) };
        message.Headers.Add("X-Correlation-ID", request.RequestId.ToString());
        using var response = await _httpClient.SendAsync(message, cxlTkn);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PythonRagasEvaluationResponse>(cancellationToken: cxlTkn) ?? throw new InvalidOperationException("The Python Ragas service returned an empty evaluation response.");
        if (result.RequestId != request.RequestId) throw new InvalidOperationException($"The Python Ragas response request ID '{result.RequestId}' does not match '{request.RequestId}'.");
        return result;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cxlTkn)
    {
        using var response = await _httpClient.GetAsync(path, cxlTkn);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cxlTkn) ?? throw new InvalidOperationException($"The Python Ragas service returned an empty response for '{path}'.");
    }
}
