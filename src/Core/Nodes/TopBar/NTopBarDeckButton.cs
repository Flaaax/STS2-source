using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;

namespace MegaCrit.Sts2.Core.Nodes.TopBar;

public partial class NTopBarDeckButton : NTopBarButton
{
	private static readonly StringName _v = new StringName("v");

	private float _elapsedTime;

	private const float _rockSpeed = 4f;

	private const float _rockDist = 0.12f;

	private float _rockBaseRotation;

	private const float _defaultV = 0.9f;

	private Player _player;

	private CardPile _pile;

	private MegaLabel _countLabel;

	private float _count;

	private Tween? _bumpTween;

	protected override string[] Hotkeys => new string[1] { MegaInput.viewDeckAndTabLeft };

	public override void _Ready()
	{
		InitTopBarButton();
		_countLabel = GetNode<MegaLabel>("DeckCardCount");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		_pile.CardAddFinished -= OnPileContentsChanged;
		_pile.CardRemoveFinished -= OnPileContentsChanged;
	}

	public void Initialize(Player player)
	{
		_player = player;
		_pile = PileType.Deck.GetPile(player);
		_pile.CardAddFinished += OnPileContentsChanged;
		_pile.CardRemoveFinished += OnPileContentsChanged;
		OnPileContentsChanged();
	}

	private void OnPileContentsChanged()
	{
		int count = _pile.Cards.Count;
		if ((float)count > _count)
		{
			_bumpTween?.Kill();
			_bumpTween = CreateTween();
			_bumpTween.TweenProperty(_countLabel, "scale", Vector2.One, 0.5).From(Vector2.One * 1.5f).SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Expo);
			_countLabel.PivotOffset = _countLabel.Size * 0.5f;
			_count = count;
		}
		_countLabel.SetTextAutoSize(count.ToString());
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (IsOpen())
		{
			NCapstoneContainer.Instance.Close();
		}
		else
		{
			NDeckViewScreen.ShowScreen(_player);
		}
		UpdateScreenOpen();
		_hsv?.SetShaderParameter(_v, 0.9f);
	}

	protected override bool IsOpen()
	{
		return NCapstoneContainer.Instance.CurrentCapstoneScreen is NDeckViewScreen;
	}

	/// <summary>
	/// Animates rocking back and forth
	/// </summary>
	/// <param name="delta"></param>
	public override void _Process(double delta)
	{
		if (base.IsScreenOpen)
		{
			_elapsedTime += (float)delta * 4f;
			_icon.Rotation = _rockBaseRotation + 0.12f * Mathf.Sin(_elapsedTime);
			_rockBaseRotation = (float)Mathf.Lerp(_rockBaseRotation, 0.0, delta);
		}
	}

	/// <summary>
	/// Toggles the anim state of this button.
	/// Utilized when an external UI (ie BackButton) closes this screen.
	/// </summary>
	public void ToggleAnimState()
	{
		UpdateScreenOpen();
	}

	protected override void OnFocus()
	{
		base.OnFocus();
		LocString locString = new LocString("static_hover_tips", "DECK.title");
		locString.Add("Hotkey", NInputManager.Instance.GetShortcutKey(MegaInput.viewDeckAndTabLeft).ToString());
		HoverTip hoverTip = new HoverTip(locString, new LocString("static_hover_tips", "DECK.description"));
		NHoverTipSet nHoverTipSet = NHoverTipSet.CreateAndShow(this, hoverTip);
		nHoverTipSet?.SetGlobalPosition(base.GlobalPosition + new Vector2(base.Size.X - nHoverTipSet.Size.X, base.Size.Y + 20f));
	}

	protected override void OnUnfocus()
	{
		base.OnUnfocus();
		NHoverTipSet.Remove(this);
	}
}
