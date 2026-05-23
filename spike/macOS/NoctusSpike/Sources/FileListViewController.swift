import AppKit

/// A file list pane using NSOutlineView (list/details view mode)
/// or NSBrowser (column view mode).
class FileListViewController: NSViewController {

    enum ViewMode {
        case list
        case columns
    }

    private var currentPath: URL
    private var items: [FileItem] = []
    private var viewMode: ViewMode = .list

    // List view components
    private var scrollView: NSScrollView!
    private var outlineView: NSOutlineView!

    // Column view components
    private var browser: NSBrowser!

    // Active pane indicator
    private var borderView: NSView!
    var isActivePane: Bool = false {
        didSet { updateActiveBorder() }
    }

    init(path: URL) {
        self.currentPath = path
        super.init(nibName: nil, bundle: nil)
    }

    required init?(coder: NSCoder) { fatalError() }

    override func loadView() {
        let container = NSView()
        container.wantsLayer = true

        // Border indicator
        borderView = NSView()
        borderView.wantsLayer = true
        borderView.layer?.borderWidth = 2
        borderView.layer?.borderColor = NSColor.clear.cgColor
        borderView.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(borderView)

        // Outline view (list mode)
        outlineView = NSOutlineView()
        outlineView.headerView = NSTableHeaderView()
        outlineView.usesAlternatingRowBackgroundColors = true
        outlineView.allowsMultipleSelection = true
        outlineView.style = .fullWidth
        outlineView.doubleAction = #selector(doubleClicked)
        outlineView.target = self

        // Register for drag
        outlineView.registerForDraggedTypes([.fileURL])
        outlineView.setDraggingSourceOperationMask([.copy, .move], forLocal: false)

        // Columns
        let nameCol = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("name"))
        nameCol.title = "Name"
        nameCol.width = 300
        nameCol.minWidth = 100
        nameCol.sortDescriptorPrototype = NSSortDescriptor(key: "name", ascending: true, selector: #selector(NSString.caseInsensitiveCompare))
        outlineView.addTableColumn(nameCol)
        outlineView.outlineTableColumn = nameCol

        let sizeCol = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("size"))
        sizeCol.title = "Size"
        sizeCol.width = 80
        sizeCol.minWidth = 60
        outlineView.addTableColumn(sizeCol)

        let dateCol = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("dateModified"))
        dateCol.title = "Date Modified"
        dateCol.width = 160
        dateCol.minWidth = 100
        outlineView.addTableColumn(dateCol)

        let kindCol = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("kind"))
        kindCol.title = "Kind"
        kindCol.width = 120
        kindCol.minWidth = 80
        outlineView.addTableColumn(kindCol)

        outlineView.dataSource = self
        outlineView.delegate = self

        scrollView = NSScrollView()
        scrollView.documentView = outlineView
        scrollView.hasVerticalScroller = true
        scrollView.hasHorizontalScroller = true
        scrollView.autohidesScrollers = true
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(scrollView)

        // Browser (column mode)
        browser = NSBrowser()
        browser.delegate = self
        browser.hasHorizontalScroller = true
        browser.isTitled = false
        browser.separatesColumns = true
        browser.translatesAutoresizingMaskIntoConstraints = false
        browser.isHidden = true
        container.addSubview(browser)

        NSLayoutConstraint.activate([
            borderView.topAnchor.constraint(equalTo: container.topAnchor),
            borderView.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            borderView.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            borderView.bottomAnchor.constraint(equalTo: container.bottomAnchor),

            scrollView.topAnchor.constraint(equalTo: container.topAnchor, constant: 2),
            scrollView.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 2),
            scrollView.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -2),
            scrollView.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -2),

            browser.topAnchor.constraint(equalTo: container.topAnchor, constant: 2),
            browser.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 2),
            browser.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -2),
            browser.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -2),
        ])

        self.view = container
    }

    override func viewDidLoad() {
        super.viewDidLoad()

        // Context menu
        let menu = NSMenu()
        menu.delegate = self
        outlineView.menu = menu

        loadDirectory(currentPath)
        updateActiveBorder()
    }

    // MARK: - Public API

    func navigate(to url: URL) {
        currentPath = url
        loadDirectory(url)
    }

    func toggleViewMode() {
        switch viewMode {
        case .list:
            viewMode = .columns
            scrollView.isHidden = true
            browser.isHidden = false
            browser.loadColumnZero()
        case .columns:
            viewMode = .list
            scrollView.isHidden = false
            browser.isHidden = true
            outlineView.reloadData()
        }
    }

    func refresh() {
        loadDirectory(currentPath)
    }

    // MARK: - Private

    private func loadDirectory(_ url: URL) {
        let fm = FileManager.default
        guard let contents = try? fm.contentsOfDirectory(
            at: url,
            includingPropertiesForKeys: [.isDirectoryKey, .fileSizeKey, .contentModificationDateKey, .localizedTypeDescriptionKey],
            options: [.skipsHiddenFiles]
        ) else { return }

        items = contents.map { FileItem(url: $0) }
            .sorted { a, b in
                // Folders first, then by name
                if a.isDirectory != b.isDirectory { return a.isDirectory }
                return a.name.localizedCaseInsensitiveCompare(b.name) == .orderedAscending
            }

        outlineView.reloadData()
        if viewMode == .columns {
            browser.loadColumnZero()
        }
    }

    private func updateActiveBorder() {
        guard isViewLoaded else { return }
        borderView.layer?.borderColor = isActivePane
            ? NSColor.controlAccentColor.cgColor
            : NSColor.clear.cgColor
    }

    @objc private func doubleClicked() {
        let row = outlineView.clickedRow
        guard row >= 0, row < items.count else { return }
        let item = items[row]

        if item.isDirectory {
            navigate(to: item.url)
        } else {
            NSWorkspace.shared.open(item.url)
        }
    }

    private func contentsOf(url: URL) -> [FileItem] {
        let fm = FileManager.default
        guard let contents = try? fm.contentsOfDirectory(
            at: url,
            includingPropertiesForKeys: [.isDirectoryKey, .fileSizeKey, .contentModificationDateKey, .localizedTypeDescriptionKey],
            options: [.skipsHiddenFiles]
        ) else { return [] }

        return contents.map { FileItem(url: $0) }
            .sorted { a, b in
                if a.isDirectory != b.isDirectory { return a.isDirectory }
                return a.name.localizedCaseInsensitiveCompare(b.name) == .orderedAscending
            }
    }
}

// MARK: - NSOutlineViewDataSource

extension FileListViewController: NSOutlineViewDataSource {

    func outlineView(_ outlineView: NSOutlineView, numberOfChildrenOfItem item: Any?) -> Int {
        return item == nil ? items.count : 0
    }

    func outlineView(_ outlineView: NSOutlineView, child index: Int, ofItem item: Any?) -> Any {
        return items[index]
    }

    func outlineView(_ outlineView: NSOutlineView, isItemExpandable item: Any) -> Bool {
        return false // Flat list, double-click to navigate into folders
    }

    // Drag source
    func outlineView(_ outlineView: NSOutlineView, pasteboardWriterForItem item: Any) -> (any NSPasteboardWriting)? {
        guard let fileItem = item as? FileItem else { return nil }
        return fileItem.url as NSURL
    }

    // Drop target
    func outlineView(_ outlineView: NSOutlineView, validateDrop info: any NSDraggingInfo, proposedItem item: Any?, proposedChildIndex index: Int) -> NSDragOperation {
        return .copy
    }

    func outlineView(_ outlineView: NSOutlineView, acceptDrop info: any NSDraggingInfo, item: Any?, childIndex index: Int) -> Bool {
        guard let urls = info.draggingPasteboard.readObjects(forClasses: [NSURL.self]) as? [URL] else {
            return false
        }
        let fm = FileManager.default
        for url in urls {
            let dest = currentPath.appendingPathComponent(url.lastPathComponent)
            try? fm.copyItem(at: url, to: dest)
        }
        refresh()
        return true
    }
}

// MARK: - NSOutlineViewDelegate

extension FileListViewController: NSOutlineViewDelegate {

    func outlineView(_ outlineView: NSOutlineView, viewFor tableColumn: NSTableColumn?, item: Any) -> NSView? {
        guard let fileItem = item as? FileItem, let column = tableColumn else { return nil }
        let id = column.identifier

        let cell = outlineView.makeView(withIdentifier: id, owner: self) as? NSTableCellView
            ?? makeCellView(identifier: id)

        switch id.rawValue {
        case "name":
            cell.textField?.stringValue = fileItem.name
            cell.imageView?.image = NSWorkspace.shared.icon(forFile: fileItem.url.path)
            cell.imageView?.image?.size = NSSize(width: 16, height: 16)
        case "size":
            cell.textField?.stringValue = fileItem.displaySize
            cell.textField?.alignment = .right
        case "dateModified":
            cell.textField?.stringValue = fileItem.displayDate
        case "kind":
            cell.textField?.stringValue = fileItem.kind
        default:
            break
        }

        return cell
    }

    private func makeCellView(identifier: NSUserInterfaceItemIdentifier) -> NSTableCellView {
        let cell = NSTableCellView()
        cell.identifier = identifier

        let textField = NSTextField(labelWithString: "")
        textField.lineBreakMode = .byTruncatingTail
        textField.translatesAutoresizingMaskIntoConstraints = false
        cell.addSubview(textField)
        cell.textField = textField

        if identifier.rawValue == "name" {
            let imageView = NSImageView()
            imageView.translatesAutoresizingMaskIntoConstraints = false
            cell.addSubview(imageView)
            cell.imageView = imageView

            NSLayoutConstraint.activate([
                imageView.leadingAnchor.constraint(equalTo: cell.leadingAnchor, constant: 2),
                imageView.centerYAnchor.constraint(equalTo: cell.centerYAnchor),
                imageView.widthAnchor.constraint(equalToConstant: 16),
                imageView.heightAnchor.constraint(equalToConstant: 16),
                textField.leadingAnchor.constraint(equalTo: imageView.trailingAnchor, constant: 4),
                textField.trailingAnchor.constraint(equalTo: cell.trailingAnchor, constant: -2),
                textField.centerYAnchor.constraint(equalTo: cell.centerYAnchor),
            ])
        } else {
            NSLayoutConstraint.activate([
                textField.leadingAnchor.constraint(equalTo: cell.leadingAnchor, constant: 2),
                textField.trailingAnchor.constraint(equalTo: cell.trailingAnchor, constant: -2),
                textField.centerYAnchor.constraint(equalTo: cell.centerYAnchor),
            ])
        }

        return cell
    }
}

// MARK: - NSBrowserDelegate

extension FileListViewController: NSBrowserDelegate {

    func browser(_ browser: NSBrowser, numberOfRowsInColumn column: Int) -> Int {
        let url = urlForBrowserColumn(column)
        return contentsOf(url: url).count
    }

    func browser(_ browser: NSBrowser, willDisplayCell cell: Any, atRow row: Int, column: Int) {
        guard let browserCell = cell as? NSBrowserCell else { return }
        let url = urlForBrowserColumn(column)
        let contents = contentsOf(url: url)
        guard row < contents.count else { return }

        let item = contents[row]
        browserCell.title = item.name
        browserCell.isLeaf = !item.isDirectory
        browserCell.image = NSWorkspace.shared.icon(forFile: item.url.path)
        browserCell.image?.size = NSSize(width: 16, height: 16)
    }

    private func urlForBrowserColumn(_ column: Int) -> URL {
        var url = currentPath
        for col in 0..<column {
            let selectedRow = browser.selectedRow(inColumn: col)
            guard selectedRow >= 0 else { break }
            let contents = contentsOf(url: url)
            guard selectedRow < contents.count else { break }
            url = contents[selectedRow].url
        }
        return url
    }
}

// MARK: - NSMenuDelegate (Context Menu)

extension FileListViewController: NSMenuDelegate {

    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()

        let clickedRow = outlineView.clickedRow
        guard clickedRow >= 0, clickedRow < items.count else { return }
        let item = items[clickedRow]

        // Open
        menu.addItem(withTitle: "Open", action: #selector(contextOpen(_:)), keyEquivalent: "")
        menu.items.last?.representedObject = item
        menu.items.last?.target = self

        // Open With submenu
        let openWithItem = NSMenuItem(title: "Open With", action: nil, keyEquivalent: "")
        let openWithMenu = NSMenu()
        let apps = NSWorkspace.shared.urlsForApplications(toOpen: item.url)
        for appURL in apps.prefix(10) {
            let appName = appURL.deletingPathExtension().lastPathComponent
            let mi = NSMenuItem(title: appName, action: #selector(contextOpenWith(_:)), keyEquivalent: "")
            mi.representedObject = (item, appURL)
            mi.target = self
            openWithMenu.addItem(mi)
        }
        openWithItem.submenu = openWithMenu
        menu.addItem(openWithItem)

        menu.addItem(.separator())

        // Reveal in Finder
        menu.addItem(withTitle: "Reveal in Finder", action: #selector(contextRevealInFinder(_:)), keyEquivalent: "")
        menu.items.last?.representedObject = item
        menu.items.last?.target = self

        // Copy Path
        menu.addItem(withTitle: "Copy Path", action: #selector(contextCopyPath(_:)), keyEquivalent: "")
        menu.items.last?.representedObject = item
        menu.items.last?.target = self

        menu.addItem(.separator())

        // Move to Trash
        menu.addItem(withTitle: "Move to Trash", action: #selector(contextTrash(_:)), keyEquivalent: "")
        menu.items.last?.representedObject = item
        menu.items.last?.target = self

        // Rename
        menu.addItem(withTitle: "Rename…", action: #selector(contextRename(_:)), keyEquivalent: "")
        menu.items.last?.representedObject = item
        menu.items.last?.target = self
    }

    @objc private func contextOpen(_ sender: NSMenuItem) {
        guard let item = sender.representedObject as? FileItem else { return }
        if item.isDirectory {
            navigate(to: item.url)
        } else {
            NSWorkspace.shared.open(item.url)
        }
    }

    @objc private func contextOpenWith(_ sender: NSMenuItem) {
        guard let (item, appURL) = sender.representedObject as? (FileItem, URL) else { return }
        NSWorkspace.shared.open([item.url], withApplicationAt: appURL, configuration: NSWorkspace.OpenConfiguration())
    }

    @objc private func contextRevealInFinder(_ sender: NSMenuItem) {
        guard let item = sender.representedObject as? FileItem else { return }
        NSWorkspace.shared.activateFileViewerSelecting([item.url])
    }

    @objc private func contextCopyPath(_ sender: NSMenuItem) {
        guard let item = sender.representedObject as? FileItem else { return }
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(item.url.path, forType: .string)
    }

    @objc private func contextTrash(_ sender: NSMenuItem) {
        guard let item = sender.representedObject as? FileItem else { return }
        do {
            try FileManager.default.trashItem(at: item.url, resultingItemURL: nil)
            refresh()
        } catch {
            let alert = NSAlert(error: error)
            alert.runModal()
        }
    }

    @objc private func contextRename(_ sender: NSMenuItem) {
        guard let item = sender.representedObject as? FileItem else { return }
        let row = items.firstIndex(where: { $0.url == item.url })
        guard let row = row else { return }

        // Get the name cell and make it editable
        guard let cellView = outlineView.view(atColumn: 0, row: row, makeIfNecessary: false) as? NSTableCellView,
              let textField = cellView.textField else { return }

        textField.isEditable = true
        textField.delegate = self
        textField.tag = row
        view.window?.makeFirstResponder(textField)
    }
}

// MARK: - NSTextFieldDelegate (Rename)

extension FileListViewController: NSTextFieldDelegate {

    func control(_ control: NSControl, textShouldEndEditing fieldEditor: NSText) -> Bool {
        guard let textField = control as? NSTextField else { return true }
        let row = textField.tag
        guard row >= 0, row < items.count else { return true }

        let item = items[row]
        let newName = textField.stringValue
        guard !newName.isEmpty, newName != item.name else {
            textField.isEditable = false
            return true
        }

        let newURL = item.url.deletingLastPathComponent().appendingPathComponent(newName)
        do {
            try FileManager.default.moveItem(at: item.url, to: newURL)
            textField.isEditable = false
            refresh()
        } catch {
            let alert = NSAlert(error: error)
            alert.runModal()
            textField.stringValue = item.name
        }

        return true
    }
}
