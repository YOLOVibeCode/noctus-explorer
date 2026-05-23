import AppKit
import NoctusCore
import NoctusShellAdapter

/// The main window with toolbar, address bar, dual-pane split, and status bar.
public class MainWindowController: NSWindowController {

    private let shellService: MacShellService
    private let fileOps: MacFileOperations
    private let fileWatcher: MacFileWatcher

    private var leftPane: FileListPane!
    private var rightPane: FileListPane!
    private var splitView: NSSplitView!

    private var addressField: NSTextField!
    private var leftStatusLabel: NSTextField!
    private var rightStatusLabel: NSTextField!
    private var backButton: NSButton!
    private var forwardButton: NSButton!
    private var upButton: NSButton!

    private var activePane: FileListPane { leftPane.isActivePane ? leftPane : rightPane }
    private var inactivePane: FileListPane { leftPane.isActivePane ? rightPane : leftPane }

    public init() {
        self.shellService = MacShellService()
        self.fileOps = MacFileOperations()
        self.fileWatcher = MacFileWatcher()

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 1200, height: 700),
            styleMask: [.titled, .closable, .resizable, .miniaturizable],
            backing: .buffered,
            defer: false
        )
        window.title = "Noctus Explorer"
        window.center()
        window.setFrameAutosaveName("NoctusMainWindow")

        super.init(window: window)

        setupUI()
        setupMenuBar()
    }

    required init?(coder: NSCoder) { fatalError() }

    private func setupUI() {
        guard let window = window else { return }

        let container = NSView()
        container.translatesAutoresizingMaskIntoConstraints = false

        // === Address bar ===
        let addressBar = NSView()
        addressBar.translatesAutoresizingMaskIntoConstraints = false
        addressBar.wantsLayer = true
        addressBar.layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor

        backButton = makeToolButton("chevron.left", action: #selector(goBack))
        forwardButton = makeToolButton("chevron.right", action: #selector(goForward))
        upButton = makeToolButton("chevron.up", action: #selector(goUp))

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

        // === Dual pane split view ===
        let homePath = shellService.specialFolder(.home)
        let desktopPath = shellService.specialFolder(.desktop)

        leftPane = FileListPane(path: homePath, shellService: shellService, fileWatcher: fileWatcher)
        leftPane.isActivePane = true
        leftPane.onNavigate = { [weak self] path in self?.onActiveNavigate(path) }
        leftPane.onSelectionChanged = { [weak self] _ in self?.updateStatusBars() }
        leftPane.onActivated = { [weak self] in self?.setActive(.left) }

        rightPane = FileListPane(path: desktopPath, shellService: shellService, fileWatcher: fileWatcher)
        rightPane.isActivePane = false
        rightPane.onNavigate = { [weak self] path in self?.onActiveNavigate(path) }
        rightPane.onSelectionChanged = { [weak self] _ in self?.updateStatusBars() }
        rightPane.onActivated = { [weak self] in self?.setActive(.right) }

        splitView = NSSplitView()
        splitView.isVertical = true
        splitView.dividerStyle = .thin
        splitView.translatesAutoresizingMaskIntoConstraints = false
        splitView.addSubview(leftPane.view)
        splitView.addSubview(rightPane.view)
        splitView.adjustSubviews()

        // === Status bar (split for each pane) ===
        let statusBar = NSView()
        statusBar.translatesAutoresizingMaskIntoConstraints = false
        statusBar.wantsLayer = true
        statusBar.layer?.backgroundColor = NSColor.windowBackgroundColor.cgColor

        leftStatusLabel = NSTextField(labelWithString: "")
        leftStatusLabel.translatesAutoresizingMaskIntoConstraints = false
        leftStatusLabel.font = .systemFont(ofSize: 11)
        leftStatusLabel.textColor = .secondaryLabelColor

        rightStatusLabel = NSTextField(labelWithString: "")
        rightStatusLabel.translatesAutoresizingMaskIntoConstraints = false
        rightStatusLabel.font = .systemFont(ofSize: 11)
        rightStatusLabel.textColor = .secondaryLabelColor

        let statusDivider = NSView()
        statusDivider.translatesAutoresizingMaskIntoConstraints = false
        statusDivider.wantsLayer = true
        statusDivider.layer?.backgroundColor = NSColor.separatorColor.cgColor

        statusBar.addSubview(leftStatusLabel)
        statusBar.addSubview(statusDivider)
        statusBar.addSubview(rightStatusLabel)

        NSLayoutConstraint.activate([
            leftStatusLabel.leadingAnchor.constraint(equalTo: statusBar.leadingAnchor, constant: 10),
            leftStatusLabel.centerYAnchor.constraint(equalTo: statusBar.centerYAnchor),
            statusDivider.centerXAnchor.constraint(equalTo: statusBar.centerXAnchor),
            statusDivider.topAnchor.constraint(equalTo: statusBar.topAnchor, constant: 4),
            statusDivider.bottomAnchor.constraint(equalTo: statusBar.bottomAnchor, constant: -4),
            statusDivider.widthAnchor.constraint(equalToConstant: 1),
            rightStatusLabel.leadingAnchor.constraint(equalTo: statusDivider.trailingAnchor, constant: 10),
            rightStatusLabel.centerYAnchor.constraint(equalTo: statusBar.centerYAnchor),
            statusBar.heightAnchor.constraint(equalToConstant: 24),
        ])

        // === Layout ===
        container.addSubview(addressBar)
        container.addSubview(splitView)
        container.addSubview(statusBar)

        NSLayoutConstraint.activate([
            addressBar.topAnchor.constraint(equalTo: container.topAnchor),
            addressBar.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            addressBar.trailingAnchor.constraint(equalTo: container.trailingAnchor),

            splitView.topAnchor.constraint(equalTo: addressBar.bottomAnchor),
            splitView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            splitView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            splitView.bottomAnchor.constraint(equalTo: statusBar.topAnchor),

            statusBar.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            statusBar.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            statusBar.bottomAnchor.constraint(equalTo: container.bottomAnchor),
        ])

        window.contentView = container

        updateAddressBar(homePath)
        updateStatusBars()
        updateNavButtons()
    }

    private func setupMenuBar() {
        let mainMenu = NSMenu()

        // App menu
        let appMenuItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "About Noctus Explorer", action: #selector(NSApplication.orderFrontStandardAboutPanel(_:)), keyEquivalent: "")
        appMenu.addItem(.separator())
        appMenu.addItem(withTitle: "Quit Noctus Explorer", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appMenuItem.submenu = appMenu
        mainMenu.addItem(appMenuItem)

        // Edit menu (for copy/paste to work)
        let editMenuItem = NSMenuItem()
        let editMenu = NSMenu(title: "Edit")
        editMenu.addItem(withTitle: "Copy", action: #selector(NSText.copy(_:)), keyEquivalent: "c")
        editMenu.addItem(withTitle: "Paste", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        editMenu.addItem(withTitle: "Select All", action: #selector(NSText.selectAll(_:)), keyEquivalent: "a")
        editMenuItem.submenu = editMenu
        mainMenu.addItem(editMenuItem)

        // View menu
        let viewMenuItem = NSMenuItem()
        let viewMenu = NSMenu(title: "View")
        viewMenu.addItem(withTitle: "Refresh", action: #selector(refreshPane), keyEquivalent: "r")
        viewMenu.items.last?.keyEquivalentModifierMask = .command
        viewMenuItem.submenu = viewMenu
        mainMenu.addItem(viewMenuItem)

        // Go menu
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

        // Tools menu
        let toolsMenuItem = NSMenuItem()
        let toolsMenu = NSMenu(title: "Tools")
        toolsMenu.addItem(withTitle: "Copy to Other Pane", action: #selector(copyToOtherPane), keyEquivalent: "")
        toolsMenu.items.last?.keyEquivalent = String(Character(UnicodeScalar(NSF5FunctionKey)!))
        toolsMenu.items.last?.keyEquivalentModifierMask = []
        toolsMenu.addItem(withTitle: "Move to Other Pane", action: #selector(moveToOtherPane), keyEquivalent: "")
        toolsMenu.items.last?.keyEquivalent = String(Character(UnicodeScalar(NSF6FunctionKey)!))
        toolsMenu.items.last?.keyEquivalentModifierMask = []
        toolsMenu.addItem(.separator())
        toolsMenu.addItem(withTitle: "Switch Pane", action: #selector(switchActivePane), keyEquivalent: "\t")
        toolsMenu.items.last?.keyEquivalentModifierMask = []
        toolsMenuItem.submenu = toolsMenu
        mainMenu.addItem(toolsMenuItem)

        NSApp.mainMenu = mainMenu
    }

    // MARK: - Pane Management

    private func setActive(_ side: PaneSide) {
        leftPane.isActivePane = (side == .left)
        rightPane.isActivePane = (side == .right)
        updateAddressBar(activePane.currentPath)
        updateNavButtons()
    }

    @objc private func switchActivePane() {
        if leftPane.isActivePane {
            setActive(.right)
        } else {
            setActive(.left)
        }
    }

    private func onActiveNavigate(_ path: PathRef) {
        updateAddressBar(path)
        updateStatusBars()
        updateNavButtons()
    }

    // MARK: - Cross-Pane Operations

    @objc private func copyToOtherPane() {
        let selection = activePane.selectedEntries
        guard !selection.isEmpty else { NSSound.beep(); return }
        let sources = selection.map(\.path)
        let dest = inactivePane.currentPath
        Task {
            do {
                try await fileOps.copy(sources, to: dest)
                inactivePane.refresh()
            } catch {
                let alert = NSAlert(error: error)
                alert.runModal()
            }
        }
    }

    @objc private func moveToOtherPane() {
        let selection = activePane.selectedEntries
        guard !selection.isEmpty else { NSSound.beep(); return }
        let sources = selection.map(\.path)
        let dest = inactivePane.currentPath
        Task {
            do {
                try await fileOps.move(sources, to: dest)
                activePane.refresh()
                inactivePane.refresh()
            } catch {
                let alert = NSAlert(error: error)
                alert.runModal()
            }
        }
    }

    // MARK: - Navigation Actions

    @objc private func goBack() { activePane.goBack(); updateNavButtons() }
    @objc private func goForward() { activePane.goForward(); updateNavButtons() }
    @objc private func goUp() { activePane.goUp() }
    @objc private func refreshPane() { activePane.refresh() }

    @objc private func goHome() { activePane.navigate(to: shellService.specialFolder(.home)) }
    @objc private func goDesktop() { activePane.navigate(to: shellService.specialFolder(.desktop)) }
    @objc private func goDownloads() { activePane.navigate(to: shellService.specialFolder(.downloads)) }

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
            activePane.navigate(to: PathRef(expanded, isDirectory: true))
        } else {
            NSSound.beep()
        }
    }

    // MARK: - UI Updates

    private func updateAddressBar(_ path: PathRef) {
        addressField.stringValue = path.fullPath
    }

    private func updateStatusBars() {
        leftStatusLabel.stringValue = leftPane.statusText
        rightStatusLabel.stringValue = rightPane.statusText
    }

    private func updateNavButtons() {
        backButton.isEnabled = activePane.navigation.canGoBack
        forwardButton.isEnabled = activePane.navigation.canGoForward
    }

    private func makeToolButton(_ symbolName: String, action: Selector) -> NSButton {
        let img = NSImage(systemSymbolName: symbolName, accessibilityDescription: symbolName)!
        let btn = NSButton(image: img, target: self, action: action)
        btn.translatesAutoresizingMaskIntoConstraints = false
        btn.bezelStyle = .accessoryBarAction
        btn.isBordered = false
        return btn
    }
}
