using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Labels.Validation;

namespace VitaCernita.Tests;

public class GmailLabelTests
{
    private readonly GmailLabelLoader _loader = new();

    #region LabelColor Unit Tests

    [Theory]
    [InlineData("#000000", "#000000")]
    [InlineData("000000", "#000000")]
    [InlineData("#FFFFFF", "#ffffff")]
    [InlineData("ffffff", "#ffffff")]
    [InlineData("#4A86E8", "#4a86e8")]
    [InlineData("#16a766", "#16a766")]
    [InlineData("#fad165", "#fad165")]
    [InlineData("#fb4c2f", "#fb4c2f")]
    public void NormalizeColorOrThrow_ValidHex_NormalizesCorrectly(string input, string expected)
    {
        string normalized = LabelColor.NormalizeColorOrThrow(input);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("black", "#000000")]
    [InlineData("BLACK", "#000000")]
    [InlineData("white", "#ffffff")]
    [InlineData("White", "#ffffff")]
    public void NormalizeColorOrThrow_SupportedAlias_ResolvesToExactHex(string alias, string expectedHex)
    {
        string normalized = LabelColor.NormalizeColorOrThrow(alias);
        Assert.Equal(expectedHex, normalized);
    }

    [Theory]
    [InlineData("red")]       // Standard hex #ff0000 is not in Google's 102 predefined palette
    [InlineData("blue")]      // Standard hex #0000ff is not in Google's 102 predefined palette
    [InlineData("green")]     // Standard hex #008000 is not in Google's 102 predefined palette
    [InlineData("yellow")]    // Standard hex #ffff00 is not in Google's 102 predefined palette
    [InlineData("purple")]    // Standard hex #800080 is not in Google's 102 predefined palette
    [InlineData("orange")]    // Standard hex #ffa500 is not in Google's 102 predefined palette
    [InlineData("#123456")]   // Random hex not in Google's palette
    [InlineData("#ff0000")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeColorOrThrow_NonMatchingColor_ThrowsLabelValidationException(string invalidInput)
    {
        var ex = Assert.Throws<LabelValidationException>(() => LabelColor.NormalizeColorOrThrow(invalidInput));
        Assert.NotNull(ex.Message);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void LabelColor_Constructor_SetsPropertiesAndSerializes()
    {
        var color = new LabelColor("white", "#4a86e8");
        Assert.Equal("#ffffff", color.TextColor);
        Assert.Equal("#4a86e8", color.BackgroundColor);

        var dict = color.ToDictionary();
        Assert.Equal("#ffffff", dict["textColor"]);
        Assert.Equal("#4a86e8", dict["backgroundColor"]);
    }

    [Fact]
    public void LabelColor_Equals_WorksProperly()
    {
        var color1 = new LabelColor("white", "#4a86e8");
        var color2 = new LabelColor("#ffffff", "#4a86e8");
        var color3 = new LabelColor("black", "#4a86e8");

        Assert.Equal(color1, color2);
        Assert.NotEqual(color1, color3);
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    #endregion

    #region MessageListVisibility & LabelListVisibility Tests

    [Theory]
    [InlineData("show", "show")]
    [InlineData("SHOW", "show")]
    [InlineData("hide", "hide")]
    [InlineData("Hide", "hide")]
    public void MessageListVisibility_Normalize_ValidValues(string input, string expected)
    {
        Assert.Equal(expected, MessageListVisibility.Normalize(input));
    }

    [Theory]
    [InlineData("visible")]
    [InlineData("hidden")]
    [InlineData("maybe")]
    [InlineData("")]
    public void MessageListVisibility_Normalize_InvalidValues_Throws(string invalid)
    {
        Assert.Throws<LabelValidationException>(() => MessageListVisibility.Normalize(invalid));
    }

    [Theory]
    [InlineData("labelShow", "labelShow")]
    [InlineData("show", "labelShow")]
    [InlineData("labelShowIfUnread", "labelShowIfUnread")]
    [InlineData("showIfUnread", "labelShowIfUnread")]
    [InlineData("show_if_unread", "labelShowIfUnread")]
    [InlineData("labelHide", "labelHide")]
    [InlineData("hide", "labelHide")]
    public void LabelListVisibility_Normalize_ValidValuesAndAliases(string input, string expected)
    {
        Assert.Equal(expected, LabelListVisibility.Normalize(input));
    }

    [Theory]
    [InlineData("always")]
    [InlineData("never")]
    [InlineData("unread_only")]
    [InlineData("")]
    public void LabelListVisibility_Normalize_InvalidValues_Throws(string invalid)
    {
        Assert.Throws<LabelValidationException>(() => LabelListVisibility.Normalize(invalid));
    }

    #endregion

    #region LabelValidator Tests

    [Theory]
    [InlineData("INBOX")]
    [InlineData("inbox")]
    [InlineData("UNREAD")]
    [InlineData("STARRED")]
    [InlineData("TRASH")]
    [InlineData("SPAM")]
    [InlineData("IMPORTANT")]
    [InlineData("CATEGORY_PERSONAL")]
    [InlineData("CATEGORY_PURCHASES")]
    public void LabelValidator_ReservedSystemLabelName_Throws(string systemLabel)
    {
        var ex = Assert.Throws<LabelValidationException>(() => LabelValidator.ValidateName(systemLabel));
        Assert.Contains("reserved", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LabelValidator_EmptyName_Throws(string? emptyName)
    {
        Assert.Throws<LabelValidationException>(() => LabelValidator.ValidateName(emptyName));
    }

    [Fact]
    public void LabelValidator_ValidCustomName_Succeeds()
    {
        string name = LabelValidator.ValidateName("Finance/Invoices");
        Assert.Equal("Finance/Invoices", name);
    }

    #endregion

    #region GmailLabel Model Tests

    [Fact]
    public void GmailLabel_ToDictionary_FormatsExactGmailApiSchema()
    {
        var label = new GmailLabel(
            name: "Clients/Acme",
            id: "Label_99",
            messageListVisibility: "show",
            labelListVisibility: "labelShow",
            color: new LabelColor("white", "#4a86e8")
        );

        var dict = label.ToDictionary();

        Assert.Equal("Label_99", dict["id"]);
        Assert.Equal("Clients/Acme", dict["name"]);
        Assert.Equal("show", dict["messageListVisibility"]);
        Assert.Equal("labelShow", dict["labelListVisibility"]);

        var colorDict = Assert.IsType<Dictionary<string, object>>(dict["color"]);
        Assert.Equal("#ffffff", colorDict["textColor"]);
        Assert.Equal("#4a86e8", colorDict["backgroundColor"]);
    }

    [Fact]
    public void GmailLabel_ToDictionary_OmitsUnsetOptionalFields()
    {
        var label = new GmailLabel(name: "SimpleLabel");
        var dict = label.ToDictionary();

        Assert.Equal("SimpleLabel", dict["name"]);
        Assert.False(dict.ContainsKey("id"));
        Assert.False(dict.ContainsKey("messageListVisibility"));
        Assert.False(dict.ContainsKey("labelListVisibility"));
        Assert.False(dict.ContainsKey("color"));
    }

    [Fact]
    public void GmailLabel_Equals_ComparesAllFields()
    {
        var label1 = new GmailLabel("Invoices", "id1", "show", "labelShow", new LabelColor("white", "#4a86e8"));
        var label2 = new GmailLabel("Invoices", "id1", "show", "labelShow", new LabelColor("#ffffff", "#4a86e8"));
        var label3 = new GmailLabel("Receipts", "id1", "show", "labelShow", new LabelColor("white", "#4a86e8"));

        Assert.Equal(label1, label2);
        Assert.NotEqual(label1, label3);
    }

    #endregion

    #region Lua DSL Parsing & Loading Tests

    [Fact]
    public async Task LoadLabel_DeclarativeTable_ParsesCorrectly()
    {
        string lua = @"
            return label {
                id = 'lbl_001',
                name = 'Receipts',
                message_list_visibility = 'show',
                label_list_visibility = 'labelShowIfUnread',
                color = {
                    text = 'white',
                    background = '#4a86e8'
                }
            }
        ";

        var label = await _loader.LoadLabelFromScriptAsync(lua);

        Assert.Equal("lbl_001", label.Id);
        Assert.Equal("Receipts", label.Name);
        Assert.Equal("show", label.MessageListVisibility);
        Assert.Equal("labelShowIfUnread", label.LabelListVisibility);
        Assert.NotNull(label.Color);
        Assert.Equal("#ffffff", label.Color.TextColor);
        Assert.Equal("#4a86e8", label.Color.BackgroundColor);
    }

    [Fact]
    public async Task LoadLabel_ColorHelper_ParsesCorrectly()
    {
        string lua = @"
            return label {
                name = 'Urgent Action',
                color = color('black', '#fad165')
            }
        ";

        var label = await _loader.LoadLabelFromScriptAsync(lua);

        Assert.Equal("Urgent Action", label.Name);
        Assert.NotNull(label.Color);
        Assert.Equal("#000000", label.Color.TextColor);
        Assert.Equal("#fad165", label.Color.BackgroundColor);
    }

    [Fact]
    public async Task LoadLabel_FluentBuilder_ChainsMethodsCorrectly()
    {
        string lua = @"
            return label()
                :id('lbl_custom')
                :name('Security Triage')
                :show_in_message_list()
                :show_if_unread()
                :color('white', '#16a766')
                :build()
        ";

        var label = await _loader.LoadLabelFromScriptAsync(lua);

        Assert.Equal("lbl_custom", label.Id);
        Assert.Equal("Security Triage", label.Name);
        Assert.Equal("show", label.MessageListVisibility);
        Assert.Equal("labelShowIfUnread", label.LabelListVisibility);
        Assert.NotNull(label.Color);
        Assert.Equal("#ffffff", label.Color.TextColor);
        Assert.Equal("#16a766", label.Color.BackgroundColor);
    }

    [Fact]
    public async Task LoadLabel_FluentBuilder_HideMethods()
    {
        string lua = @"
            return label()
                :name('Archive Only')
                :hide_in_message_list()
                :hide_in_label_list()
                :build()
        ";

        var label = await _loader.LoadLabelFromScriptAsync(lua);

        Assert.Equal("Archive Only", label.Name);
        Assert.Equal("hide", label.MessageListVisibility);
        Assert.Equal("labelHide", label.LabelListVisibility);
        Assert.Null(label.Color);
    }

    [Fact]
    public async Task LoadLabels_ArrayOfLabels_LoadsMultiple()
    {
        string lua = @"
            return {
                label { name = 'Alpha', message_list_visibility = 'show' },
                label { name = 'Beta', message_list_visibility = 'hide' }
            }
        ";

        var labels = await _loader.LoadLabelsFromScriptAsync(lua);

        Assert.Equal(2, labels.Count);
        Assert.Equal("Alpha", labels[0].Name);
        Assert.Equal("show", labels[0].MessageListVisibility);
        Assert.Equal("Beta", labels[1].Name);
        Assert.Equal("hide", labels[1].MessageListVisibility);
    }

    [Fact]
    public async Task LoadConfiguration_FiltersAndLabelsTogether()
    {
        string lua = @"
            return {
                labels = {
                    label {
                        id = 'lbl_fin',
                        name = 'Finance/Invoices',
                        color = color('white', '#43d692')
                    }
                },
                filters = {
                    filter {
                        id = 'f_001',
                        query = From('billing@stripe.com'),
                        action = actions(archive, add_label('Finance/Invoices'))
                    }
                }
            }
        ";

        var filterLoader = new GmailFilterLoader();
        var config = await filterLoader.LoadConfigurationFromScriptAsync(lua);

        Assert.Single(config.Labels);
        Assert.Equal("Finance/Invoices", config.Labels[0].Name);
        Assert.Equal("#43d692", config.Labels[0].Color?.BackgroundColor);

        Assert.Single(config.Filters);
        Assert.Equal("f_001", config.Filters[0].Id);
        Assert.Equal("from:billing@stripe.com", config.Filters[0].ToGmailQuery());
    }

    [Fact]
    public async Task LoadLabel_InvalidColor_ThrowsWithClearMessage()
    {
        string lua = @"
            return label {
                name = 'Bad Color',
                color = color('red', '#4a86e8')
            }
        ";

        var ex = await Assert.ThrowsAsync<LabelValidationException>(() => _loader.LoadLabelFromScriptAsync(lua));
        Assert.Contains("Invalid color 'red'", ex.Message);
        Assert.Contains("predefined hex colors", ex.Message);
    }

    [Fact]
    public async Task LoadLabel_MissingOneColorComponent_Throws()
    {
        string lua = @"
            return label {
                name = 'Incomplete Color',
                color = { text = 'white' }
            }
        ";

        var ex = await Assert.ThrowsAsync<LabelValidationException>(() => _loader.LoadLabelFromScriptAsync(lua));
        Assert.Contains("Both textColor and backgroundColor must be provided", ex.Message);
    }

    [Fact]
    public async Task LoadLabel_InvalidVisibility_Throws()
    {
        string lua = @"
            return label {
                name = 'Bad Visibility',
                message_list_visibility = 'invalid_option'
            }
        ";

        var ex = await Assert.ThrowsAsync<LabelValidationException>(() => _loader.LoadLabelFromScriptAsync(lua));
        Assert.Contains("Invalid messageListVisibility 'invalid_option'", ex.Message);
    }

    [Fact]
    public async Task LoadLabel_ReservedSystemLabelName_Throws()
    {
        string lua = @"
            return label {
                name = 'INBOX'
            }
        ";

        var ex = await Assert.ThrowsAsync<LabelValidationException>(() => _loader.LoadLabelFromScriptAsync(lua));
        Assert.Contains("reserved Gmail system label", ex.Message);
    }

    #endregion
}
