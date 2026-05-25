using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services.BatchRename;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Multi-step batch rename dialog. Five chainable operations with live preview.
/// Each operation row has an enable checkbox + inline config; the preview list
/// refreshes on any change. Apply runs IFileOperations.Rename per entry,
/// skipping any flagged invalid by the engine.
/// </summary>
public sealed class BatchRenameDialog : Form
{
    private readonly IReadOnlyList<FileEntry> _entries;
    private readonly IFileOperations _fileOps;
    private readonly BatchRenameEngine _engine = new();
    private readonly OperationRows _rows;
    private readonly ListView _previewList;
    private readonly Label _summaryLabel;
    private readonly Button _applyButton;

    public BatchRenameDialog(IReadOnlyList<FileEntry> entries, IFileOperations fileOps)
    {
        _entries = entries;
        _fileOps = fileOps;

        Text = $"Batch Rename — {entries.Count} item{(entries.Count == 1 ? "" : "s")}";
        Width = 720;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(600, 480);
        Font = new Font("Segoe UI", 9f);

        _rows = new OperationRows();
        _rows.Changed += (_, _) => RefreshPreview();

        var opsHeader = new Label
        {
            Text = "Operations (applied in order, top to bottom)",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(8, 4, 0, 0),
        };

        var previewHeader = new Label
        {
            Text = "Preview",
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(8, 4, 0, 0),
        };

        _previewList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Consolas", 9f),
        };
        _previewList.Columns.Add("Original", 280);
        _previewList.Columns.Add("New Name", 280);
        _previewList.Columns.Add("Status", 100);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(8),
        };

        _summaryLabel = new Label
        {
            Dock = DockStyle.Left,
            Width = 320,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
        };

        var cancel = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Right,
            Width = 90,
            DialogResult = DialogResult.Cancel,
        };
        _applyButton = new Button
        {
            Text = "Apply",
            Dock = DockStyle.Right,
            Width = 90,
        };
        _applyButton.Click += (_, _) => Apply();

        buttonPanel.Controls.Add(cancel);
        buttonPanel.Controls.Add(_applyButton);
        buttonPanel.Controls.Add(_summaryLabel);

        var previewContainer = new Panel { Dock = DockStyle.Fill };
        previewContainer.Controls.Add(_previewList);
        previewContainer.Controls.Add(previewHeader);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 260,
        };
        split.Panel1.Controls.Add(_rows);
        split.Panel1.Controls.Add(opsHeader);
        split.Panel2.Controls.Add(previewContainer);

        Controls.Add(split);
        Controls.Add(buttonPanel);

        CancelButton = cancel;
        AcceptButton = _applyButton;

        RefreshPreview();
    }

    private IReadOnlyList<IRenameOperation> CurrentOperations() => _rows.BuildOperations();

    private void RefreshPreview()
    {
        var ops = CurrentOperations();
        var previews = _engine.Preview(_entries, ops);

        _previewList.BeginUpdate();
        _previewList.Items.Clear();

        int valid = 0, invalid = 0, unchanged = 0;
        foreach (var p in previews)
        {
            var item = new ListViewItem(p.Original.Name);
            item.SubItems.Add(p.NewName);

            string status;
            if (!p.IsValid)
            {
                status = p.ValidationError ?? "Invalid";
                item.ForeColor = Color.Firebrick;
                invalid++;
            }
            else if (p.ValidationError == "Unchanged")
            {
                status = "Unchanged";
                item.ForeColor = SystemColors.GrayText;
                unchanged++;
            }
            else
            {
                status = "Ready";
                valid++;
            }
            item.SubItems.Add(status);
            _previewList.Items.Add(item);
        }

        _previewList.EndUpdate();

        _summaryLabel.Text = $"{valid} to rename, {unchanged} unchanged, {invalid} invalid";
        _applyButton.Enabled = valid > 0;
    }

    private void Apply()
    {
        var ops = CurrentOperations();
        var previews = _engine.Preview(_entries, ops);

        int succeeded = 0, failed = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var p in previews)
        {
            if (!p.IsValid || p.ValidationError == "Unchanged")
            {
                skipped++;
                continue;
            }

            try
            {
                using var handle = _fileOps.Rename(p.Original.Path, p.NewName);
                if (handle.Status == OperationStatus.Failed)
                {
                    failed++;
                    errors.Add($"{p.Original.Name}: rename failed");
                }
                else
                {
                    succeeded++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{p.Original.Name}: {ex.Message}");
            }
        }

        if (failed > 0)
        {
            var msg = $"Renamed {succeeded}, failed {failed}, skipped {skipped}.\n\n"
                    + string.Join("\n", errors.Take(8));
            MessageBox.Show(this, msg, "Batch Rename — partial completion",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // ----------------------------------------------------------
    // Operation rows: one row per operation type, each with enable
    // checkbox + inline config controls. Raises Changed on any edit.
    // ----------------------------------------------------------

    private sealed class OperationRows : Panel
    {
        public event EventHandler? Changed;

        // Find/Replace
        private readonly CheckBox _findReplaceEnable;
        private readonly TextBox _findText;
        private readonly TextBox _replaceText;
        private readonly CheckBox _useRegex;
        private readonly CheckBox _caseSensitive;

        // Insert Text
        private readonly CheckBox _insertEnable;
        private readonly TextBox _insertText;
        private readonly ComboBox _insertPosition;

        // Change Case
        private readonly CheckBox _caseEnable;
        private readonly ComboBox _caseMode;

        // Number Sequence
        private readonly CheckBox _numberEnable;
        private readonly NumericUpDown _numberStart;
        private readonly NumericUpDown _numberStep;
        private readonly NumericUpDown _numberPadding;
        private readonly CheckBox _numberPrepend;

        // Date Stamp
        private readonly CheckBox _dateEnable;
        private readonly TextBox _dateFormat;
        private readonly ComboBox _dateField;
        private readonly ComboBox _datePosition;

        public OperationRows()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            Padding = new Padding(8, 28, 8, 8);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
            };

            // Find/Replace row
            _findReplaceEnable = MakeEnable("Find && Replace");
            _findText = MakeTextBox(placeholder: "find");
            _replaceText = MakeTextBox(placeholder: "replace with");
            _useRegex = MakeCheckBox("Regex");
            _caseSensitive = MakeCheckBox("Case-sensitive");
            table.Controls.Add(BuildRow(_findReplaceEnable,
                _findText, ArrowLabel(), _replaceText, _useRegex, _caseSensitive));

            // Insert Text row
            _insertEnable = MakeEnable("Insert Text");
            _insertText = MakeTextBox(placeholder: "text");
            _insertPosition = MakeCombo(["At Start", "At End", "Before Extension"]);
            table.Controls.Add(BuildRow(_insertEnable, _insertText, _insertPosition));

            // Change Case row
            _caseEnable = MakeEnable("Change Case");
            _caseMode = MakeCombo(["UPPER", "lower", "Title Case", "Sentence case"]);
            table.Controls.Add(BuildRow(_caseEnable, _caseMode));

            // Number Sequence row
            _numberEnable = MakeEnable("Number Sequence");
            _numberStart = MakeNumeric(1, -999999, 999999);
            _numberStep = MakeNumeric(1, 1, 9999);
            _numberPadding = MakeNumeric(3, 0, 12);
            _numberPrepend = MakeCheckBox("Prepend");
            table.Controls.Add(BuildRow(_numberEnable,
                LabeledNumeric("Start", _numberStart),
                LabeledNumeric("Step", _numberStep),
                LabeledNumeric("Pad", _numberPadding),
                _numberPrepend));

            // Date Stamp row
            _dateEnable = MakeEnable("Date Stamp");
            _dateFormat = MakeTextBox(placeholder: "yyyy-MM-dd");
            _dateFormat.Text = "yyyy-MM-dd";
            _dateField = MakeCombo(["Modified", "Created"]);
            _datePosition = MakeCombo(["At Start", "At End", "Before Extension"]);
            table.Controls.Add(BuildRow(_dateEnable, _dateFormat, _dateField, _datePosition));

            Controls.Add(table);
        }

        private static Label ArrowLabel() => new()
        {
            Text = "→",
            AutoSize = true,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(4, 4, 4, 0),
        };

        private CheckBox MakeEnable(string label)
        {
            var cb = new CheckBox
            {
                Text = label,
                AutoSize = true,
                Margin = new Padding(0, 6, 8, 0),
                Width = 150,
                MinimumSize = new Size(150, 24),
            };
            cb.CheckedChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            return cb;
        }

        private TextBox MakeTextBox(string placeholder)
        {
            var t = new TextBox
            {
                Width = 130,
                PlaceholderText = placeholder,
                Margin = new Padding(0, 4, 4, 0),
            };
            t.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            return t;
        }

        private CheckBox MakeCheckBox(string label)
        {
            var cb = new CheckBox
            {
                Text = label,
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0),
            };
            cb.CheckedChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            return cb;
        }

        private ComboBox MakeCombo(string[] items)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Margin = new Padding(0, 4, 4, 0),
            };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            c.SelectedIndexChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            return c;
        }

        private NumericUpDown MakeNumeric(int value, int min, int max)
        {
            var n = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Width = 60,
                Margin = new Padding(0, 4, 4, 0),
            };
            n.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            return n;
        }

        private static Control LabeledNumeric(string label, NumericUpDown nud)
        {
            var p = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            p.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 2, 0) });
            p.Controls.Add(nud);
            return p;
        }

        private static FlowLayoutPanel BuildRow(params Control[] controls)
        {
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2),
            };
            flow.Controls.AddRange(controls);
            return flow;
        }

        public IReadOnlyList<IRenameOperation> BuildOperations()
        {
            var ops = new List<IRenameOperation>();
            if (_findReplaceEnable.Checked)
                ops.Add(new FindReplaceOperation(_findText.Text, _replaceText.Text,
                    _useRegex.Checked, _caseSensitive.Checked));

            if (_insertEnable.Checked)
            {
                var pos = _insertPosition.SelectedIndex switch
                {
                    0 => InsertPosition.AtStart,
                    1 => InsertPosition.AtEnd,
                    2 => InsertPosition.BeforeExtension,
                    _ => InsertPosition.AtStart,
                };
                ops.Add(new InsertTextOperation(_insertText.Text, pos));
            }

            if (_caseEnable.Checked)
            {
                var mode = _caseMode.SelectedIndex switch
                {
                    0 => CaseMode.Upper,
                    1 => CaseMode.Lower,
                    2 => CaseMode.Title,
                    3 => CaseMode.Sentence,
                    _ => CaseMode.Upper,
                };
                ops.Add(new ChangeCaseOperation(mode));
            }

            if (_numberEnable.Checked)
                ops.Add(new NumberSequenceOperation(
                    start: (int)_numberStart.Value,
                    step: (int)_numberStep.Value,
                    padding: (int)_numberPadding.Value,
                    prepend: _numberPrepend.Checked));

            if (_dateEnable.Checked)
            {
                var field = _dateField.SelectedIndex == 1 ? DateField.Created : DateField.Modified;
                var pos = _datePosition.SelectedIndex switch
                {
                    0 => InsertPosition.AtStart,
                    1 => InsertPosition.AtEnd,
                    2 => InsertPosition.BeforeExtension,
                    _ => InsertPosition.AtStart,
                };
                ops.Add(new DateStampOperation(_dateFormat.Text, field, pos));
            }

            return ops;
        }
    }
}
