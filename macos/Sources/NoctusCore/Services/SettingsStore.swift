import Foundation

public final class SettingsStore {
    private var values: [String: Any] = [:]
    private var subscribers: [(prefix: String, callback: (String, Any) -> Void)] = []

    public init() {}

    public func get<T>(_ key: String, default defaultValue: T) -> T {
        guard let raw = values[key] else { return defaultValue }
        return (raw as? T) ?? defaultValue
    }

    public func set(_ key: String, value: Any) {
        values[key] = value
        for sub in subscribers where key.lowercased().hasPrefix(sub.prefix.lowercased()) {
            sub.callback(key, value)
        }
    }

    public func subscribe(prefix: String, callback: @escaping (String, Any) -> Void) {
        subscribers.append((prefix, callback))
    }

    public func save(to path: String) throws {
        var root: [String: Any] = [:]
        for (key, value) in values.sorted(by: { $0.key < $1.key }) {
            let parts = key.split(separator: ".").map(String.init)
            var current = root
            for i in 0..<parts.count - 1 {
                if current[parts[i]] == nil {
                    current[parts[i]] = [String: Any]()
                }
                current = current[parts[i]] as? [String: Any] ?? [:]
            }
            // For simplicity in the spike, flatten to plist-serializable
            root[key] = value
        }

        let data = try JSONSerialization.data(withJSONObject: root, options: [.prettyPrinted, .sortedKeys])
        let dir = (path as NSString).deletingLastPathComponent
        try FileManager.default.createDirectory(atPath: dir, withIntermediateDirectories: true)
        let tmpPath = path + ".tmp"
        try data.write(to: URL(fileURLWithPath: tmpPath))
        try FileManager.default.moveItem(atPath: tmpPath, toPath: path)
    }

    public func load(from path: String) {
        guard let data = FileManager.default.contents(atPath: path),
              let dict = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return }
        values = dict
    }
}
