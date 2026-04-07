using System;
using System.Collections.Generic;

namespace Sandbox;

public class EditorEventsManager {
	readonly List<string> _firedEventNames = new();
	readonly object _lock = new();

	public IReadOnlyList<string> AllEventNames { get; }

	public IReadOnlyList<string> FiredEventNames {
		get {
			lock ( _lock )
				return new List<string>(_firedEventNames);
		}
	}

	public event Action<string> OnEventFired;

	public EditorEventsManager() {
		AllEventNames = new List<string> {
			"assetsystem.newfolder",
			"content.changed",
			"compile.shader",
			"package.changed",
			"package.changed.installed",
			"package.changed.uninstalled",
			"package.changed.favourite",
			"package.changed.rating",
			"scene.session.save",
			"scene.saved",
			"actiongraph.saving",
			"actiongraph.saved",
		};
	}

	void RecordFired(string eventName) {
		lock ( _lock )
			_firedEventNames.Add(eventName);
		try {
			OnEventFired?.Invoke(eventName);
		} catch ( Exception ex ) {
			Log.Warning($"[EditorEventsManager] OnEventFired callback failed for '{eventName}': {ex.Message}");
		}
	}

	// ─── Asset System ───────────────────────────────────────────────────────
	[Event("assetsystem.newfolder", Priority = 100)]
	void OnAssetsystemNewfolder() => RecordFired("assetsystem.newfolder");

	[Event("content.changed", Priority = 100)]
	void OnContentChanged(string _) => RecordFired("content.changed");

	[Event("compile.shader", Priority = 100)]
	void OnCompileShader(string _) => RecordFired("compile.shader");

	[Event("package.changed", Priority = 100)]
	void OnPackageChanged(Package _) => RecordFired("package.changed");

	[Event("package.changed.installed", Priority = 100)]
	void OnPackageChangedInstalled(Package _) => RecordFired("package.changed.installed");

	[Event("package.changed.uninstalled", Priority = 100)]
	void OnPackageChangedUninstalled(Package _) => RecordFired("package.changed.uninstalled");

	[Event("package.changed.favourite", Priority = 100)]
	void OnPackageChangedFavourite(Package _) => RecordFired("package.changed.favourite");

	[Event("package.changed.rating", Priority = 100)]
	void OnPackageChangedRating(Package _) => RecordFired("package.changed.rating");

	// ─── Scenes ─────────────────────────────────────────────────────────────
	[Event("scene.session.save", Priority = 100)]
	void OnSceneSessionSave() => RecordFired("scene.session.save");

	[Event("scene.saved", Priority = 100)]
	void OnSceneSaved(Scene _) => RecordFired("scene.saved");


	[Event("actiongraph.saving", Priority = 100)]
	void OnActiongraphSaving(object _, GameResource __) => RecordFired("actiongraph.saving");

	[Event("actiongraph.saved", Priority = 100)]
	void OnActiongraphSaved(object _) => RecordFired("actiongraph.saved");
}
