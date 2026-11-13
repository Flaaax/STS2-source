using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// A node in the blitz map shown in the top bar.
/// Since it is not a real map point (i.e. not shown in NMapScreen), it doesn't inherit from NMapPoint.
/// </summary>
public partial class NBlitzTopBarMapPoint : NClickableControl
{
	private static readonly string _scenePath = SceneHelper.GetScenePath("/ui/blitz_map_point");

	private const float _minCircleScale = 0.65f;

	private const float _maxCircleScale = 0.7f;

	private TextureRect _icon;

	private TextureRect _outline;

	private NSelectionReticle _controllerSelectionReticle;

	private MapPoint _mapPoint;

	private IRunState _runState;

	private NMapCircleVfx? _circleVfx;

	public MapPoint Point => _mapPoint;

	public static NBlitzTopBarMapPoint Create(MapPoint point, IRunState runState)
	{
		NBlitzTopBarMapPoint nBlitzTopBarMapPoint = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NBlitzTopBarMapPoint>(PackedScene.GenEditState.Disabled);
		nBlitzTopBarMapPoint._mapPoint = point;
		nBlitzTopBarMapPoint._runState = runState;
		return nBlitzTopBarMapPoint;
	}

	public override void _Ready()
	{
		ConnectSignals();
		_icon = GetNode<TextureRect>("%Icon");
		_outline = GetNode<TextureRect>("%Outline");
		_controllerSelectionReticle = GetNode<NSelectionReticle>("%SelectionReticle");
		MapPointType pointType = _mapPoint.PointType;
		if ((uint)(pointType - 7) <= 1u)
		{
			_icon.SetAnchorAndOffset(Side.Left, 0f, -10f);
			_icon.SetAnchorAndOffset(Side.Top, 0f, -10f);
			_icon.SetAnchorAndOffset(Side.Right, 1f, 10f);
			_icon.SetAnchorAndOffset(Side.Bottom, 1f, 10f);
		}
		UpdateIcon();
	}

	private void UpdateIcon()
	{
		MapPointType pointType = _mapPoint.PointType;
		if ((uint)(pointType - 7) <= 1u)
		{
			string roomIconPath;
			string roomIconOutlinePath;
			if (_mapPoint.PointType == MapPointType.Ancient)
			{
				roomIconPath = ImageHelper.GetRoomIconPath(_mapPoint.PointType, RoomType.Event, _runState.Act.Ancient.Id);
				roomIconOutlinePath = ImageHelper.GetRoomIconOutlinePath(_mapPoint.PointType, RoomType.Event, _runState.Act.Ancient.Id);
			}
			else
			{
				roomIconPath = ImageHelper.GetRoomIconPath(_mapPoint.PointType, RoomType.Boss, _runState.Act.BossEncounter.Id);
				roomIconOutlinePath = ImageHelper.GetRoomIconOutlinePath(_mapPoint.PointType, RoomType.Boss, _runState.Act.BossEncounter.Id);
			}
			_icon.Texture = ((roomIconPath == null) ? null : ResourceLoader.Load<Texture2D>(roomIconPath, null, ResourceLoader.CacheMode.Reuse));
			_outline.Texture = ((roomIconOutlinePath == null) ? null : ResourceLoader.Load<Texture2D>(roomIconOutlinePath, null, ResourceLoader.CacheMode.Reuse));
		}
		else if (_mapPoint.PointType != MapPointType.Unknown)
		{
			string filename = NNormalMapPoint.IconName(_mapPoint.PointType);
			string path = NNormalMapPoint.IconPath(filename);
			_icon.Texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
			string path2 = NNormalMapPoint.OutlinePath(NNormalMapPoint.IconName(_mapPoint.PointType));
			_outline.Texture = ResourceLoader.Load<Texture2D>(path2, null, ResourceLoader.CacheMode.Reuse);
		}
		else if (_runState.MapPointHistory.Count > _runState.CurrentActIndex)
		{
			IReadOnlyList<MapPointHistoryEntry> readOnlyList = _runState.MapPointHistory[_runState.CurrentActIndex];
			if (readOnlyList.Count > _mapPoint.coord.row)
			{
				RoomType roomType = readOnlyList[_mapPoint.coord.row].Rooms.First().RoomType;
				_icon.Texture = ResourceLoader.Load<Texture2D>(NNormalMapPoint.UnknownIconPath(roomType), null, ResourceLoader.CacheMode.Reuse);
				_outline.Texture = ResourceLoader.Load<Texture2D>(NNormalMapPoint.UnknownOutlinePath(roomType), null, ResourceLoader.CacheMode.Reuse);
			}
		}
	}

	public void ShowCircleVfx(bool playAnim)
	{
		if (_circleVfx == null)
		{
			_circleVfx = NMapCircleVfx.Create(_runState, _mapPoint.coord, playAnim, 0.65f, 0.7f);
			this.AddChildSafely(_circleVfx);
			_circleVfx.Position += base.PivotOffset;
		}
	}

	public void HideCircleVfx()
	{
		_circleVfx?.QueueFreeSafely();
		_circleVfx = null;
	}

	protected override void OnFocus()
	{
		if (_isEnabled && NControllerManager.Instance.IsUsingDirectionalNavigation)
		{
			_controllerSelectionReticle.OnSelect();
		}
		MapPointHistoryEntry historyEntryFor = _runState.GetHistoryEntryFor(new MapLocation(Point.coord, _runState.CurrentActIndex));
		NHoverTipSet nHoverTipSet;
		if (historyEntryFor != null && _runState.MapLocation.coord != Point.coord)
		{
			int num = Point.coord.row + 1;
			for (int i = 0; i < _runState.MapPointHistory.Count - 1; i++)
			{
				num += _runState.MapPointHistory[i].Count;
			}
			nHoverTipSet = NHoverTipSet.CreateAndShowMapPointHistory(this, NMapPointHistoryHoverTip.Create(num, LocalContext.NetId.Value, historyEntryFor, _runState.GameMode));
		}
		else
		{
			MapPointType pointType = Point.PointType;
			LocString locString = default(LocString);
			switch (pointType)
			{
			case MapPointType.Unassigned:
				throw new NotImplementedException();
			case MapPointType.Unknown:
				locString = new LocString("map", "LEGEND_UNKNOWN");
				break;
			case MapPointType.Shop:
				locString = new LocString("map", "LEGEND_MERCHANT");
				break;
			case MapPointType.Treasure:
				locString = new LocString("map", "LEGEND_TREASURE");
				break;
			case MapPointType.RestSite:
				locString = new LocString("map", "LEGEND_REST");
				break;
			case MapPointType.Monster:
				locString = new LocString("map", "LEGEND_ENEMY");
				break;
			case MapPointType.Elite:
				locString = new LocString("map", "LEGEND_ELITE");
				break;
			case MapPointType.Ancient:
				locString = new LocString("map", "LEGEND_ANCIENT");
				break;
			case MapPointType.Boss:
				locString = new LocString("map", "LEGEND_BOSS");
				break;
			default:
				throw new System.Runtime.CompilerServices.SwitchExpressionException(pointType);
				break;
			}
			LocString locString2 = locString;
			LocString title = new LocString(locString2.LocTable, locString2.LocEntryKey + ".hoverTip.title");
			LocString locString3 = new LocString(locString2.LocTable, locString2.LocEntryKey + ".hoverTip.description");
			HoverTip hoverTip;
			if (Point.PointType == MapPointType.Boss)
			{
				LocString locString4 = new LocString("static_hover_tips", "BOSS.description");
				locString4.Add("BossName", _runState.Act.BossEncounter.Title);
				hoverTip = new HoverTip(title, locString3.GetFormattedText() + "\n\n" + locString4.GetFormattedText());
			}
			else
			{
				hoverTip = new HoverTip(title, locString3);
			}
			nHoverTipSet = NHoverTipSet.CreateAndShow(this, hoverTip);
		}
		if (nHoverTipSet != null)
		{
			nHoverTipSet.GlobalPosition = new Vector2(base.GlobalPosition.X + base.Size.X * 0.5f - nHoverTipSet.Size.X * 0.5f, base.GlobalPosition.Y + base.Size.Y + 20f);
		}
	}

	protected override void OnUnfocus()
	{
		_controllerSelectionReticle.OnDeselect();
		NHoverTipSet.Remove(this);
	}
}
