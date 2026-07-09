using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

public partial class NCustomRunModifiersList : Control
{
	[Signal]
	public delegate void ModifiersChangedEventHandler();

	private readonly List<NRunModifierTickbox> _modifierTickboxes = new List<NRunModifierTickbox>();

	private Control _container;

	private MultiplayerUiMode _mode;

	public Control? DefaultFocusedControl => _modifierTickboxes.FirstOrDefault();

	public override void _Ready()
	{
		_container = GetNode<Control>("ScrollContainer/Mask/Content");
		foreach (Node child in _container.GetChildren())
		{
			child.QueueFreeSafely();
		}
		foreach (ModifierModel allModifier in GetAllModifiers())
		{
			NRunModifierTickbox nRunModifierTickbox = NRunModifierTickbox.Create(allModifier);
			_container.AddChildSafely(nRunModifierTickbox);
			_modifierTickboxes.Add(nRunModifierTickbox);
			nRunModifierTickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NRunModifierTickbox>(AfterModifiersChanged));
		}
	}

	/// <summary>
	/// Call this once when the menu opens with the appropriate mode for the screen we're displayed on.
	/// </summary>
	/// <param name="mode">See the documentation for the enumeration for the correct value to pass.</param>
	public void Initialize(MultiplayerUiMode mode)
	{
		_mode = mode;
		if ((uint)(mode - 3) > 1u)
		{
			return;
		}
		foreach (NRunModifierTickbox modifierTickbox in _modifierTickboxes)
		{
			modifierTickbox.Disable();
		}
	}

	/// <summary>
	/// Call this when:
	/// - We are in client mode and the host changes the modifiers that are selected
	/// - Once when the game is loaded from a multiplayer save
	/// </summary>
	/// <param name="modifiers">The modifiers to display as checked.</param>
	public void SyncModifierList(IReadOnlyCollection<ModifierModel> modifiers)
	{
		MultiplayerUiMode mode = _mode;
		if ((uint)(mode - 1) <= 1u)
		{
			throw new InvalidOperationException("This should only be called in client or load mode!");
		}
		foreach (NRunModifierTickbox tickbox in _modifierTickboxes)
		{
			tickbox.IsTicked = modifiers.FirstOrDefault((ModifierModel m) => m.IsEquivalent(tickbox.Modifier)) != null;
		}
	}

	/// <summary>
	/// Ticks on exactly the given modifiers, unticking all others, then notifies listeners so the change is
	/// propagated to the lobby (and synced on to any clients).
	/// Only valid in host or singleplayer mode. Clients receive modifier changes through <see cref="M:MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NCustomRunModifiersList.SyncModifierList(System.Collections.Generic.IReadOnlyCollection{MegaCrit.Sts2.Core.Models.ModifierModel})" />.
	/// </summary>
	/// <param name="modifiers">The modifiers to display as ticked. Assumed to already respect mutual exclusivity.</param>
	public void SetTickedModifiers(IReadOnlyCollection<ModifierModel> modifiers)
	{
		MultiplayerUiMode mode = _mode;
		if ((uint)(mode - 3) <= 1u)
		{
			throw new InvalidOperationException("This should only be called in host or singleplayer mode!");
		}
		foreach (NRunModifierTickbox tickbox in _modifierTickboxes)
		{
			tickbox.IsTicked = modifiers.Any((ModifierModel m) => m.IsEquivalent(tickbox.Modifier));
		}
		EmitSignal(SignalName.ModifiersChanged);
	}

	private IEnumerable<ModifierModel> GetAllModifiers()
	{
		foreach (ModifierModel item in ModelDb.GoodModifiers.Concat(ModelDb.BadModifiers))
		{
			if (item is CharacterCards canonicalCharacterCardsModifier)
			{
				foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
				{
					CharacterCards characterCards = (CharacterCards)canonicalCharacterCardsModifier.ToMutable();
					characterCards.CharacterModel = allCharacter.Id;
					yield return characterCards;
				}
			}
			else
			{
				yield return item.ToMutable();
			}
		}
	}

	private void UntickMutuallyExclusiveModifiersForTickbox(NRunModifierTickbox tickbox)
	{
		if (!tickbox.IsTicked)
		{
			return;
		}
		IReadOnlySet<ModifierModel> readOnlySet = ModelDb.MutuallyExclusiveModifiers.FirstOrDefault((IReadOnlySet<ModifierModel> s) => s.Any((ModifierModel m) => m.GetType() == tickbox.Modifier.GetType()));
		if (readOnlySet == null)
		{
			return;
		}
		foreach (NRunModifierTickbox otherTickbox in _modifierTickboxes)
		{
			if (!(otherTickbox.Modifier.GetType() == tickbox.Modifier.GetType()) && readOnlySet.Any((ModifierModel m) => m.GetType() == otherTickbox.Modifier.GetType()))
			{
				otherTickbox.IsTicked = false;
			}
		}
	}

	private void AfterModifiersChanged(NRunModifierTickbox tickbox)
	{
		UntickMutuallyExclusiveModifiersForTickbox(tickbox);
		EmitSignal(SignalName.ModifiersChanged);
	}

	/// <summary>
	/// Returns the list of modifiers that are ticked on.
	/// </summary>
	public List<ModifierModel> GetModifiersTickedOn()
	{
		return (from t in _modifierTickboxes
			where t.IsTicked
			select t.Modifier).ToList();
	}
}
