import AppKit

/// Hosts two FileListViewControllers in an NSSplitView (dual pane).
class MainSplitViewController: NSSplitViewController {

    private var leftPaneController: FileListViewController!
    private var rightPaneController: FileListViewController!
    private var statusLabel: NSTextField!

    override func viewDidLoad() {
        super.viewDidLoad()

        let homeURL = FileManager.default.homeDirectoryForCurrentUser

        // Left pane — list view
        leftPaneController = FileListViewController(path: homeURL)
        leftPaneController.isActivePane = true

        // Right pane — list view (starts at Desktop)
        let desktopURL = homeURL.appendingPathComponent("Desktop")
        rightPaneController = FileListViewController(path: desktopURL)
        rightPaneController.isActivePane = false

        let leftItem = NSSplitViewItem(viewController: leftPaneController)
        leftItem.minimumThickness = 250
        leftItem.holdingPriority = .defaultLow

        let rightItem = NSSplitViewItem(viewController: rightPaneController)
        rightItem.minimumThickness = 250
        rightItem.holdingPriority = .defaultLow

        addSplitViewItem(leftItem)
        addSplitViewItem(rightItem)

        // Click to switch active pane
        let leftClick = NSClickGestureRecognizer(target: self, action: #selector(leftPaneClicked))
        leftPaneController.view.addGestureRecognizer(leftClick)

        let rightClick = NSClickGestureRecognizer(target: self, action: #selector(rightPaneClicked))
        rightPaneController.view.addGestureRecognizer(rightClick)
    }

    // MARK: - Actions

    @objc func toggleViewMode() {
        // Toggle the active pane's view mode
        if leftPaneController.isActivePane {
            leftPaneController.toggleViewMode()
        } else {
            rightPaneController.toggleViewMode()
        }
    }

    @objc func refreshAll() {
        leftPaneController.refresh()
        rightPaneController.refresh()
    }

    @objc private func leftPaneClicked() {
        leftPaneController.isActivePane = true
        rightPaneController.isActivePane = false
    }

    @objc private func rightPaneClicked() {
        leftPaneController.isActivePane = false
        rightPaneController.isActivePane = true
    }

    // Tab key to switch panes
    override func keyDown(with event: NSEvent) {
        if event.keyCode == 48 { // Tab key
            if leftPaneController.isActivePane {
                rightPaneClicked()
            } else {
                leftPaneClicked()
            }
        } else {
            super.keyDown(with: event)
        }
    }

    override var acceptsFirstResponder: Bool { true }
}
