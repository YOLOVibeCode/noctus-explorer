using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// A horizontal tab strip for managing multiple tabs in a pane.
/// Each tab represents a directory location with its own ExplorerBrowserPane.
/// </summary>
public sealed class TabStripControl : UserControl
{
    private readonly List<TabInfo> _tabs = [];
    private int _activeIndex = -1;
    private readonly FlowLayoutPanel _tabPanel;
    private readonly Button _newTabButton;

    public event EventHandler<int>? TabActivated;
    public event EventHandler<int>? TabClosed;
    public event EventHandler? NewTabRequested;

    public int ActiveIndex => _activeIndex;
    public int TabCount => _tabs.Count;

    public TabStripControl()
    {
        Height = 28;
        Dock = DockStyle.Top;
        BackColor = SystemColors.Control;

        _tabPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(2, 2, 0, 0),
        };

        _newTabButton = new Button
        {
            Text = "+",
            Width = 24,
            Height = 22,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 0, 0),
        };
        _newTabButton.FlatAppearance.BorderSize = 0;
        _newTabButton.Click += (_, _) => NewTabRequested?.Invoke(this, EventArgs.Empty);

        _tabPanel.Controls.Add(_newTabButton);
        Controls.Add(_tabPanel);
    }

    public int AddTab(string title, int tabId)
    {
        var tab = new TabInfo { Id = tabId, Title = title };
        var btn = CreateTabButton(tab);
        tab.Button = btn;
        _tabs.Add(tab);

        // Insert before the "+" button
        _tabPanel.Controls.SetChildIndex(btn, _tabPanel.Controls.Count - 2);

        ActivateTab(_tabs.Count - 1);
        return _tabs.Count - 1;
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;

        var tab = _tabs[index];
        _tabPanel.Controls.Remove(tab.Button);
        tab.Button?.Dispose();
        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            _activeIndex = -1;
            return;
        }

        if (_activeIndex >= _tabs.Count)
            _activeIndex = _tabs.Count - 1;

        ActivateTab(_activeIndex);
    }

    public void ActivateTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;

        _activeIndex = index;
        for (int i = 0; i < _tabs.Count; i++)
        {
            var btn = _tabs[i].Button;
            if (btn is null) continue;
            btn.BackColor = i == _activeIndex ? SystemColors.Window : SystemColors.Control;
            btn.Font = new Font(btn.Font, i == _activeIndex ? FontStyle.Bold : FontStyle.Regular);
        }

        TabActivated?.Invoke(this, _tabs[index].Id);
    }

    public void UpdateTabTitle(int index, string title)
    {
        if (index < 0 || index >= _tabs.Count) return;
        _tabs[index].Title = title;
        if (_tabs[index].Button is { } btn)
        {
            // Truncate long titles
            btn.Text = title.Length > 20 ? title[..17] + "…  ✕" : title + "  ✕";
        }
    }

    public void ActivateNext()
    {
        if (_tabs.Count <= 1) return;
        ActivateTab((_activeIndex + 1) % _tabs.Count);
    }

    public void ActivatePrevious()
    {
        if (_tabs.Count <= 1) return;
        ActivateTab((_activeIndex - 1 + _tabs.Count) % _tabs.Count);
    }

    public void ActivateByNumber(int number)
    {
        // 1-based: Ctrl+1 = first tab, Ctrl+9 = last tab
        if (number == 9)
            ActivateTab(_tabs.Count - 1);
        else if (number >= 1 && number <= _tabs.Count)
            ActivateTab(number - 1);
    }

    private Button CreateTabButton(TabInfo tab)
    {
        var btn = new Button
        {
            Text = tab.Title.Length > 20 ? tab.Title[..17] + "…  ✕" : tab.Title + "  ✕",
            Height = 22,
            AutoSize = true,
            MinimumSize = new Size(80, 22),
            MaximumSize = new Size(200, 22),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Padding = new Padding(4, 0, 4, 0),
            Margin = new Padding(1, 0, 0, 0),
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = SystemColors.ControlDark;

        btn.MouseDown += (_, e) =>
        {
            var index = _tabs.FindIndex(t => t.Button == btn);
            if (index < 0) return;

            if (e.Button == MouseButtons.Middle)
            {
                // Middle-click to close
                TabClosed?.Invoke(this, _tabs[index].Id);
            }
            else
            {
                // Check if click was on the ✕ region (last ~16px)
                if (e.X > btn.Width - 20 && _tabs.Count > 1)
                {
                    TabClosed?.Invoke(this, _tabs[index].Id);
                }
                else
                {
                    ActivateTab(index);
                }
            }
        };

        _tabPanel.Controls.Add(btn);
        return btn;
    }

    private class TabInfo
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public Button? Button { get; set; }
    }
}
