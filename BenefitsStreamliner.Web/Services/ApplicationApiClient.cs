using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BenefitsStreamliner.Core.DTOs;
using BenefitsStreamliner.Core.Rules;
using Microsoft.AspNetCore.Components.Forms;

namespace BenefitsStreamliner.Web.Services;

public class ApplicationApiClient(HttpClient http)
{
    // The claim is sent as multipart/form-data so the uploaded files travel with it.
    public async Task<(ApplicationResponse? Data, string? Error)> SubmitAsync(
        ApplicationRequest req, IReadOnlyList<IBrowserFile> documents, IBrowserFile? proofOfPayment)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            void Add(string name, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value)) content.Add(new StringContent(value.Trim()), name);
            }
            void AddDate(string name, DateTime? value) => Add(name, value?.ToString("yyyy-MM-dd"));

            Add(nameof(req.EmployeeName), req.EmployeeName);
            Add(nameof(req.Sin), SinValidator.Digits(req.Sin));
            Add(nameof(req.Phone), req.Phone);
            Add(nameof(req.Email), req.Email);
            Add(nameof(req.MailingAddress), req.MailingAddress);
            Add(nameof(req.City), req.City);
            Add(nameof(req.Province), req.Province);
            Add(nameof(req.PostalCode), req.PostalCode);
            Add(nameof(req.CompanyName), req.CompanyName);
            Add(nameof(req.CompanyAddress), req.CompanyAddress);
            Add(nameof(req.CompanyCity), req.CompanyCity);
            Add(nameof(req.CompanyProvince), req.CompanyProvince);
            Add(nameof(req.CompanyPostalCode), req.CompanyPostalCode);
            Add(nameof(req.Trade), req.Trade);
            Add(nameof(req.TrainingYear), req.TrainingYear?.ToString());
            AddDate(nameof(req.TrainingFrom), req.TrainingFrom);
            AddDate(nameof(req.TrainingTo), req.TrainingTo);
            AddDate(nameof(req.ReturnedToWork), req.ReturnedToWork);
            Add(nameof(req.PayTo), req.PayTo);
            Add(nameof(req.CertificateNumber), req.CertificateNumber);
            Add(nameof(req.TuitionCode), req.TuitionCode);
            Add(nameof(req.EmployeeSignature), req.EmployeeSignature);
            AddDate(nameof(req.EmployeeSignDate), req.EmployeeSignDate);
            Add(nameof(req.RepName), req.RepName);
            Add(nameof(req.RepPosition), req.RepPosition);
            Add(nameof(req.RepSignature), req.RepSignature);
            AddDate(nameof(req.RepSignDate), req.RepSignDate);

            foreach (var file in documents)
                content.Add(new StreamContent(file.OpenReadStream(ClaimValidator.MaxFileBytes)), "documents", file.Name);
            if (proofOfPayment is not null)
                content.Add(new StreamContent(proofOfPayment.OpenReadStream(ClaimValidator.MaxFileBytes)), "proofOfPayment", proofOfPayment.Name);

            var response = await http.PostAsync("api/applications", content);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<ApplicationResponse>(), null);

            return (null, await DescribeFailureAsync(response));
        }
        catch (HttpRequestException)
        {
            return (null, "Cannot reach the Benefits Streamliner API. Is it running?");
        }
        catch (IOException)
        {
            return (null, "One of the files could not be read. Please choose it again.");
        }
    }

    public Task<(CheckBenefitsResult? Data, string? Error)> CheckBenefitsAsync(string id) =>
        SendAsync<CheckBenefitsResult>(() => http.PostAsync($"api/applications/{id}/check-benefits", null));

    public Task<(CheckBenefitsResult? Data, string? Error)> GetStatusAsync(string id) =>
        SendAsync<CheckBenefitsResult>(() => http.GetAsync($"api/applications/{id}"));

    public Task<(CheckBenefitsResult? Data, string? Error)> DecideAsync(string id, bool approve) =>
        SendAsync<CheckBenefitsResult>(() => http.PostAsJsonAsync($"api/applications/{id}/decision",
            new DecisionRequest { Decision = approve ? "Approve" : "Reject" }));

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

    // 400 (validation) and 409 (duplicate claim) carry messages we wrote, so they are safe to show.
    // Anything else gets a generic message: no stack traces or internals reach the user.
    private static async Task<string> DescribeFailureAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));
                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }

                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return detail.GetString()!;
            }
            catch (JsonException) { /* fall through to the generic message */ }

            return "Please check the form fields and try again.";
        }

        return "The server couldn't process the request. Please try again.";
    }
}
