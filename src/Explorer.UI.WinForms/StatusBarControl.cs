namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Status bar showing item count, selection summary, and free disk space.
/// </summary>
public sealed class StatusBarControl : UserControl
{
    private readonly Label _itemCountLabel;
    private readonly Label _selectionLabel;
    private readonly Label _freeSpaceLabel;

    public StatusBarControl()
    {
        Height = 24;
        Dock = DockStyle.Bottom;
        BackColor = SystemColors.Control;
        Padding = new Padding(8, 0, 8, 0);

        _itemCountLabel = new Label
        {
            AutoSize = true,
            Location = new Point(8, 4),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
            Text = "0 items",
        };

        _selectionLabel = new Label
        {
            AutoSize = true,
            Location = new Point(120, 4),
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
        };

        _freeSpaceLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
        };

        Controls.Add(_itemCountLabel);
        Controls.Add(_selectionLabel);
        Controls.Add(_freeSpaceLabel);

        Resize += (_, _) => LayoutFreeSpace();
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

        LayoutFreeSpace();
    }

    private void LayoutFreeSpace()
    {
        _freeSpaceLabel.Location = new Point(Width - _freeSpaceLabel.Width - 12, 4);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
