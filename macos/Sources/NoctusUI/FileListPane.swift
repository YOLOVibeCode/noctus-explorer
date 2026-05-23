import AppKit
import NoctusCore
import NoctusShellAdapter

/// A single file browsing pane backed by NSOutlineView.
public class FileListPane: NSViewController {

    private let shellService: ShellServiceProtocol
    private let fileWatcher: FileWatcherProtocol
    private var entries: [FileEntry] = []
    private(set) var currentPath: PathRef
    var navigation: NavigationHistory

    // UI
    private var scrollView: NSScrollView!
    private var outlineView: NSOutlineView!
    private var borderView: NSView!

    // Callbacks
    var onNavigate: ((PathRef) -> Void)?
    var onSelectionChanged: (([FileEntry]) -> Void)?
    var onActivated: (() -> Void)?

    var isActivePane: Bool = false {
        didSet {
            guard isViewLoaded else { return }
            borderView.layer?.borderColor = isActivePane
                ? NSColor.controlAccentColor.cgColor
                : NSColor.clear.cgColor
        }
    }

    public init(path: PathRef, shellService: ShellServiceProtocol, fileWatcher: FileWatcherProtocol) {
        self.currentPath = path
        self.shellService = shellService
        self.fileWatcher = fileWatcher
        self.navigation = NavigationHistory(path)
        super.init(nibName: nil, bundle: nil)
    }

    required init?(coder: NSCoder) { fatalError() }

    public override func loadView() {
        let container = NSView()
        container.wantsLayer = true

        borderView = NSView()
        borderView.wantsLayer = true
        borderView.layer?.borderWidth = 2
        borderView.layer?.borderColor = NSColor.clear.cgColor
        borderView.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(borderView)

        outlineView = NSOutlineView()
        outlineView.headerView = NSTableHeaderView()
        outlineView.usesAlternatingRowBackgroundColors = true
        outlineView.allowsMultipleSelection = true
        outlineView.style = .fullWidth
        outlineView.doubleAction = #selector(doubleClicked)
        outlineView.target = self
        outlineView.dataSource = self
        outlineView.delegate = self
        outlineView.registerForDraggedTypes([.fileURL])
        outlineView.setDraggingSourceOperationMask([.copy, .move], forLocal: false)

        addColumn("name", title: "Name", width: 300)
        addColumn("size", title: "Size", width: 80)
        addColumn("dateModified", title: "Date Modified", width: 160)
        addColumn("kind", title: "Kind", width: 120)

        scrollView = NSScrollView()
        scrollView.documentView = outlineView
        scrollView.hasVerticalScroller = true
        scrollView.autohidesScrollers = true
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(scrollView)

        NSLayoutConstraint.activate([
            borderView.topAnchor.constraint(equalTo: container.topAnchor),
            borderView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            borderView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            borderView.bottomAnchor.constraint(equalTo: container.bottomAnchor),
            scrollView.topAnchor.constraint(equalTo: container.topAnchor, constant: 2),
            scrollView.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 2),
            scrollView.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -2),
            scrollView.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -2),
        ])

        self.view = container
    }

    public override func viewDidLoad() {
        super.viewDidLoad()

        let menu = NSMenu()
        menu.delegate = self
        outlineView.menu = menu

        Task { await loadDirectory(currentPath) }
    }

    // MARK: - Public

    func navigate(to path: PathRef) {
        navigation.push(path)
        currentPath = path
        onNavigate?(path)
        Task { await loadDirectory(path) }
    }

    func goBack() {
        guard navigation.canGoBack else { return }
        let target = navigation.goBack()
        currentPath = target
        onNavigate?(target)
        Task { await loadDirectory(target) }
    }

    func goForward() {
        guard navigation.canGoForward else { return }
        let target = navigation.goForward()
        currentPath = target
        onNavigate?(target)
        Task { await loadDirectory(target) }
    }

    func goUp() {
        guard let parent = currentPath.getParent() else { return }
        navigate(to: parent)
    }

    func refresh() {
        Task { await loadDirectory(currentPath) }
    }

    var selectedEntries: [FileEntry] {
        outlineView.selectedRowIndexes.compactMap { idx in
            idx < entries.count ? entries[idx] : nil
        }
    }

    var statusText: String {
        let sel = outlineView.selectedRowIndexes
        if sel.isEmpty {
            return "\(entries.count) items"
        }
        let totalSize = sel.compactMap { entries[$0].size }.reduce(0, +)
        return "\(sel.count) selected (\(ByteCountFormatter.string(fromByteCount: totalSize, countStyle: .file)))"
    }

    // MARK: - Private

    private func loadDirectory(_ path: PathRef) async {
        do {
            let items = try await shellService.enumerate(path)
            entries = items.sorted { a, b in
                if a.isDirectory != b.isDirectory { return a.isDirectory }
                return a.name.localizedCaseInsensitiveCompare(b.name) == .orderedAscending
            }
            outlineView.reloadData()
            onSelectionChanged?([])
        } catch {
            entries = []
            outlineView.reloadData()
        }
    }

    @objc private func doubleClicked() {
        let row = outlineView.clickedRow
        guard row >= 0, row < entries.count else { return }
        let item = entries[row]
        if item.isDirectory {
            navigate(to: item.path)
        } else {
            NSWorkspace.shared.open(URL(fileURLWithPath: item.path.fullPath))
        }
    }

    private func addColumn(_ id: String, title: String, width: CGFloat) {
        let col = NSTableColumn(identifier: NSUserInterfaceItemIdentifier(id))
        col.title = title
        col.width = width
        col.minWidth = 60
        outlineView.addTableColumn(col)
        if id == "name" { outlineView.outlineTableColumn = col }
    }
}

// MARK: - NSOutlineViewDataSource & Delegate

extension FileListPane: NSOutlineViewDataSource, NSOutlineViewDelegate {
    public func outlineView(_ outlineView: NSOutlineView, numberOfChildrenOfItem item: Any?) -> Int {
        item == nil ? entries.count : 0
    }

    public func outlineView(_ outlineView: NSOutlineView, child index: Int, ofItem item: Any?) -> Any {
        entries[index]
    }

    public func outlineView(_ outlineView: NSOutlineView, isItemExpandable item: Any) -> Bool { false }

    public func outlineView(_ outlineView: NSOutlineView, viewFor tableColumn: NSTableColumn?, item: Any) -> NSView? {
        guard let entry = item as? FileEntry, let col = tableColumn else { return nil }
        let id = col.identifier

        let cell = outlineView.makeView(withIdentifier: id, owner: self) as? NSTableCellView ?? {
            let c = NSTableCellView()
            c.identifier = id
            let tf = NSTextField(labelWithString: "")
            tf.lineBreakMode = .byTruncatingTail
            tf.translatesAutoresizingMaskIntoConstraints = false
            c.addSubview(tf)
            c.textField = tf

            if id.rawValue == "name" {
                let iv = NSImageView()
                iv.translatesAutoresizingMaskIntoConstraints = false
                c.addSubview(iv)
                c.imageView = iv
                NSLayoutConstraint.activate([
                    iv.leadingAnchor.constraint(equalTo: c.leadingAnchor, constant: 2),
                    iv.centerYAnchor.constraint(equalTo: c.centerYAnchor),
                    iv.widthAnchor.constraint(equalToConstant: 16),
                    iv.heightAnchor.constraint(equalToConstant: 16),
                    tf.leadingAnchor.constraint(equalTo: iv.trailingAnchor, constant: 4),
                    tf.trailingAnchor.constraint(equalTo: c.trailingAnchor, constant: -2),
                    tf.centerYAnchor.constraint(equalTo: c.centerYAnchor),
                ])
            } else {
                NSLayoutConstraint.activate([
                    tf.leadingAnchor.constraint(equalTo: c.leadingAnchor, constant: 2),
                    tf.trailingAnchor.constraint(equalTo: c.trailingAnchor, constant: -2),
                    tf.centerYAnchor.constraint(equalTo: c.centerYAnchor),
                ])
            }
            return c
        }()

        switch id.rawValue {
        case "name":
            cell.textField?.stringValue = entry.name
            cell.imageView?.image = NSWorkspace.shared.icon(forFile: entry.path.fullPath)
            cell.imageView?.image?.size = NSSize(width: 16, height: 16)
        case "size":
            cell.textField?.stringValue = entry.isDirectory ? "—" : ByteCountFormatter.string(fromByteCount: entry.size ?? 0, countStyle: .file)
            cell.textField?.alignment = .right
        case "dateModified":
            let fmt = DateFormatter()
            fmt.dateStyle = .medium
            fmt.timeStyle = .short
            cell.textField?.stringValue = fmt.string(from: entry.dateModified)
        case "kind":
            cell.textField?.stringValue = entry.kind
        default: break
        }
        return cell
    }

    public func outlineViewSelectionDidChange(_ notification: Notification) {
        if !isActivePane { onActivated?() }
        onSelectionChanged?(selectedEntries)
    }

    // Drag source
    public func outlineView(_ outlineView: NSOutlineView, pasteboardWriterForItem item: Any) -> (any NSPasteboardWriting)? {
        guard let entry = item as? FileEntry else { return nil }
        return URL(fileURLWithPath: entry.path.fullPath) as NSURL
    }

    // Drop target
    public func outlineView(_ outlineView: NSOutlineView, validateDrop info: any NSDraggingInfo, proposedItem item: Any?, proposedChildIndex index: Int) -> NSDragOperation { .copy }

    public func outlineView(_ outlineView: NSOutlineView, acceptDrop info: any NSDraggingInfo, item: Any?, childIndex index: Int) -> Bool {
        guard let urls = info.draggingPasteboard.readObjects(forClasses: [NSURL.self]) as? [URL] else { return false }
        for url in urls {
            let dest = URL(fileURLWithPath: currentPath.fullPath).appendingPathComponent(url.lastPathComponent)
            try? FileManager.default.copyItem(at: url, to: dest)
        }
        refresh()
        return true
    }
}

// MARK: - Context Menu

extension FileListPane: NSMenuDelegate {
    public func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()
        let row = outlineView.clickedRow
        guard row >= 0, row < entries.count else { return }
        let entry = entries[row]

        let openItem = NSMenuItem(title: "Open", action: #selector(contextOpen(_:)), keyEquivalent: "")
        openItem.representedObject = entry
        openItem.target = self
        menu.addItem(openItem)

        menu.addItem(.separator())

        let revealItem = NSMenuItem(title: "Reveal in Finder", action: #selector(contextReveal(_:)), keyEquivalent: "")
        revealItem.representedObject = entry
        revealItem.target = self
        menu.addItem(revealItem)

        let copyPathItem = NSMenuItem(title: "Copy Path", action: #selector(contextCopyPath(_:)), keyEquivalent: "")
        copyPathItem.representedObject = entry
        copyPathItem.target = self
        menu.addItem(copyPathItem)

        menu.addItem(.separator())

        let trashItem = NSMenuItem(title: "Move to Trash", action: #selector(contextTrash(_:)), keyEquivalent: "")
        trashItem.representedObject = entry
        trashItem.target = self
        menu.addItem(trashItem)
    }

    @objc private func contextOpen(_ sender: NSMenuItem) {
        guard let entry = sender.representedObject as? FileEntry else { return }
        if entry.isDirectory { navigate(to: entry.path) }
        else { NSWorkspace.shared.open(URL(fileURLWithPath: entry.path.fullPath)) }
    }

    @objc private func contextReveal(_ sender: NSMenuItem) {
        guard let entry = sender.representedObject as? FileEntry else { return }
        NSWorkspace.shared.activateFileViewerSelecting([URL(fileURLWithPath: entry.path.fullPath)])
    }

    @objc private func contextCopyPath(_ sender: NSMenuItem) {
        guard let entry = sender.representedObject as? FileEntry else { return }
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(entry.path.fullPath, forType: .string)
    }

    @objc private func contextTrash(_ sender: NSMenuItem) {
        guard let entry = sender.representedObject as? FileEntry else { return }
        try? FileManager.default.trashItem(at: URL(fileURLWithPath: entry.path.fullPath), resultingItemURL: nil)
        refresh()
    }
}
