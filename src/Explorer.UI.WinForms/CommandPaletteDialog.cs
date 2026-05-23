using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Ctrl+Shift+P command palette — a modal dialog with fuzzy search over all registered commands.
/// </summary>
public sealed class CommandPaletteDialog : Form
{
    private readonly CommandRegistry _registry;
    private readonly KeyBindingResolver _keyResolver;
    private readonly TextBox _searchBox;
    private readonly ListBox _resultsList;
    private List<CommandDefinition> _filtered = [];

    public CommandPaletteDialog(CommandRegistry registry, KeyBindingResolver keyResolver)
    {
        _registry = registry;
        _keyResolver = keyResolver;

        Text = "Command Palette";
        Width = 500;
        Height = 400;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;

        _searchBox = new TextBox
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 12f),
            PlaceholderText = "Type a command…",
            Height = 32,
        };
        _searchBox.TextChanged += (_, _) => FilterCommands();

        _resultsList = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f),
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 32,
        };
        _resultsList.DrawItem += DrawResultItem;
        _resultsList.DoubleClick += (_, _) => ExecuteSelected();
        _resultsList.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { ExecuteSelected(); e.Handled = true; }
        };

        Controls.Add(_resultsList);
        Controls.Add(_searchBox);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            if (e.KeyCode == Keys.Enter) ExecuteSelected();
        };

        FilterCommands();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _searchBox.Focus();
    }

    private void FilterCommands()
    {
        var query = _searchBox.Text.Trim();
        var all = _registry.GetAll().Where(c => c.CanExecute()).ToList();

        if (string.IsNullOrEmpty(query))
        {
            _filtered = all.OrderBy(c => c.Name).ToList();
        }
        else
        {
            // Simple fuzzy match: all query chars must appear in order
            _filtered = all
                .Select(c => (cmd: c, score: FuzzyScore(c.Name, query)))
                .Where(x => x.score >= 0)
                .OrderByDescending(x => x.score)
                .Select(x => x.cmd)
                .ToList();
        }

        _resultsList.Items.Clear();
        foreach (var cmd in _filtered)
            _resultsList.Items.Add(cmd);

        if (_resultsList.Items.Count > 0)
            _resultsList.SelectedIndex = 0;
    }

    private void ExecuteSelected()
    {
        if (_resultsList.SelectedItem is CommandDefinition cmd)
        {
            DialogResult = DialogResult.OK;
            Close();
            cmd.Execute();
        }
    }

    private void DrawResultItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _filtered.Count) return;

        e.DrawBackground();
        var cmd = _filtered[e.Index];
        var binding = _keyResolver.GetBinding(cmd.Id);

        // Command name
        var nameFont = new Font("Segoe UI", 10f);
        var descFont = new Font("Segoe UI", 8f);
        var bindingFont = new Font("Segoe UI", 8.5f);

        var nameColor = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : SystemColors.ControlText;
        var descColor = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : SystemColors.GrayText;

        TextRenderer.DrawText(e.Graphics, cmd.Name, nameFont,
            new Point(e.Bounds.Left + 8, e.Bounds.Top + 2), nameColor);

        if (!string.IsNullOrEmpty(cmd.Description))
        {
            TextRenderer.DrawText(e.Graphics, cmd.Description, descFont,
                new Point(e.Bounds.Left + 8, e.Bounds.Top + 17), descColor);
        }

        if (binding is not null)
        {
            var bindingText = binding.ToString();
            var bindingSize = TextRenderer.MeasureText(bindingText, bindingFont);
            TextRenderer.DrawText(e.Graphics, bindingText, bindingFont,
                new Point(e.Bounds.Right - bindingSize.Width - 8, e.Bounds.Top + 6), descColor);
        }

        e.DrawFocusRectangle();
    }

    /// <summary>
    /// Returns a fuzzy match score (higher = better), or -1 if no match.
    /// All query characters must appear in order in the target.
    /// </summary>
    private static int FuzzyScore(string target, string query)
    {
        int score = 0;
        int ti = 0;
        var targetLower = target.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();

        foreach (char qc in queryLower)
        {
            bool found = false;
            while (ti < targetLower.Length)
            {
                if (targetLower[ti] == qc)
                {
                    score += (ti == 0 || targetLower[ti - 1] == ' ' || targetLower[ti - 1] == '.') ? 10 : 1;
                    ti++;
                    found = true;
                    break;
                }
                ti++;
            }
            if (!found) return -1;
        }
        return score;
    }
}
