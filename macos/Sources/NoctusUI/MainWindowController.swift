import AppKit
import NoctusCore
import NoctusShellAdapter

/// The main window with toolbar, address bar, file pane, and status bar.
public class MainWindowController: NSWindowController {

    private let shellService: MacShellService
    private let fileWatcher: MacFileWatcher
    private var pane: FileListPane!
    private var addressField: NSTextField!
    private var statusLabel: NSTextField!
    private var backButton: NSButton!
    private var forwardButton: NSButton!
    private var upButton: NSButton!

    public init() {
        self.shellService = MacShellService()
        self.fileWatcher = MacFileWatcher()

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 900, height: 600),
            styleMask: [.titled, .closable, .resizable, .miniaturizable],
            backing: .buffered,
            defer: false
        )
        window.title = "Noctus Explorer"
        window.center()
        window.setFrameAutosaveName("NoctusMainWindow")

        super.init(window: window)

        setupUI()
        setupToolbar()
    }

    required init?(coder: NSCoder) { fatalError() }

    private func setupUI() {
        guard let window = window else { return }

        let container = NSView()
        container.translatesAutoresizingMaskIntoConstraints = false

        // Address bar
        let addressBar = NSView()
        addressBar.translatesAutoresizingMaskIntoConstraints = false
        addressBar.wantsLayer = true
        addressBar.layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor

        backButton = makeToolButton(image: NSImage(systemSymbolName: "chevron.left", accessibilityDescription: "Back")!, action: #selector(goBack))
        forwardButton = makeToolButton(image: NSImage(systemSymbolName: "chevron.right", accessibilityDescription: "Forward")!, action: #selector(goForward))
        upButton = makeToolButton(image: NSImage(systemSymbolName: "chevron.up", accessibilityDescription: "Up")!, action: #selector(goUp))

        addressField = NSTextField()
        addressField.translatesAutoresizingMaskIntoConstraints = false
        addressField.placeholderString = "Enter path…"
        addressField.font = .systemFont(ofSize: 13)
        addressField.target = self
        addressField.action = #selector(addressBarSubmitted)

        addressBar.addSubview(backButton)
        addressBar.addSubview(forwardButton)
        addressBar.addSubview(upButton)
        addressBar.addSubview(addressField)

        NSLayoutConstraint.activate([
            backButton.leadingAnchor.constraint(equalTo: addressBar.leadingAnchor, constant: 8),
            backButton.centerYAnchor.constraint(equalTo: addressBar.centerYAnchor),
            backButton.widthAnchor.constraint(equalToConstant: 28),
            forwardButton.leadingAnchor.constraint(equalTo: backButton.trailingAnchor, constant: 2),
            forwardButton.centerYAnchor.constraint(equalTo: addressBar.centerYAnchor),
            forwardButton.widthAnchor.constraint(equalToConstant: 28),
            upButton.leadingAnchor.constraint(equalTo: forwardButton.trailingAnchor, constant: 2),
            upButton.centerYAnchor.constraint(equalTo: addressBar.centerYAnchor),
            upButton.widthAnchor.constraint(equalToConstant: 28),
            addressField.leadingAnchor.constraint(equalTo: upButton.trailingAnchor, constant: 8),
            addressField.trailingAnchor.constraint(equalTo: addressBar.trailingAnchor, constant: -8),
            addressField.centerYAnchor.constraint(equalTo: addressBar.centerYAnchor),
            addressBar.heightAnchor.constraint(equalToConstant: 36),
        ])

        // File pane
        let homePath = shellService.specialFolder(.home)
        pane = FileListPane(path: homePath, shellService: shellService, fileWatcher: fileWatcher)
        pane.isActivePane = true
        pane.onNavigate = { [weak self] path in
            self?.updateAddressBar(path)
            self?.updateStatusBar()
            self?.updateNavButtons()
        }
        pane.onSelectionChanged = { [weak self] _ in
            self?.updateStatusBar()
        }

        let paneView = pane.view
        paneView.translatesAutoresizingMaskIntoConstraints = false

        // Status bar
        let statusBar = NSView()
        statusBar.translatesAutoresizingMaskIntoConstraints = false
        statusBar.wantsLayer = true
        statusBar.layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor

        statusLabel = NSTextField(labelWithString: "")
        statusLabel.translatesAutoresizingMaskIntoConstraints = false
        statusLabel.font = .systemFont(ofSize: 11)
        statusLabel.textColor = .secondaryLabelColor
        statusBar.addSubview(statusLabel)

        NSLayoutConstraint.activate([
            statusLabel.leadingAnchor.constraint(equalTo: statusBar.leadingAnchor, constant: 10),
            statusLabel.centerYAnchor.constraint(equalTo: statusBar.centerYAnchor),
            statusBar.heightAnchor.constraint(equalToConstant: 24),
        ])

        // Layout
        container.addSubview(addressBar)
        container.addSubview(paneView)
        container.addSubview(statusBar)

        NSLayoutConstraint.activate([
            addressBar.topAnchor.constraint(equalTo: container.topAnchor),
            addressBar.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            addressBar.trailingAnchor.constraint(equalTo: container.trailingAnchor),

            paneView.topAnchor.constraint(equalTo: addressBar.bottomAnchor),
            paneView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            paneView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            paneView.bottomAnchor.constraint(equalTo: statusBar.topAnchor),

            statusBar.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            statusBar.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            statusBar.bottomAnchor.constraint(equalTo: container.bottomAnchor),
        ])

        window.contentView = container
        updateAddressBar(homePath)
        updateStatusBar()
    }

    private func setupToolbar() {
        // Menu bar
        let mainMenu = NSMenu()

        let appMenuItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "About Noctus Explorer", action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "Quit Noctus Explorer", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appMenuItem.submenu = appMenu
        mainMenu.addItem(appMenuItem)

        let goMenuItem = NSMenuItem()
        let goMenu = NSMenu(title: "Go")
        goMenu.addItem(withTitle: "Back", action: #selector(goBack), keyEquivalent: "[")
        goMenu.items.last?.keyEquivalentModifierMask = .command
        goMenu.addItem(withTitle: "Forward", action: #selector(goForward), keyEquivalent: "]")
        goMenu.items.last?.keyEquivalentModifierMask = .command
        goMenu.addItem(withTitle: "Enclosing Folder", action: #selector(goUp), keyEquivalent: String(Character(UnicodeScalar(NSUpArrowFunctionKey)!)))
        goMenu.items.last?.keyEquivalentModifierMask = .command
        goMenu.addItem(.separator())
        goMenu.addItem(withTitle: "Home", action: #selector(goHome), keyEquivalent: "H")
        goMenu.items.last?.keyEquivalentModifierMask = [.command, .shift]
        goMenu.addItem(withTitle: "Desktop", action: #selector(goDesktop), keyEquivalent: "D")
        goMenu.items.last?.keyEquivalentModifierMask = [.command, .shift]
        goMenu.addItem(withTitle: "Downloads", action: #selector(goDownloads), keyEquivalent: "L")
        goMenu.items.last?.keyEquivalentModifierMask = [.command, .shift]
        goMenu.addItem(.separator())
        goMenu.addItem(withTitle: "Go to Folder…", action: #selector(focusAddressBar), keyEquivalent: "l")
        goMenu.items.last?.keyEquivalentModifierMask = .command
        goMenuItem.submenu = goMenu
        mainMenu.addItem(goMenuItem)

        let viewMenuItem = NSMenuItem()
        let viewMenu = NSMenu(title: "View")
        viewMenu.addItem(withTitle: "Refresh", action: #selector(refreshPane), keyEquivalent: "r")
        viewMenu.items.last?.keyEquivalentModifierMask = .command
        viewMenuItem.submenu = viewMenu
        mainMenu.addItem(viewMenuItem)

        NSApp.mainMenu = mainMenu
    }

    // MARK: - Actions

    @objc private func goBack() { pane.goBack() }
    @objc private func goForward() { pane.goForward() }
    @objc private func goUp() { pane.goUp() }
    @objc private func refreshPane() { pane.refresh() }

    @objc private func goHome() { pane.navigate(to: shellService.specialFolder(.home)) }
    @objc private func goDesktop() { pane.navigate(to: shellService.specialFolder(.desktop)) }
    @objc private func goDownloads() { pane.navigate(to: shellService.specialFolder(.downloads)) }

    @objc private func focusAddressBar() {
        window?.makeFirstResponder(addressField)
        addressField.selectText(nil)
    }

    @objc private func addressBarSubmitted() {
        let text = addressField.stringValue
        guard !text.isEmpty else { return }
        let expanded = NSString(string: text).expandingTildeInPath
        var isDir: ObjCBool = false
        if FileManager.default.fileExists(atPath: expanded, isDirectory: &isDir), isDir.boolValue {
            pane.navigate(to: PathRef(expanded, isDirectory: true))
        } else {
            NSSound.beep()
        }
    }

    // MARK: - UI Updates

    private func updateAddressBar(_ path: PathRef) {
        addressField.stringValue = path.fullPath
    }

    private func updateStatusBar() {
        statusLabel.stringValue = pane.statusText
    }

    private func updateNavButtons() {
        backButton.isEnabled = pane.navigation.canGoBack
        forwardButton.isEnabled = pane.navigation.canGoForward
    }

    private func makeToolButton(image: NSImage, action: Selector) -> NSButton {
        let btn = NSButton(image: image, target: self, action: action)
        btn.translatesAutoresizingMaskIntoConstraints = false
        btn.bezelStyle = .accessoryBarAction
        btn.isBordered = false
        return btn
    }
}
