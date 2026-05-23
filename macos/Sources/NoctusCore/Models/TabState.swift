import Foundation

public struct TabState {
    public let id: Int
    public let location: PathRef
    public var viewMode: ViewMode
    public var sortField: SortField
    public var sortDirection: SortDirection

    public init(id: Int, location: PathRef, viewMode: ViewMode = .details,
                sortField: SortField = .name, sortDirection: SortDirection = .ascending) {
        self.id = id
        self.location = location
        self.viewMode = viewMode
        self.sortField = sortField
        self.sortDirection = sortDirection
    }
}
