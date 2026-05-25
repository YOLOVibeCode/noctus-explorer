namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Status bar showing item count, selection summary, and free disk space.
/// </summary>
public sealed class StatusBarControl : UserControl
{
    private readonly Label _itemCountLabel;
    private readonly Label _selectionLabel;
    private readonly Label _freeSpaceLabel;

    private float _scale = 1f;

    public StatusBarControl()
    {
        Height = 26;
        Dock = DockStyle.Bottom;
        BackColor = SystemColors.Control;
        Padding = new Padding(10, 0, 10, 0);

        _itemCountLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SystemColors.ControlText,
            Text = "0 items",
        };

        _selectionLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SystemColors.GrayText,
        };

        _freeSpaceLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", 9f),
            ForeColor = SystemColors.GrayText,
        };

        Controls.Add(_itemCountLabel);
        Controls.Add(_selectionLabel);
        Controls.Add(_freeSpaceLabel);

        Resize += (_, _) => LayoutLabels();
    }

    public void ApplyScale(float scale)
    {
        _scale = scale;
        Padding = new Padding((int)(10 * scale), 0, (int)(10 * scale), 0);
        LayoutLabels();
    }

    public void Update(int totalItems, int selectedCount, long selectedSize, string? drivePath)
    {
        _itemCountLabel.Text = $"{totalItems} items";

        _selectionLabel.Text = selectedCount > 0
            ? $"{selectedCount} selected ({FormatSize(selectedSize)})"
            : "";

        if (drivePath is not null)
        {
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(drivePath) ?? drivePath);
                _freeSpaceLabel.Text = $"{FormatSize(driveInfo.AvailableFreeSpace)} free";
            }
            catch
            {
                _freeSpaceLabel.Text = "";
            }
        }

        LayoutLabels();
    }

    private void LayoutLabels()
    {
        // Vertically center all labels in the bar
        var midY = Math.Max(0, (Height - _itemCountLabel.Height) / 2);
        _itemCountLabel.Location = new Point((int)(12 * _scale), midY);
        _selectionLabel.Location = new Point(
            _itemCountLabel.Right + (int)(18 * _scale),
            midY);
        _freeSpaceLabel.Location = new Point(
            Width - _freeSpaceLabel.Width - (int)(12 * _scale),
            midY);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
