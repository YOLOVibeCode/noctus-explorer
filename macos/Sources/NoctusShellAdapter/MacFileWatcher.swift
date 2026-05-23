import Foundation
import NoctusCore

public final class MacFileWatcher: FileWatcherProtocol {
    private var sources: [String: DispatchSourceFileSystemObject] = [:]
    private var descriptors: [String: Int32] = [:]
    public var onChange: ((FileChangeType, PathRef, PathRef?) -> Void)?

    public init() {}

    public func watch(_ directory: PathRef) {
        let path = directory.fullPath
        guard sources[path] == nil else { return }

        let fd = open(path, O_EVTONLY)
        guard fd >= 0 else { return }

        let source = DispatchSource.makeFileSystemObjectSource(
            fileDescriptor: fd,
            eventMask: [.write, .delete, .rename, .attrib],
            queue: .global()
        )

        source.setEventHandler { [weak self] in
            self?.onChange?(.modified, directory, nil)
        }

        source.setCancelHandler {
            close(fd)
        }

        source.resume()
        sources[path] = source
        descriptors[path] = fd
    }

    public func unwatch(_ directory: PathRef) {
        let path = directory.fullPath
        sources[path]?.cancel()
        sources.removeValue(forKey: path)
        descriptors.removeValue(forKey: path)
    }

    deinit {
        for (_, source) in sources {
            source.cancel()
        }
    }
}
