using System.Net;
using System.Net.Http.Json;

var baseUrl = "https://localhost:7074";
var showId = "def95593-5852-4fc3-908e-2962b8c21445";

using var client = new HttpClient();

var tasks = Enumerable.Range(1, 20).Select(async i =>
{
    var userId = $"stress-user-{i}";

    var request = new
    {
        seats = new[] { "A1" },
        idempotencyKey = $"stress-{i}"
    };

    using var httpRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"{baseUrl}/shows/{showId}/reserve");

    httpRequest.Headers.Add("X-User-Id", userId);
    httpRequest.Content = JsonContent.Create(request);

    var response = await client.SendAsync(httpRequest);

    var body = await response.Content.ReadAsStringAsync();

    return new
    {
        User = userId,
        StatusCode = (int)response.StatusCode,
        Body = body
    };
});

var results = await Task.WhenAll(tasks);

Console.WriteLine("===== CONCURRENCY TEST RESULT =====");

foreach (var result in results)
{
    Console.WriteLine(
        $"{result.User} -> {result.StatusCode}");
}

Console.WriteLine();
Console.WriteLine(
    $"201 Created: {results.Count(x => x.StatusCode == 201)}");

Console.WriteLine(
    $"409 Conflict: {results.Count(x => x.StatusCode == 409)}");

Console.WriteLine(
    $"500 Errors: {results.Count(x => x.StatusCode >= 500)}");