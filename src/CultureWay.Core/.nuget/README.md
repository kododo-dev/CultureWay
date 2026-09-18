# Kododo.CultureWay.Core

Core abstractions for CultureWay: `IStore`, `IReadOnlySource` and the `Translation` model.

Reference this package when implementing a custom persistence backend or a read-only translation source.

- `IStore`: persistence. Minimal implementation: `InitializeAsync`, `GetAllAsync`, `SetAsync`, `DeleteAsync`. The runtime-language members have default implementations; override them to persist added languages and the default language.
- `IReadOnlySource`: baseline translations (e.g. `.resx`) shown in the editor. Their keys can be overridden or reset, but not deleted.

Full documentation: https://github.com/kododo-dev/CultureWay
