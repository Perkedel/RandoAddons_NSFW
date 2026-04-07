using System;
using Editor;
using Sandbox.git;

namespace Sandbox.widgets;

/// <summary>
/// Full-area settings view for SandGit. Covers branch, file list, and commit areas when shown.
/// Exit button returns to the regular repo view.
/// </summary>
public class SandGitSettingsWidget : Widget {
	const float HeaderHeight = 32f;
	const float SectionSpacing = 12f;
	const float RowHeight = 28f;
	const float LabelWidth = 100f;

	private readonly Action _onExit;
	private readonly LineEdit _gitPathField;

	public SandGitSettingsWidget(Widget parent, Action onExit) : base(parent) {
		_onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));
		Layout = Layout.Column();
		Layout.Spacing = SectionSpacing;

		var headerRow = new Widget(this) { Layout = Layout.Row(), FixedHeight = HeaderHeight };
		headerRow.Layout.Spacing = 8f;

		var titleLabel = new Label("Settings", headerRow);
		var exitButton = new Button(headerRow) { Text = "Back" };
		exitButton.Clicked += OnExitClicked;

		var headerSpacer = new Widget(headerRow) { MinimumWidth = 0 };
		headerRow.Layout.Add(titleLabel);
		headerRow.Layout.Add(headerSpacer, 1);
		headerRow.Layout.Add(exitButton);

		var gitSection = new Widget(this) { Layout = Layout.Column() };
		gitSection.Layout.Spacing = 4f;

		var gitRow = new Widget(gitSection) { Layout = Layout.Row() };
		gitRow.Layout.Spacing = 8f;

		var gitLabel = new Label("Git executable", gitRow) { MinimumWidth = LabelWidth };
		_gitPathField = new LineEdit(gitRow) {
			MinimumHeight = RowHeight,
			MinimumWidth = 0,
			PlaceholderText = "git or full path to git.exe"
		};
		_gitPathField.Text = Core.GetCurrentGitPath();

		gitRow.Layout.Add(gitLabel);
		gitRow.Layout.Add(_gitPathField, 1);

		gitSection.Layout.Add(gitRow);

		var spacer = new Widget(this) { Layout = Layout.Column() };
		Layout.Add(headerRow);
		Layout.Add(gitSection);
		Layout.Add(spacer, 1);
	}

	void OnExitClicked() {
		if ( !IsValid )
			return;
		var path = _gitPathField?.Text?.Trim() ?? string.Empty;
		Core.SetGitPathOverride(string.IsNullOrEmpty(path) ? null : path);
		_onExit();
	}
}
