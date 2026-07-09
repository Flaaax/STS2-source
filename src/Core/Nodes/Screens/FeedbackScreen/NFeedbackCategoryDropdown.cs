using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;

/// <summary>
/// The language dropdown in the OptionsScreen.
/// </summary>
public partial class NFeedbackCategoryDropdown : NDropdown
{
	[Export(PropertyHint.None, "")]
	private PackedScene _dropdownItemScene;

	private NSelectionReticle _selectionReticle;

	private static readonly string[] _baseCategories = new string[3] { "bug", "balance", "feedback" };

	private static readonly LocString[] _baseCategoryLoc = new LocString[3]
	{
		new LocString("settings_ui", "FEEDBACK_CATEGORY.bug"),
		new LocString("settings_ui", "FEEDBACK_CATEGORY.balance"),
		new LocString("settings_ui", "FEEDBACK_CATEGORY.feedback")
	};

	private string[] _categories = Array.Empty<string>();

	private LocString[] _categoryLoc = Array.Empty<LocString>();

	private int _currentCategoryIndex;

	public string CurrentCategory => _categories[_currentCategoryIndex];

	public override void _Ready()
	{
		ConnectSignals();
		_currentOptionHighlight = GetNode<Panel>("%Highlight");
		_currentOptionLabel = GetNode<MegaLabel>("%Label");
		PopulateOptions();
		_currentOptionLabel.SetTextAutoSize(_categoryLoc[_currentCategoryIndex].GetFormattedText());
		_selectionReticle = GetNode<NSelectionReticle>("SelectionReticle");
	}

	protected override void OnFocus()
	{
		_currentOptionHighlight.Modulate = new Color("afcdde");
		if (NControllerManager.Instance.IsUsingController)
		{
			_selectionReticle.OnSelect();
		}
	}

	protected override void OnUnfocus()
	{
		_selectionReticle.OnDeselect();
		_currentOptionHighlight.Modulate = Colors.White;
	}

	private void PopulateOptions()
	{
		if (LocManager.Instance.Language != "eng")
		{
			string[] baseCategories = _baseCategories;
			int num = 0;
			string[] array = new string[1 + baseCategories.Length];
			ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(baseCategories);
			readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			array[num] = "translation";
			_categories = array;
			LocString[] baseCategoryLoc = _baseCategoryLoc;
			num = 0;
			LocString[] array2 = new LocString[1 + baseCategoryLoc.Length];
			ReadOnlySpan<LocString> readOnlySpan2 = new ReadOnlySpan<LocString>(baseCategoryLoc);
			readOnlySpan2.CopyTo(new Span<LocString>(array2).Slice(num, readOnlySpan2.Length));
			num += readOnlySpan2.Length;
			array2[num] = new LocString("settings_ui", "FEEDBACK_CATEGORY.translation");
			_categoryLoc = array2;
		}
		else
		{
			_categories = _baseCategories;
			_categoryLoc = _baseCategoryLoc;
		}
		_currentCategoryIndex = _categories.IndexOf("feedback");
		Control node = GetNode<Control>("DropdownContainer/VBoxContainer");
		foreach (Node child in node.GetChildren())
		{
			node.RemoveChildSafely(child);
			child.QueueFreeSafely();
		}
		for (int i = 0; i < _categories.Length; i++)
		{
			NFeedbackCategoryDropdownItem nFeedbackCategoryDropdownItem = _dropdownItemScene.Instantiate<NFeedbackCategoryDropdownItem>(PackedScene.GenEditState.Disabled);
			node.AddChildSafely(nFeedbackCategoryDropdownItem);
			nFeedbackCategoryDropdownItem.Connect(NDropdownItem.SignalName.Selected, Callable.From<NDropdownItem>(OnDropdownItemSelected));
			nFeedbackCategoryDropdownItem.Init(i, _categoryLoc[i].GetFormattedText());
		}
		node.GetParent<NDropdownContainer>().RefreshLayout();
	}

	private void OnDropdownItemSelected(NDropdownItem item)
	{
		NFeedbackCategoryDropdownItem nFeedbackCategoryDropdownItem = (NFeedbackCategoryDropdownItem)item;
		if (nFeedbackCategoryDropdownItem.CategoryIndex != _currentCategoryIndex)
		{
			CloseDropdown();
			_currentCategoryIndex = nFeedbackCategoryDropdownItem.CategoryIndex;
			_currentOptionLabel.SetTextAutoSize(_categoryLoc[_currentCategoryIndex].GetFormattedText());
		}
	}
}
