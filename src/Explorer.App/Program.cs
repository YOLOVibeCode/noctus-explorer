using Microsoft.Extensions.DependencyInjection;
using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using NoctusExplorer.Core.ViewModels;
using NoctusExplorer.Shell.Windows;
using NoctusExplorer.UI.WinForms;

namespace NoctusExplorer.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Build DI container
        var services = new ServiceCollection();

        // Core services
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<CommandRegistry>();
        services.AddSingleton<KeyBindingResolver>();
        services.AddSingleton<BookmarkStore>();
        services.AddSingleton<CustomActionStore>();
        services.AddSingleton<CustomActionEngine>();
        services.AddSingleton<DropStackService>();
        services.AddSingleton<OperationsQueue>();

        // Shell adapter
        services.AddSingleton<IShellService, WinShellService>();
        services.AddSingleton<IFileOperations, WinFileOperations>();
        services.AddSingleton<IFileWatcher, WinFileWatcher>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // UI
        services.AddTransient<MainForm>();

        var provider = services.BuildServiceProvider();

        // Load settings
        var settings = provider.GetRequiredService<SettingsStore>();
        var settingsPath = ResolveSettingsPath();
        settings.Load(settingsPath);

        // Register default keybindings
        var keyResolver = provider.GetRequiredService<KeyBindingResolver>();
        RegisterDefaultBindings(keyResolver);

        // Register commands
        var commands = provider.GetRequiredService<CommandRegistry>();
        var vm = provider.GetRequiredService<MainViewModel>();
        RegisterCommands(commands, vm);

        // Restore session
        vm.RestoreSession();

        // Ensure left pane has at least one tab
        if (vm.LeftPane.Tabs.Count == 0)
        {
            var shell = provider.GetRequiredService<IShellService>();
            vm.LeftPane.AddTab(shell.GetSpecialFolder(SpecialFolder.Home));
        }

        // Run
        var form = provider.GetRequiredService<MainForm>();
        Application.Run(form);

        // Save on exit
        vm.SaveSession();
        settings.Save(settingsPath);
    }

    private static string ResolveSettingsPath()
    {
        // Portable mode: settings file next to executable
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var portablePath = Path.Combine(exeDir, "noctus-explorer.json");
        if (File.Exists(portablePath))
            return portablePath;

        // Installed mode
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "NoctusExplorer", "settings.json");
    }

    private static void RegisterDefaultBindings(KeyBindingResolver resolver)
    {
        resolver.LoadBindings(new Dictionary<string, KeyChord>
        {
            ["pane.copyToOther"] = new KeyChord("F5"),
            ["pane.moveToOther"] = new KeyChord("F6"),
            ["file.newFolder"] = new KeyChord("F7"),
            ["edit.delete"] = new KeyChord("F8"),
            ["edit.rename"] = new KeyChord("F2"),
            ["pane.switchActive"] = new KeyChord("TAB"),
            ["tools.commandPalette"] = new KeyChord("P", ctrl: true, shift: true),
            ["view.toggleHidden"] = new KeyChord("H", ctrl: true),
            ["tools.filter"] = new KeyChord("F", ctrl: true),
            ["edit.copyPath"] = new KeyChord("C", ctrl: true, shift: true),
        });
    }

    private static void RegisterCommands(CommandRegistry registry, MainViewModel vm)
    {
        registry.Register(new CommandDefinition
        {
            Id = "pane.switchActive",
            Name = "Switch Active Pane",
            Description = "Toggle focus between left and right pane",
            CanExecute = () => true,
            Execute = () => vm.SwitchActivePane()
        });

        registry.Register(new CommandDefinition
        {
            Id = "pane.copyToOther",
            Name = "Copy to Other Pane",
            Description = "Copy selected files to the inactive pane's directory",
            CanExecute = () => vm.ActivePane.ActiveTab?.Selection.Count > 0,
            Execute = () => vm.CopyToOtherPane()
        });

        registry.Register(new CommandDefinition
        {
            Id = "pane.moveToOther",
            Name = "Move to Other Pane",
            Description = "Move selected files to the inactive pane's directory",
            CanExecute = () => vm.ActivePane.ActiveTab?.Selection.Count > 0,
            Execute = () => vm.MoveToOtherPane()
        });

        registry.Register(new CommandDefinition
        {
            Id = "view.toggleSplit",
            Name = "Toggle Split Mode",
            Description = "Cycle between single, vertical, and horizontal split",
            CanExecute = () => true,
            Execute = () => vm.ToggleSplitMode()
        });
    }
}
