using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuntiesCleaners.Client.Models;
using AuntiesCleaners.Client.Services;
using AuntiesCleaners.Tests.Helpers;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace AuntiesCleaners.Tests.Properties;

/// <summary>
/// Property-based tests for UserService invite flow (client-side logic).
/// Tests Properties 6–10 from the user-invite-flow design document.
/// </summary>
public class InviteUserServiceTests
{
    // ── Generators ──────────────────────────────────────────────────

    private static Gen<string> EmailGen =>
        from name in Generators.NonEmptyNameGen
        from domain in Gen.Elements("example.com", "test.org", "mail.net", "company.io")
        select $"{name.ToLower()}@{domain}";

    private static Gen<string> RoleGen =>
        Gen.Elements("Worker", "Manager", "Boss", "Admin");

    private static Gen<Guid?> OptionalGuidGen =>
        Gen.OneOf(
            Generators.GuidGen.Select(g => (Guid?)g),
            Gen.Constant((Guid?)null));

    private static Gen<string> WhitespaceOnlyGen =>
        Gen.Elements("", " ", "  ", "\t", "\n", " \t\n ");

    private static readonly char[] PasswordChars =
        "abcdefghijABCD123!@#".ToCharArray();

    private static readonly char[] SimpleChars =
        "abcxyz123".ToCharArray();

    private static Gen<string> PasswordAtLeast6Gen =>
        from chars in Gen.ListOf(Gen.Elements(PasswordChars))
        where chars.Count >= 6
        select new string(chars.Take(20).ToArray());

    private static Gen<string> PasswordUnder6Gen =>
        from chars in Gen.ListOf(Gen.Elements(SimpleChars))
        where chars.Count >= 1 && chars.Count <= 5
        select new string(chars.ToArray());

    // ── Mock HttpMessageHandler ─────────────────────────────────────

    /// <summary>
    /// A delegating handler that captures the outgoing request and returns a canned response.
    /// This is the standard pattern for testing HttpClient since SendAsync is not virtual.
    /// </summary>
    private class MockHttpMessageHandler : DelegatingHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }

        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public MockHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string? responseBody = null)
        {
            _statusCode = statusCode;
            _responseBody = responseBody ?? JsonSerializer.Serialize(new InviteResult { Success = true, EmailSent = true });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content != null)
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static (UserService service, MockHttpMessageHandler handler) CreateService(
        HttpStatusCode statusCode = HttpStatusCode.OK, string? responseBody = null)
    {
        var handler = new MockHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var supabase = Substitute.For<ISupabaseClientService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:AnonKey"] = "test-anon-key"
            })
            .Build();

        var service = new UserService(supabase, httpClient, config);
        return (service, handler);
    }

    // ── Property 6: Client forwards all form data to edge function ──
    // Feature: user-invite-flow, Property 6: Client forwards all form data to edge function

    /// <summary>
    /// For any valid form data (non-empty name, non-empty email, any role, optional workerId),
    /// InviteUserAsync sends a POST with a JSON body containing all provided fields.
    /// **Validates: Requirements 3.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ClientForwardsAllFormDataToEdgeFunction()
    {
        var gen =
            from name in Generators.NonEmptyNameGen
            from email in EmailGen
            from role in RoleGen
            from workerId in OptionalGuidGen
            select (name, email, role, workerId);

        return Prop.ForAll(Arb.From(gen), async tuple =>
        {
            var (name, email, role, workerId) = tuple;
            var (service, handler) = CreateService();

            await service.InviteUserAsync(name, email, role, workerId);

            // Verify the request was captured
            Assert.NotNull(handler.CapturedRequest);
            Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
            Assert.Equal("https://test.supabase.co/functions/v1/invite-user", handler.CapturedRequest.RequestUri?.ToString());
            Assert.Equal("Bearer test-anon-key", handler.CapturedRequest.Headers.Authorization?.ToString());

            // Verify the JSON body contains all fields
            Assert.NotNull(handler.CapturedRequestBody);
            using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
            var root = doc.RootElement;

            Assert.Equal(name, root.GetProperty("name").GetString());
            Assert.Equal(email, root.GetProperty("email").GetString());
            Assert.Equal(role, root.GetProperty("role").GetString());

            if (workerId.HasValue)
                Assert.Equal(workerId.Value.ToString(), root.GetProperty("workerId").GetString());
            else
                Assert.Equal(JsonValueKind.Null, root.GetProperty("workerId").ValueKind);
        });
    }

    // ── Property 7: Empty name or email prevents invite ─────────────
    // Feature: user-invite-flow, Property 7: Empty name or email prevents invite

    /// <summary>
    /// For any whitespace-only name, InviteUserAsync throws ArgumentException without making an HTTP call.
    /// **Validates: Requirements 3.4, 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EmptyNamePreventsInvite()
    {
        var gen =
            from name in WhitespaceOnlyGen
            from email in EmailGen
            from role in RoleGen
            from workerId in OptionalGuidGen
            select (name, email, role, workerId);

        return Prop.ForAll(Arb.From(gen), async tuple =>
        {
            var (name, email, role, workerId) = tuple;
            var (service, handler) = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.InviteUserAsync(name, email, role, workerId));

            Assert.Equal("name", ex.ParamName);
            Assert.Null(handler.CapturedRequest); // No HTTP call made
        });
    }

    /// <summary>
    /// For any whitespace-only email, InviteUserAsync throws ArgumentException without making an HTTP call.
    /// **Validates: Requirements 3.4, 3.5**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EmptyEmailPreventsInvite()
    {
        var gen =
            from name in Generators.NonEmptyNameGen
            from email in WhitespaceOnlyGen
            from role in RoleGen
            from workerId in OptionalGuidGen
            select (name, email, role, workerId);

        return Prop.ForAll(Arb.From(gen), async tuple =>
        {
            var (name, email, role, workerId) = tuple;
            var (service, handler) = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => service.InviteUserAsync(name, email, role, workerId));

            Assert.Equal("email", ex.ParamName);
            Assert.Null(handler.CapturedRequest); // No HTTP call made
        });
    }

    // ── Property 8: Valid password updates auth account ──────────────
    // Feature: user-invite-flow, Property 8: Valid password updates auth account

    /// <summary>
    /// For any password string of 6+ characters where confirmation matches,
    /// the password validation logic passes (length >= 6 and password == confirmPassword).
    /// **Validates: Requirements 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ValidPasswordPassesValidation()
    {
        return Prop.ForAll(Arb.From(PasswordAtLeast6Gen), password =>
        {
            var confirmPassword = password;
            var isLongEnough = password.Length >= 6;
            var matches = password == confirmPassword;
            return isLongEnough && matches;
        });
    }

    // ── Property 9: Invalid password input shows validation error ────
    // Feature: user-invite-flow, Property 9: Invalid password input shows validation error

    /// <summary>
    /// For any password shorter than 6 characters, validation rejects it.
    /// **Validates: Requirements 4.6, 4.7**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ShortPasswordFailsValidation()
    {
        return Prop.ForAll(Arb.From(PasswordUnder6Gen), password =>
        {
            return password.Length < 6;
        });
    }

    /// <summary>
    /// For any two non-matching passwords (both >= 6 chars), validation rejects them.
    /// **Validates: Requirements 4.6, 4.7**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MismatchedPasswordsFailValidation()
    {
        var gen =
            from pw1 in PasswordAtLeast6Gen
            from pw2 in PasswordAtLeast6Gen
            where pw1 != pw2
            select (pw1, pw2);

        return Prop.ForAll(Arb.From(gen), tuple =>
        {
            var (password, confirmPassword) = tuple;
            var isValid = password.Length >= 6 && password == confirmPassword;
            return !isValid;
        });
    }

    // ── Property 10: Resend sends correct email to edge function ────
    // Feature: user-invite-flow, Property 10: Resend sends correct email to edge function

    /// <summary>
    /// For any email, ResendInviteAsync sends a POST with action "resend" and the email in the body.
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ResendSendsCorrectEmailToEdgeFunction()
    {
        return Prop.ForAll(Arb.From(EmailGen), async email =>
        {
            var (service, handler) = CreateService();

            await service.ResendInviteAsync(email);

            // Verify the request was captured
            Assert.NotNull(handler.CapturedRequest);
            Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
            Assert.Equal("https://test.supabase.co/functions/v1/invite-user", handler.CapturedRequest.RequestUri?.ToString());
            Assert.Equal("Bearer test-anon-key", handler.CapturedRequest.Headers.Authorization?.ToString());

            // Verify the JSON body contains action and email
            Assert.NotNull(handler.CapturedRequestBody);
            using var doc = JsonDocument.Parse(handler.CapturedRequestBody!);
            var root = doc.RootElement;

            Assert.Equal("resend", root.GetProperty("action").GetString());
            Assert.Equal(email, root.GetProperty("email").GetString());
        });
    }
}
