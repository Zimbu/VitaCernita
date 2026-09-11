using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Api;
using VitaCernita.Core.Api.Fakes;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.AutoReply.Diff;
using VitaCernita.Core.AutoReply.Validation;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Sources;

namespace VitaCernita.Tests;

public class AutoReplyTests
{
    #region Domain Model & Serialization Tests

    [Fact]
    public void AutoReply_DefaultConstructor_HasExpectedDefaults()
    {
        var ar = new AutoReply();
        Assert.False(ar.EnableAutoReply);
        Assert.Null(ar.ResponseSubject);
        Assert.Null(ar.ResponseBodyPlainText);
        Assert.Null(ar.ResponseBodyHtml);
        Assert.False(ar.RestrictToContacts);
        Assert.False(ar.RestrictToDomain);
        Assert.Null(ar.StartTime);
        Assert.Null(ar.EndTime);
        Assert.Null(ar.StartDateTime);
        Assert.Null(ar.EndDateTime);
        Assert.False(ar.HasContent);
    }

    [Fact]
    public void AutoReply_ToDictionary_ProducesExactGmailApiFormat()
    {
        var ar = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Out of Office",
            ResponseBodyPlainText = "I am on vacation.",
            ResponseBodyHtml = "<p>I am on vacation.</p>",
            RestrictToContacts = true,
            RestrictToDomain = true,
            StartTime = 1760000000000L,
            EndTime = 1761000000000L
        };

        var dict = ar.ToDictionary();

        Assert.Equal(true, dict["enableAutoReply"]);
        Assert.Equal("Out of Office", dict["responseSubject"]);
        Assert.Equal("I am on vacation.", dict["responseBodyPlainText"]);
        Assert.Equal("<p>I am on vacation.</p>", dict["responseBodyHtml"]);
        Assert.Equal(true, dict["restrictToContacts"]);
        Assert.Equal(true, dict["restrictToDomain"]);
        Assert.Equal("1760000000000", dict["startTime"]);
        Assert.Equal("1761000000000", dict["endTime"]);
    }

    [Fact]
    public void AutoReply_ToJsonAndFromJson_Roundtrip()
    {
        var original = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Holiday Leave",
            ResponseBodyPlainText = "Away from keyboard",
            ResponseBodyHtml = "<p>Away from keyboard</p>",
            RestrictToContacts = true,
            RestrictToDomain = false,
            StartTime = 1760000000000L,
            EndTime = 1761000000000L
        };

        string json = original.ToJson();
        var deserialized = AutoReply.FromJson(json);

        Assert.Equal(original, deserialized);
        Assert.Equal(original.ResponseSubject, deserialized.ResponseSubject);
        Assert.Equal(original.StartTime, deserialized.StartTime);
        Assert.Equal(original.StartDateTime, deserialized.StartDateTime);
    }

    [Fact]
    public void AutoReply_FromJson_ParsesStringAndNumberTimestamps()
    {
        string jsonWithStrings = @"{
            ""enableAutoReply"": true,
            ""responseSubject"": ""Away"",
            ""startTime"": ""1760000000000"",
            ""endTime"": ""1761000000000""
        }";

        var ar1 = AutoReply.FromJson(jsonWithStrings);
        Assert.Equal(1760000000000L, ar1.StartTime);
        Assert.Equal(1761000000000L, ar1.EndTime);

        string jsonWithNumbers = @"{
            ""enableAutoReply"": true,
            ""responseSubject"": ""Away"",
            ""startTime"": 1760000000000,
            ""endTime"": 1761000000000
        }";

        var ar2 = AutoReply.FromJson(jsonWithNumbers);
        Assert.Equal(1760000000000L, ar2.StartTime);
        Assert.Equal(1761000000000L, ar2.EndTime);
    }

    [Fact]
    public void AutoReply_FromDictionary_SupportsFieldAliases()
    {
        var dict = new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["subject"] = "Annual Leave",
            ["body"] = "Will reply next week",
            ["contacts_only"] = true,
            ["domain_only"] = true,
            ["start_time"] = 1760000000000L,
            ["end_time"] = 1761000000000L
        };

        var ar = AutoReply.FromDictionary(dict);
        Assert.True(ar.EnableAutoReply);
        Assert.Equal("Annual Leave", ar.ResponseSubject);
        Assert.Equal("Will reply next week", ar.ResponseBodyPlainText);
        Assert.True(ar.RestrictToContacts);
        Assert.True(ar.RestrictToDomain);
        Assert.Equal(1760000000000L, ar.StartTime);
        Assert.Equal(1761000000000L, ar.EndTime);
    }

    [Fact]
    public void AutoReply_EqualsAndHashCode_RespectsAllProperties()
    {
        var ar1 = new AutoReply(true, "Sub", "Body", null, true, false, 100, 200);
        var ar2 = new AutoReply(true, "Sub", "Body", null, true, false, 100, 200);
        var ar3 = new AutoReply(true, "Sub2", "Body", null, true, false, 100, 200);

        Assert.Equal(ar1, ar2);
        Assert.Equal(ar1.GetHashCode(), ar2.GetHashCode());
        Assert.NotEqual(ar1, ar3);
    }

    [Fact]
    public void AutoReply_ToString_ProducesDescriptiveString()
    {
        var ar = new AutoReply(true, "Summer Vacation", "Out in the woods");
        string str = ar.ToString();
        Assert.Contains("Enabled: True", str);
        Assert.Contains("Summer Vacation", str);
        Assert.Contains("Out in the woods", str);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void AutoReplyValidator_ValidEnabledAutoReply_Passes()
    {
        var ar = new AutoReply(true, "Vacation", null, null);
        AutoReplyValidator.Validate(ar);

        var arHtml = new AutoReply(true, null, null, "<p>Away</p>");
        AutoReplyValidator.Validate(arHtml);

        var arPlain = new AutoReply(true, null, "Away", null);
        AutoReplyValidator.Validate(arPlain);
    }

    [Fact]
    public void AutoReplyValidator_EnabledWithoutContent_ThrowsException()
    {
        var ar = new AutoReply(true, null, null, null);
        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyValidator.Validate(ar));
        Assert.Contains("either the response subject or the response body must be nonempty", ex.Message);
    }

    [Fact]
    public void AutoReplyValidator_DisabledWithoutContent_Passes()
    {
        var ar = new AutoReply(false, null, null, null);
        AutoReplyValidator.Validate(ar); // No exception
    }

    [Fact]
    public void AutoReplyValidator_NegativeStartTime_ThrowsException()
    {
        var ar = new AutoReply(true, "Sub", "Body", null, startTime: -50);
        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyValidator.Validate(ar));
        Assert.Contains("startTime cannot be negative", ex.Message);
    }

    [Fact]
    public void AutoReplyValidator_NegativeEndTime_ThrowsException()
    {
        var ar = new AutoReply(true, "Sub", "Body", null, endTime: -1);
        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyValidator.Validate(ar));
        Assert.Contains("endTime cannot be negative", ex.Message);
    }

    [Fact]
    public void AutoReplyValidator_StartTimeAfterEndTime_ThrowsException()
    {
        var ar = new AutoReply(true, "Sub", "Body", null, startTime: 2000, endTime: 1000);
        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyValidator.Validate(ar));
        Assert.Contains("startTime (2000) must precede or equal endTime (1000)", ex.Message);
    }

    [Fact]
    public void AutoReplyValidator_StartTimeEqualOrBeforeEndTime_Passes()
    {
        var ar = new AutoReply(true, "Sub", "Body", null, startTime: 1000, endTime: 1000);
        AutoReplyValidator.Validate(ar);

        var ar2 = new AutoReply(true, "Sub", "Body", null, startTime: 1000, endTime: 2000);
        AutoReplyValidator.Validate(ar2);
    }

    #endregion

    #region Workspace Account Domain Restriction Validation Tests

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("john.doe@gmail.com")]
    [InlineData("USER@GMAIL.COM")]
    [InlineData("alice@googlemail.com")]
    public void AutoReplyValidator_RestrictToDomain_OnGmailUser_ThrowsException(string email)
    {
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: true);
        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyValidator.ValidateForAccount(ar, email));
        Assert.Contains("only valid for Google Workspace users", ex.Message);
        Assert.Contains("@gmail.com", ex.Message);
    }

    [Theory]
    [InlineData("alice@company.com")]
    [InlineData("bob@university.edu")]
    [InlineData("admin@myorg.org")]
    public void AutoReplyValidator_RestrictToDomain_OnWorkspaceUser_Passes(string email)
    {
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: true);
        AutoReplyValidator.ValidateForAccount(ar, email); // Should not throw
    }

    [Fact]
    public void AutoReplyValidator_RestrictToDomain_OnMe_Passes()
    {
        // "me" cannot be statically determined without API interaction
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: true);
        AutoReplyValidator.ValidateForAccount(ar, "me"); // Should not throw
    }

    [Fact]
    public void AutoReplyValidator_DomainRestrictionFalse_OnGmailUser_Passes()
    {
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: false);
        AutoReplyValidator.ValidateForAccount(ar, "user@gmail.com"); // Should not throw
    }

    [Fact]
    public void AutoReplyValidator_AccountClassificationHelpers_WorkAsExpected()
    {
        Assert.True(AutoReplyValidator.IsStandardGmailAccount("test@gmail.com"));
        Assert.True(AutoReplyValidator.IsStandardGmailAccount("test@googlemail.com"));
        Assert.False(AutoReplyValidator.IsStandardGmailAccount("test@corp.com"));
        Assert.False(AutoReplyValidator.IsStandardGmailAccount("me"));
        Assert.False(AutoReplyValidator.IsStandardGmailAccount(null));

        Assert.True(AutoReplyValidator.IsGoogleWorkspaceAccount("test@corp.com"));
        Assert.True(AutoReplyValidator.IsGoogleWorkspaceAccount("test@alumni.stanford.edu"));
        Assert.False(AutoReplyValidator.IsGoogleWorkspaceAccount("test@gmail.com"));
        Assert.False(AutoReplyValidator.IsGoogleWorkspaceAccount("me"));
        Assert.False(AutoReplyValidator.IsGoogleWorkspaceAccount(null));
    }

    #endregion

    #region Lua DSL Loading Tests

    [Fact]
    public async Task GmailFilterLoader_DeclarativeTable_ParsesAutoReply()
    {
        string lua = @"
            return auto_reply {
                enabled = true,
                subject = 'Out of Office until Oct 15',
                body = 'I will be checking email intermittently.',
                contacts_only = true,
                domain_only = false,
                start_date = '2026-10-01',
                end_date = '2026-10-15'
            }
        ";

        var loader = new GmailFilterLoader();
        var ar = await loader.LoadAutoReplyFromScriptAsync(lua);

        Assert.NotNull(ar);
        Assert.True(ar.EnableAutoReply);
        Assert.Equal("Out of Office until Oct 15", ar.ResponseSubject);
        Assert.Equal("I will be checking email intermittently.", ar.ResponseBodyPlainText);
        Assert.True(ar.RestrictToContacts);
        Assert.False(ar.RestrictToDomain);
        Assert.NotNull(ar.StartTime);
        Assert.NotNull(ar.EndTime);
        Assert.True(ar.StartTime < ar.EndTime);
    }

    [Fact]
    public async Task GmailFilterLoader_FluentBuilder_ParsesAutoReply()
    {
        string lua = @"
            return auto_reply()
                :enable(true)
                :subject('Parental Leave')
                :body('I am on parental leave until Jan 2027.')
                :html('<p>I am on <b>parental leave</b> until Jan 2027.</p>')
                :contacts_only(false)
                :domain_only(true)
                :start('2026-11-01')
                :end_time('2027-01-01')
                :build()
        ";

        var loader = new GmailFilterLoader();
        var ar = await loader.LoadAutoReplyFromScriptAsync(lua);

        Assert.NotNull(ar);
        Assert.True(ar.EnableAutoReply);
        Assert.Equal("Parental Leave", ar.ResponseSubject);
        Assert.Equal("I am on parental leave until Jan 2027.", ar.ResponseBodyPlainText);
        Assert.Equal("<p>I am on <b>parental leave</b> until Jan 2027.</p>", ar.ResponseBodyHtml);
        Assert.False(ar.RestrictToContacts);
        Assert.True(ar.RestrictToDomain);
    }

    [Fact]
    public async Task GmailFilterLoader_Aliases_ParseAutoReply()
    {
        var loader = new GmailFilterLoader();

        // 1. VacationSettings alias
        string lua1 = @"return VacationSettings { subject = 'Away 1', body = 'Body 1' }";
        var ar1 = await loader.LoadAutoReplyFromScriptAsync(lua1);
        Assert.NotNull(ar1);
        Assert.Equal("Away 1", ar1.ResponseSubject);

        // 2. vacation alias
        string lua2 = @"return vacation { subject = 'Away 2', body = 'Body 2' }";
        var ar2 = await loader.LoadAutoReplyFromScriptAsync(lua2);
        Assert.NotNull(ar2);
        Assert.Equal("Away 2", ar2.ResponseSubject);

        // 3. AutoReply alias
        string lua3 = @"return AutoReply { subject = 'Away 3', body = 'Body 3' }";
        var ar3 = await loader.LoadAutoReplyFromScriptAsync(lua3);
        Assert.NotNull(ar3);
        Assert.Equal("Away 3", ar3.ResponseSubject);
    }

    [Fact]
    public async Task GmailFilterLoader_DateFormats_ParsesDatesToEpochMs()
    {
        string lua = @"
            return auto_reply {
                enabled = true,
                subject = 'Away',
                body = 'Away',
                start_date = '2026-10-01T08:30:00Z',
                end_date = '2026-10-10T17:00:00Z'
            }
        ";

        var loader = new GmailFilterLoader();
        var ar = await loader.LoadAutoReplyFromScriptAsync(lua);

        Assert.NotNull(ar);
        Assert.NotNull(ar.StartTime);
        Assert.NotNull(ar.EndTime);

        var startDto = DateTimeOffset.FromUnixTimeMilliseconds(ar.StartTime.Value);
        Assert.Equal(2026, startDto.Year);
        Assert.Equal(10, startDto.Month);
        Assert.Equal(1, startDto.Day);
        Assert.Equal(8, startDto.Hour);
        Assert.Equal(30, startDto.Minute);
    }

    [Fact]
    public async Task GmailFilterLoader_CombinedConfig_ParsesFiltersLabelsAndAutoReply()
    {
        string lua = @"
            return {
                labels = {
                    label { name = 'Urgent' }
                },
                filters = {
                    filter {
                        query = From('boss@company.com'),
                        action = mark_important
                    }
                },
                auto_reply = auto_reply {
                    enabled = true,
                    subject = 'Automatic Reply: On Vacation',
                    body = 'I am currently away.'
                }
            }
        ";

        var loader = new GmailFilterLoader();
        var config = await loader.LoadConfigurationFromScriptAsync(lua);

        Assert.Single(config.Labels);
        Assert.Single(config.Filters);
        Assert.NotNull(config.AutoReply);
        Assert.True(config.AutoReply.EnableAutoReply);
        Assert.Equal("Automatic Reply: On Vacation", config.AutoReply.ResponseSubject);
    }

    [Fact]
    public async Task GmailFilterLoader_GlobalAutoReply_ParsesSuccessfully()
    {
        string lua = @"
            auto_reply = {
                enabled = true,
                subject = 'Global Vacation',
                body = 'Back soon'
            }
        ";

        var loader = new GmailFilterLoader();
        var ar = await loader.LoadAutoReplyFromScriptAsync(lua);

        Assert.NotNull(ar);
        Assert.Equal("Global Vacation", ar.ResponseSubject);
    }

    #endregion

    #region Diffing Tests

    [Fact]
    public void AutoReplyDiffer_BothNull_ReturnsUnchanged()
    {
        var diff = AutoReplyDiffer.Diff(null, null);
        Assert.Equal(AutoReplyDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void AutoReplyDiffer_IdenticalSettings_ReturnsUnchanged()
    {
        var current = new AutoReply(true, "Away", "Body", null, false, false, 100, 200);
        var desired = new AutoReply(true, "Away", "Body", null, false, false, 100, 200);

        var diff = AutoReplyDiffer.Diff(current, desired);
        Assert.Equal(AutoReplyDiffType.Unchanged, diff.DiffType);
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.FieldDifferences);
    }

    [Fact]
    public void AutoReplyDiffer_CurrentNull_DesiredEnabled_ReturnsAdded()
    {
        var desired = new AutoReply(true, "Away", "Body");
        var diff = AutoReplyDiffer.Diff(null, desired);

        Assert.Equal(AutoReplyDiffType.Added, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal(desired, diff.DesiredAutoReply);
    }

    [Fact]
    public void AutoReplyDiffer_CurrentEnabled_DesiredDisabled_ReturnsDisabled()
    {
        var current = new AutoReply(true, "Away", "Body");
        var desired = new AutoReply(false, "Away", "Body");

        var diff = AutoReplyDiffer.Diff(current, desired);
        Assert.Equal(AutoReplyDiffType.Disabled, diff.DiffType);
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public void AutoReplyDiffer_ModifiedSubjectAndBody_ReportsFieldDifferences()
    {
        var current = new AutoReply(true, "Old Subject", "Old Body", null, false, false);
        var desired = new AutoReply(true, "New Subject", "New Body", null, true, false);

        var diff = AutoReplyDiffer.Diff(current, desired);

        Assert.Equal(AutoReplyDiffType.Modified, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal(3, diff.FieldDifferences.Count);
        Assert.Contains(diff.FieldDifferences, f => f.FieldName == "responseSubject" && (string)f.CurrentValue! == "Old Subject" && (string)f.DesiredValue! == "New Subject");
        Assert.Contains(diff.FieldDifferences, f => f.FieldName == "responseBodyPlainText" && (string)f.CurrentValue! == "Old Body" && (string)f.DesiredValue! == "New Body");
        Assert.Contains(diff.FieldDifferences, f => f.FieldName == "restrictToContacts" && (bool)f.CurrentValue! == false && (bool)f.DesiredValue! == true);
    }

    [Fact]
    public void AutoReplyDiffer_IgnoreUnsetDesiredFields_IgnoresNulls()
    {
        var current = new AutoReply(true, "Existing Subject", "Existing Body", "<p>Html</p>", false, false, 100, 200);
        var desired = new AutoReply(true, "Existing Subject", null, null, false, false, null, null);

        var options = new AutoReplyDiffOptions { IgnoreUnsetDesiredFields = true };
        var diff = AutoReplyDiffer.Diff(current, desired, options);

        Assert.Equal(AutoReplyDiffType.Unchanged, diff.DiffType);
        Assert.Empty(diff.FieldDifferences);
    }

    [Fact]
    public void AutoReplyDiffer_TargetAccountGmail_ReportsAccountError()
    {
        var current = new AutoReply(false);
        var desired = new AutoReply(true, "Subject", "Body", restrictToDomain: true);

        var options = new AutoReplyDiffOptions { TargetAccount = "user@gmail.com" };
        var diff = AutoReplyDiffer.Diff(current, desired, options);

        Assert.NotNull(diff.AccountError);
        Assert.Contains("only valid for Google Workspace accounts", diff.AccountError);
        Assert.Contains("user@gmail.com", diff.AccountError);

        string report = diff.ToDryRunReport();
        Assert.Contains("[ERROR]", report);
    }

    [Fact]
    public void AutoReplyDiffer_TargetAccountGmail_StrictValidation_ThrowsException()
    {
        var current = new AutoReply(false);
        var desired = new AutoReply(true, "Subject", "Body", restrictToDomain: true);

        var options = new AutoReplyDiffOptions
        {
            TargetAccount = "user@gmail.com",
            StrictAccountValidation = true
        };

        var ex = Assert.Throws<AutoReplyValidationException>(() => AutoReplyDiffer.Diff(current, desired, options));
        Assert.Contains("only valid for Google Workspace users", ex.Message);
    }

    [Fact]
    public void AutoReplyDiffer_ToDryRunReport_OutputsReadableSummary()
    {
        var desired = new AutoReply(true, "Holiday", "Away until next week");
        var diff = AutoReplyDiffer.Diff(null, desired);

        string report = diff.ToDryRunReport();
        Assert.Contains("+ Enable Auto-Reply", report);
        Assert.Contains("Holiday", report);
        Assert.Contains("Away until next week", report);
    }

    #endregion

    #region Fake API Client Tests

    [Fact]
    public async Task FakeGmailApiClient_GetAndUpdateAutoReply_TracksCallCounts()
    {
        var fake = new FakeGmailApiClient();
        Assert.Equal(0, fake.GetAutoReplyCallCount);
        Assert.Equal(0, fake.UpdateAutoReplyCallCount);

        var initial = await fake.GetAutoReplyAsync("me");
        Assert.Null(initial);
        Assert.Equal(1, fake.GetAutoReplyCallCount);

        var toUpdate = new AutoReply(true, "Out of office", "On vacation");
        var updated = await fake.UpdateAutoReplyAsync(toUpdate, "me");

        Assert.Equal(1, fake.UpdateAutoReplyCallCount);
        Assert.Equal(toUpdate, updated);

        var retrieved = await fake.GetAutoReplyAsync("me");
        Assert.Equal(toUpdate, retrieved);
        Assert.Equal(2, fake.GetAutoReplyCallCount);
    }

    [Fact]
    public async Task FakeGmailApiClient_UpdateWithDomainRestrictionOnGmailAccount_ThrowsGmailApiException()
    {
        var fake = new FakeGmailApiClient();
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: true);

        var ex = await Assert.ThrowsAsync<GmailApiException>(() => fake.UpdateAutoReplyAsync(ar, "user@gmail.com"));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Contains("only available for Google Workspace users", ex.Message);
    }

    [Fact]
    public async Task FakeGmailApiClient_UpdateWithDomainRestrictionOnWorkspaceAccount_Succeeds()
    {
        var fake = new FakeGmailApiClient();
        var ar = new AutoReply(true, "Subject", "Body", restrictToDomain: true);

        var updated = await fake.UpdateAutoReplyAsync(ar, "alice@mycorp.com");
        Assert.NotNull(updated);
        Assert.True(updated.RestrictToDomain);
    }

    #endregion

    #region IGmailSource & Agnostic Differ Tests

    [Fact]
    public async Task GmailSourceDiffer_DiffAutoReplyAsync_DiffsLuaAndFakeSources()
    {
        string lua = @"
            return {
                auto_reply = auto_reply {
                    enabled = true,
                    subject = 'Conference Trip',
                    body = 'Attending conference'
                }
            }
        ";

        var fake = new FakeGmailApiClient();
        fake.SetAutoReply(new AutoReply(true, "Old Vacation", "Old body"));

        var currentSource = new ApiGmailSource(fake, "me", name: "Current Account");
        var desiredSource = LuaGmailSource.FromScript(lua, "Desired Lua");

        var diff = await GmailSourceDiffer.DiffAutoReplyAsync(currentSource, desiredSource);

        Assert.Equal(AutoReplyDiffType.Modified, diff.DiffType);
        Assert.True(diff.HasChanges);
        Assert.Equal(2, diff.FieldDifferences.Count);
        Assert.Contains(diff.FieldDifferences, f => f.FieldName == "responseSubject");
        Assert.Contains(diff.FieldDifferences, f => f.FieldName == "responseBodyPlainText");
    }

    [Fact]
    public async Task GmailSourceDiffer_DiffAutoReplyAsync_SupportsLinqQueryExpressions()
    {
        var sourceA = new InMemoryGmailSource(autoReply: new AutoReply(true, "Sub A", "Body A"));
        var sourceB = new InMemoryGmailSource(autoReply: new AutoReply(true, "Sub B", "Body B"));

        // Diff filtering for enabled auto replies
        var diff = await GmailSourceDiffer.DiffAutoReplyAsync(
            sourceA,
            sourceB,
            currentFilter: q => q.Where(ar => ar.EnableAutoReply),
            desiredFilter: q => q.Where(ar => ar.EnableAutoReply));

        Assert.Equal(AutoReplyDiffType.Modified, diff.DiffType);
    }

    [Fact]
    public async Task GmailAccountDiffer_DiffAutoReplyAsync_DiffsAgainstAccount()
    {
        var fake = new FakeGmailApiClient();
        fake.SetAutoReply(new AutoReply(false));

        var desired = new AutoReply(true, "Away", "Body");
        var diff = await GmailAccountDiffer.DiffAutoReplyAsync(fake, desired, userId: "me");

        Assert.Equal(AutoReplyDiffType.Added, diff.DiffType);
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public async Task GmailAccountDiffer_DiffAutoReplyFromScriptAsync_DetectsGmailDomainError()
    {
        string lua = @"
            return auto_reply {
                enabled = true,
                subject = 'Away',
                body = 'Back soon',
                domain_only = true
            }
        ";

        var fake = new FakeGmailApiClient();
        var diff = await GmailAccountDiffer.DiffAutoReplyFromScriptAsync(fake, lua, userId: "alice@gmail.com");

        Assert.NotNull(diff.AccountError);
        Assert.Contains("alice@gmail.com", diff.AccountError);
    }

    #endregion
}
