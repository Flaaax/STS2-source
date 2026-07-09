using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Timeline;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Timeline;

/// <summary>
/// Represents a single column in the Timeline.
/// Node which has a list of NEpochSlots, an Era icon, its name, and year.
/// </summary>
public partial class NEraColumn : Control
{
	private const string _scenePath = "res://scenes/timeline_screen/era_column.tscn";

	public static readonly IEnumerable<string> assetPaths;

	private TextureRect _icon;

	private MegaLabel _name;

	private MegaLabel _year;

	private Tween _iconTween;

	private Tween _labelTween;

	private bool _labelSpawned;

	public EpochEra era;

	private Vector2 _prevLocalPos;

	private Vector2 _prevGlobalPos;

	private Vector2 _predictedPosition;

	private Vector2 _targetPosition;

	private bool _isAnimated;

	private EpochSlotData _data;

	public static NEraColumn Create(EpochSlotData data)
	{
		NEraColumn nEraColumn = PreloadManager.Cache.GetScene("res://scenes/timeline_screen/era_column.tscn").Instantiate<NEraColumn>(PackedScene.GenEditState.Disabled);
		nEraColumn._data = data;
		return nEraColumn;
	}

	public override void _Ready()
	{
		_icon = GetNode<TextureRect>("%Icon");
		_name = GetNode<MegaLabel>("%Name");
		_year = GetNode<MegaLabel>("%Year");
		era = _data.Era;
		SetName(_data.Era.ToString());
		Init(_data);
		base.ItemRectChanged += RectChange;
	}

	/// <summary>
	/// Creates this Era Column and also the first slot for it
	/// </summary>
	public void Init(EpochSlotData epochSlot)
	{
		(Texture2D, string) eraIcon = NTimelineScreen.GetEraIcon(epochSlot.Era);
		_icon.Texture = eraIcon.Item1;
		_name.SetTextAutoSize(new LocString("eras", eraIcon.Item2 + ".name").GetFormattedText());
		_year.SetTextAutoSize(new LocString("eras", eraIcon.Item2 + ".year").GetFormattedText());
		AddSlot(epochSlot);
	}

	public void AddSlot(EpochSlotData epochSlotData)
	{
		NEpochSlot nEpochSlot = NEpochSlot.Create(epochSlotData);
		this.AddChildSafely(nEpochSlot);
		nEpochSlot.Name = $"Slot{epochSlotData.EraPosition}";
		this.MoveChildSafely(nEpochSlot, 0);
	}

	public void SpawnIcon()
	{
		_iconTween = CreateTween().SetParallel();
		_iconTween.TweenProperty(_icon, "modulate:a", 1f, 0.5);
		_iconTween.TweenProperty(_icon, "scale", Vector2.One, 0.5).From(Vector2.One * 0.1f).SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Back);
	}

	/// <summary>
	/// Epoch slot pop-up effect!
	/// </summary>
	public async Task SpawnSlots(bool isAnimated)
	{
		foreach (Node child in GetChildren())
		{
			if (child is NEpochSlot { HasSpawned: false } nEpochSlot)
			{
				if (isAnimated)
				{
					await nEpochSlot.SpawnSlot();
				}
				else
				{
					TaskHelper.RunSafely(nEpochSlot.SpawnSlot());
				}
			}
		}
	}

	public async Task SpawnNameAndYear()
	{
		if (!_labelSpawned)
		{
			_labelSpawned = true;
			_labelTween = CreateTween().SetParallel();
			_name.SelfModulate = new Color(_name.SelfModulate.R, _name.SelfModulate.G, _name.SelfModulate.B, 0f);
			_year.Modulate = new Color(_year.Modulate.R, _year.Modulate.G, _year.Modulate.B, 0f);
			_labelTween.TweenProperty(_name, "self_modulate:a", 1f, 1.0);
			_labelTween.TweenProperty(_name, "position:y", 28f, 1.0).From(-36f).SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);
			_labelTween.TweenProperty(_year, "modulate:a", 1f, 1.0).SetDelay(0.5);
			_labelTween.TweenProperty(_year, "position:y", 20f, 1.0).SetDelay(0.5).From(0f)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);
			await _labelTween.AwaitFinished(this);
			await Task.Delay(500);
		}
	}

	public async Task SaveBeforeAnimationPosition()
	{
		_isAnimated = true;
		_prevLocalPos = base.Position;
		_prevGlobalPos = base.GlobalPosition;
		await this.AwaitProcessFrame();
		_isAnimated = false;
		_targetPosition = _predictedPosition;
		base.GlobalPosition = _prevGlobalPos;
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(this, "position", _targetPosition, 2.0).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Cubic);
	}

	/// <summary>
	/// Cache the position of where we predict the Position this column will be.
	/// There's no way to forcefully refresh the layout in Godot so PredictHBoxLayout()
	/// was created and that passes in the expected position.
	/// </summary>
	public void SetPredictedPosition(Vector2 setPredictedPosition)
	{
		if (_isAnimated)
		{
			_predictedPosition = setPredictedPosition;
		}
	}

	/// <summary>
	/// Behold the hack. Every time this Node's size is updated we force the position to be where it should.
	/// Note that this is called several times per frame as many children are added to the list of columns
	/// so the _isAnimated bool is actually disabled next frame.
	/// </summary>
	private void RectChange()
	{
		if (_isAnimated)
		{
			base.GlobalPosition = _prevGlobalPos;
		}
	}

	public override void _ExitTree()
	{
		base.ItemRectChanged -= RectChange;
	}

	static NEraColumn()
	{
		List<string> list = new List<string>();
		list.Add("res://scenes/timeline_screen/era_column.tscn");
		list.AddRange(NEpochSlot.assetPaths);
		assetPaths = new _003C_003Ez__ReadOnlyList<string>(list);
	}
}
