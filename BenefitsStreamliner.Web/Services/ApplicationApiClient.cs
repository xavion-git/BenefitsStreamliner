using System.Net;
using System.Net.Http.Json;
using BenefitsStreamliner.Core.DTOs;

namespace BenefitsStreamliner.Web.Services;

public class ApplicationApiClient(HttpClient http)
{
    public Task<(ApplicationResponse? Data, string? Error)> SubmitAsync(ApplicationRequest req) =>
        SendAsync<ApplicationResponse>(() => http.PostAsJsonAsync("api/applications", req));

    public Task<(CheckBenefitsResult? Data, string? Error)> CheckBenefitsAsync(string id) =>
        SendAsync<CheckBenefitsResult>(() => http.PostAsync($"api/applications/{id}/check-benefits", null));

    public Task<(CheckBenefitsResult? Data, string? Error)> GetStatusAsync(string id) =>
        SendAsync<CheckBenefitsResult>(() => http.GetAsync($"api/applications/{id}"));

    private static async Task<(T? Data, string? Error)> SendAsync<T>(Func<Task<HttpResponseMessage>> call)
    {
        try
        {
            var response = await call();
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<T>(), null);

            return (default, response.StatusCode == HttpStatusCode.BadRequest
                ? "Please check the form fields and try again."
                : "The server couldn't process the request. Please try again.");
        }
        catch (HttpRequestException)
        {
            return (default, "Cannot reach the Benefits Streamliner API. Is it running?");
        }
    }
}
