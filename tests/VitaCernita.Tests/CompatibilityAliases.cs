// Global using aliases for tests to map legacy redundant diff types to the new unified diff models.
global using LabelDiff = VitaCernita.Core.Diffing.ResourceDiff<VitaCernita.Core.Labels.GmailLabel>;
global using FilterDiff = VitaCernita.Core.Diffing.ResourceDiff<VitaCernita.Core.Filters.GmailFilter>;
global using AutoReplyDiff = VitaCernita.Core.Diffing.ResourceDiff<VitaCernita.Core.AutoReply.AutoReply>;
global using LabelDiffType = VitaCernita.Core.Diffing.DiffKind;
global using FilterDiffType = VitaCernita.Core.Diffing.DiffKind;
global using AutoReplyDiffType = VitaCernita.Core.Diffing.DiffKind;
global using LabelFieldDiff = VitaCernita.Core.Diffing.FieldDiff;
global using FilterFieldDiff = VitaCernita.Core.Diffing.FieldDiff;
global using AutoReplyFieldDiff = VitaCernita.Core.Diffing.FieldDiff;
