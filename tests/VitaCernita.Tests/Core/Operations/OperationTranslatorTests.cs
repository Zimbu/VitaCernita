using System.Collections.Generic;
using VitaCernita.Core.Actions;
using VitaCernita.Core.AutoReply;
using VitaCernita.Core.Diffing;
using VitaCernita.Core.Diffing.AutoReply;
using VitaCernita.Core.Diffing.Filters;
using VitaCernita.Core.Diffing.Labels;
using VitaCernita.Core.Filters;
using VitaCernita.Core.Labels;
using VitaCernita.Core.Operations;
using VitaCernita.Core.Operations.AutoReply;
using VitaCernita.Core.Operations.Filters;
using VitaCernita.Core.Operations.Labels;
using VitaCernita.Core.Operations.Translators;
using VitaCernita.Core.Queries;
using Xunit;

namespace VitaCernita.Tests.Core.Operations;

public class OperationTranslatorTests
{
    [Fact]
    public void GenericResourceDiff_DecoupledFromApiDetails()
    {
        // Demonstrates pure model comparison without any API dependencies
        var fieldChanges = new List<FieldDiff>
        {
            new("color", null, new LabelColor("#ffffff", "#000000")),
            new("messageListVisibility", "show", "hide")
        };

        var genericDiff = new ResourceDiff<GmailLabel>(
            diffType: DiffKind.Modified,
            current: new GmailLabel("Work", "Label_1", messageListVisibility: "show"),
            desired: new GmailLabel("Work", "Label_1", messageListVisibility: "hide", color: new LabelColor("#ffffff", "#000000")),
            fieldDifferences: fieldChanges,
            identifier: "Work",
            id: "Label_1");

        Assert.Equal(DiffKind.Modified, genericDiff.DiffType);
        Assert.True(genericDiff.HasChanges);
        Assert.Equal("Work", genericDiff.Identifier);
        Assert.Equal("Label_1", genericDiff.Id);
        Assert.Equal(2, genericDiff.FieldDifferences.Count);
    }

    [Fact]
    public void LabelOperationTranslator_BuildsSparsePatchPayload()
    {
        var current = new GmailLabel("Projects", "Label_P", messageListVisibility: "show", labelListVisibility: "labelShow");
        var desired = new GmailLabel("Projects", "Label_P", messageListVisibility: "hide", labelListVisibility: "labelHide");

        var diff = LabelDiffer.Diff(current, desired);

        // Translator builds the patch payload
        var patch = LabelOperationTranslator.BuildPatchPayload(diff);
        Assert.Equal(2, patch.Count);
        Assert.Equal("hide", patch["messageListVisibility"]);
        Assert.Equal("labelHide", patch["labelListVisibility"]);
        Assert.False(patch.ContainsKey("name"));

        // Operation generation
        var op = LabelOperationTranslator.ToPatchOperation(diff);
        Assert.IsType<PatchLabelOperation>(op);
        Assert.Equal("Label_P", op.LabelId);
        Assert.Equal(2, op.Payload?.Count);
    }

    [Fact]
    public void FilterOperationTranslator_BuildsCreateAndDeleteOperations()
    {
        var current = new GmailFilter(
            id: "filter_old_1",
            query: new FieldCondition("from", "old@service.com"),
            action: new GmailAction().Archive());

        var desired = new GmailFilter(
            id: "filter_old_1",
            query: new FieldCondition("from", "new@service.com"),
            action: new GmailAction().Archive().Star());

        var diff = FilterDiffer.Diff(current, desired);

        Assert.Equal(DiffKind.Modified, diff.DiffType);
        Assert.Equal("filter_old_1", FilterOperationTranslator.ResolveDeleteId(diff));

        var updateOp = FilterOperationTranslator.ToUpdateOperation(diff);
        Assert.IsType<UpdateFilterOperation>(updateOp);
        Assert.Equal("filter_old_1", updateOp.FilterId);
        Assert.NotNull(updateOp.Payload);

        // Extension methods test
        Assert.NotNull(diff.GetCreatePayload());
        Assert.Equal("filter_old_1", diff.GetDeleteId());
    }

    [Fact]
    public void AutoReplyOperationTranslator_BuildsUpdateOperation()
    {
        var current = new AutoReply { EnableAutoReply = false };
        var desired = new AutoReply
        {
            EnableAutoReply = true,
            ResponseSubject = "Out of Office",
            ResponseBodyPlainText = "Traveling this week."
        };

        var diff = AutoReplyDiffer.Diff(current, desired);
        Assert.Equal(DiffKind.Added, diff.DiffType);

        var op = AutoReplyOperationTranslator.ToUpdateOperation(diff);
        Assert.IsType<UpdateAutoReplyOperation>(op);
        Assert.True(op.IsEnabling);
        Assert.NotNull(op.Payload);
        Assert.Equal("Out of Office", op.Payload?["responseSubject"]);

        // Extension method test
        var payload = diff.GetUpdatePayload();
        Assert.NotNull(payload);
        Assert.Equal("Out of Office", payload["responseSubject"]);
    }
}
