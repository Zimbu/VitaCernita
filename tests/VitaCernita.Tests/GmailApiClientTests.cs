using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Actions;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Auth;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Queries;

namespace VitaCernita.Tests;

public class GmailApiClientTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; }

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            Handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Handler(request));
        }
    }

    #region FakeGmailApiClient Tests

    [Fact]
    public async Task FakeClient_ListLabels_FiltersSystemLabelsByDefault()
    {
        var fake = new FakeGmailApiClient();
        fake.AddLabel(GmailLabel.CreateSystemLabel("INBOX", id: "INBOX"));
        fake.AddLabel(new GmailLabel("Receipts", id: "L1", messageListVisibility: "show"));
        fake.AddLabel(GmailLabel.CreateSystemLabel("TRASH", id: "TRASH"));
        fake.AddLabel(new GmailLabel("Work", id: "L2"));

        var userLabels = await fake.ListLabelsAsync("me", onlyUserLabels: true);
        Assert.Equal(2, userLabels.Count);
        Assert.Contains(userLabels, l => l.Name == "Receipts");
        Assert.Contains(userLabels, l => l.Name == "Work");
        Assert.Equal(1, fake.ListLabelsCallCount);

        var allLabels = await fake.ListLabelsAsync("me", onlyUserLabels: false);
        Assert.Equal(4, allLabels.Count);
        Assert.Equal(2, fake.ListLabelsCallCount);
    }

    [Fact]
    public async Task FakeClient_GetLabel_MatchesByIdAndName()
    {
        var fake = new FakeGmailApiClient();
        fake.AddLabel(new GmailLabel("Receipts", id: "L_REC"));

        var byId = await fake.GetLabelAsync("L_REC");
        Assert.NotNull(byId);
        Assert.Equal("Receipts", byId.Name);

        var byName = await fake.GetLabelAsync("Receipts");
        Assert.NotNull(byName);
        Assert.Equal("L_REC", byName.Id);

        var missing = await fake.GetLabelAsync("NonExistent");
        Assert.Null(missing);

        Assert.Equal(3, fake.GetLabelCallCount);
    }

    [Fact]
    public async Task FakeClient_ListAndGetFilters_WorksProperly()
    {
        var fake = new FakeGmailApiClient();
        var f1 = new GmailFilter("F1", new RawQueryCondition("from:test@example.com"), new GmailAction().Archive());
        var f2 = new GmailFilter("F2", new RawQueryCondition("subject:invoice"), new GmailAction().Star());

        fake.AddFilters(new[] { f1, f2 });

        var filters = await fake.ListFiltersAsync();
        Assert.Equal(2, filters.Count);
        Assert.Equal(1, fake.ListFiltersCallCount);

        var get1 = await fake.GetFilterAsync("F1");
        Assert.NotNull(get1);
        Assert.Equal("from:test@example.com", get1.ToGmailQuery());

        var getMissing = await fake.GetFilterAsync("Unknown");
        Assert.Null(getMissing);
    }

    [Fact]
    public async Task FakeClient_SimulatedHttpError_ThrowsGmailApiException()
    {
        var fake = new FakeGmailApiClient();
        fake.SimulatedHttpError = HttpStatusCode.Unauthorized;

        var ex = await Assert.ThrowsAsync<GmailApiException>(() => fake.ListLabelsAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task FakeClient_SimulatedError_ThrowsSpecifiedException()
    {
        var fake = new FakeGmailApiClient();
        fake.SimulatedError = new InvalidOperationException("Simulated network failure");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => fake.ListLabelsAsync());
        Assert.Equal("Simulated network failure", ex.Message);
    }

    #endregion

    #region HttpGmailApiClient Tests

    [Fact]
    public async Task HttpClient_ListLabelsAsync_SendsAuthorizationAndParsesResponse()
    {
        string mockResponseJson = @"{
            ""labels"": [
                { ""id"": ""INBOX"", ""name"": ""INBOX"", ""type"": ""system"" },
                { ""id"": ""L1"", ""name"": ""Invoices"", ""type"": ""user"", ""messageListVisibility"": ""show"" }
            ]
        }";

        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseJson)
            };
        });

        var httpClient = new HttpClient(handler);
        var tokenProvider = new BearerTokenProvider("secret-oauth-token");
        var client = new HttpGmailApiClient(httpClient, tokenProvider);

        var labels = await client.ListLabelsAsync("me", onlyUserLabels: true);

        Assert.Single(labels);
        Assert.Equal("Invoices", labels[0].Name);
        Assert.Equal("L1", labels[0].Id);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("https://gmail.googleapis.com/gmail/v1/users/me/labels", capturedRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("secret-oauth-token", capturedRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task HttpClient_GetLabelAsync_ReturnsLabelOn200_NullOn404()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.EndsWith("L_FOUND") == true)
            {
                string json = @"{ ""id"": ""L_FOUND"", ""name"": ""FoundLabel"", ""type"": ""user"" }";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new HttpGmailApiClient(new HttpClient(handler));

        var found = await client.GetLabelAsync("L_FOUND");
        Assert.NotNull(found);
        Assert.Equal("FoundLabel", found.Name);

        var notFound = await client.GetLabelAsync("L_MISSING");
        Assert.Null(notFound);
    }

    [Fact]
    public async Task HttpClient_ListFiltersAsync_ParsesFiltersResponse()
    {
        string mockFiltersJson = @"{
            ""filter"": [
                {
                    ""id"": ""filter_123"",
                    ""criteria"": { ""query"": ""from:billing@stripe.com"" },
                    ""action"": { ""removeLabelIds"": [""INBOX""], ""addLabelIds"": [""Receipts""] }
                }
            ]
        }";

        var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(mockFiltersJson) });

        var client = new HttpGmailApiClient(new HttpClient(handler));
        var filters = await client.ListFiltersAsync();

        Assert.Single(filters);
        Assert.Equal("filter_123", filters[0].Id);
        Assert.Equal("from:billing@stripe.com", filters[0].ToGmailQuery());
        Assert.True(filters[0].Action?.IsArchive);
    }

    [Fact]
    public async Task HttpClient_GetFilterAsync_ReturnsFilterOn200_NullOn404()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri?.AbsolutePath.EndsWith("F_EXISTS") == true)
            {
                string json = @"{ ""id"": ""F_EXISTS"", ""criteria"": { ""from"": ""boss@corp.com"" } }";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new HttpGmailApiClient(new HttpClient(handler));

        var filter = await client.GetFilterAsync("F_EXISTS");
        Assert.NotNull(filter);
        Assert.Equal("F_EXISTS", filter.Id);
        Assert.Equal("from:boss@corp.com", filter.ToGmailQuery());

        var missing = await client.GetFilterAsync("F_NONE");
        Assert.Null(missing);
    }

    [Fact]
    public async Task HttpClient_ErrorResponse_ThrowsGmailApiException()
    {
        var handler = new MockHttpMessageHandler(req =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(@"{ ""error"": ""Backend Failure"" }")
            });

        var client = new HttpGmailApiClient(new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<GmailApiException>(() => client.ListLabelsAsync());
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Contains("Backend Failure", ex.ResponseBody);
    }

    #endregion

    #region GmailAccountDiffer Integration Tests

    [Fact]
    public async Task GmailAccountDiffer_DiffLabelsAsync_IdentifiesDifferencesWithFake()
    {
        var fake = new FakeGmailApiClient();
        fake.AddLabel(new GmailLabel("Receipts", id: "L1", messageListVisibility: "show"));
        fake.AddLabel(new GmailLabel("Deprecated", id: "L2"));

        var desired = new List<GmailLabel>
        {
            new GmailLabel("Receipts", messageListVisibility: "hide"), // Modified
            new GmailLabel("NewTag", messageListVisibility: "show")     // Added
        };

        var diff = await GmailAccountDiffer.DiffLabelsAsync(fake, desired);

        Assert.True(diff.HasDifferences);
        Assert.Equal(1, diff.TotalCreations);
        Assert.Equal("NewTag", diff.Creations[0].Name);

        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("Receipts", diff.Modifications[0].Name);

        Assert.Equal(1, diff.TotalDeletions);
        Assert.Equal("Deprecated", diff.Deletions[0].Name);
    }

    [Fact]
    public async Task GmailAccountDiffer_DiffLabelsFromScriptAsync_DiffsDirectlyFromLua()
    {
        var fake = new FakeGmailApiClient();
        fake.AddLabel(new GmailLabel("Work", id: "L_WORK", messageListVisibility: "show", labelListVisibility: "labelShow"));

        string lua = @"
            return {
                label {
                    name = 'Work',
                    message_list_visibility = 'hide',
                    label_list_visibility = 'labelShow'
                },
                label {
                    name = 'Urgent',
                    color = { text = 'white', background = 'black' }
                }
            }
        ";

        var diff = await GmailAccountDiffer.DiffLabelsFromScriptAsync(fake, lua);

        Assert.Equal(1, diff.TotalCreations);
        Assert.Equal("Urgent", diff.Creations[0].Name);

        Assert.Equal(1, diff.TotalModifications);
        Assert.Equal("Work", diff.Modifications[0].Name);
        Assert.Equal("L_WORK", diff.Modifications[0].Id);
        Assert.Equal("hide", diff.Modifications[0].GetPatchPayload()["messageListVisibility"]);
    }

    #endregion

    #region Deserialization Unit Tests

    [Fact]
    public void GmailFilter_FromJson_ParsesStructuredCriteriaAndAction()
    {
        string json = @"{
            ""id"": ""f_001"",
            ""criteria"": {
                ""from"": ""billing@stripe.com"",
                ""to"": ""accounting@company.com"",
                ""subject"": ""Invoice"",
                ""hasAttachment"": true
            },
            ""action"": {
                ""removeLabelIds"": [""INBOX""],
                ""addLabelIds"": [""CATEGORY_PURCHASES"", ""Receipts""],
                ""forward"": ""cfo@company.com""
            }
        }";

        var filter = GmailFilter.FromJson(json);

        Assert.Equal("f_001", filter.Id);
        Assert.Equal("from:billing@stripe.com to:accounting@company.com subject:Invoice has:attachment", filter.ToGmailQuery());
        Assert.NotNull(filter.Action);
        Assert.True(filter.Action.IsArchive);
        Assert.Equal("CATEGORY_PURCHASES", filter.Action.Category);
        Assert.Equal("cfo@company.com", filter.Action.Forward);
    }

    [Fact]
    public void GmailFilter_FromApiListResponse_HandlesEmptyOrMissingList()
    {
        var empty = GmailFilter.FromApiListResponse("{}");
        Assert.Empty(empty);

        var emptyList = GmailFilter.FromApiListResponse(@"{ ""filter"": [] }");
        Assert.Empty(emptyList);
    }

    [Fact]
    public void GmailFilter_FromDictionary_ParsesCorrectly()
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = "f_dict",
            ["criteria"] = new Dictionary<string, object?>
            {
                ["query"] = "from:team@corp.com",
                ["subject"] = "Meeting"
            },
            ["action"] = new Dictionary<string, object?>
            {
                ["addLabelIds"] = new[] { "Work" }
            }
        };

        var filter = GmailFilter.FromDictionary(dict);
        Assert.Equal("f_dict", filter.Id);
        Assert.Equal("from:team@corp.com subject:Meeting", filter.ToGmailQuery());
        Assert.NotNull(filter.Action);
        Assert.Contains("Work", filter.Action.CustomLabels);
    }

    #endregion

    #region BearerTokenProvider Tests

    [Fact]
    public async Task BearerTokenProvider_ExplicitToken_ReturnsToken()
    {
        var provider = new BearerTokenProvider("my-test-token");
        var token = await provider.GetAccessTokenAsync();
        Assert.Equal("my-test-token", token);
    }

    [Fact]
    public async Task BearerTokenProvider_EnvVar_ReturnsTokenFromEnvironment()
    {
        string envName = "TEST_GMAIL_TOKEN_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(envName, "env-secret-123");
            var provider = new BearerTokenProvider(token: null, envVarName: envName);
            var token = await provider.GetAccessTokenAsync();
            Assert.Equal("env-secret-123", token);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envName, null);
        }
    }

    [Fact]
    public async Task BearerTokenProvider_NoTokenSet_ReturnsNull()
    {
        var provider = new BearerTokenProvider(token: null, envVarName: "DEFINITELY_NOT_SET_" + Guid.NewGuid().ToString("N"));
        var token = await provider.GetAccessTokenAsync();
        Assert.Null(token);
    }

    [Fact]
    public void FakeClient_ClearAndReset_ResetsStateAndCounts()
    {
        var fake = new FakeGmailApiClient();
        fake.AddLabel(new GmailLabel("Test", id: "L1"));
        fake.AddFilter(new GmailFilter("F1"));

        fake.Clear();

        Assert.Equal(0, fake.ListLabelsCallCount);
        Assert.Equal(0, fake.GetLabelCallCount);
        Assert.Equal(0, fake.ListFiltersCallCount);
        Assert.Equal(0, fake.GetFilterCallCount);
    }

    #endregion
}
