using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

public partial class NBlitzTopBarMap : Control
{
	private const float _pointSize = 35f;

	private const float _pointSpacing = 35f;

	private const float _neighborPadding = 25f;

	private readonly List<NBlitzTopBarMapPoint> _mapPoints = new List<NBlitzTopBarMapPoint>();

	private IRunState _runState;

	/// <summary>
	/// Marker which points to the current map point.
	/// </summary>
	private NMapMarker _marker;

	/// <summary>
	/// Container which is sized to exactly contain all the map points.
	/// This is repositioned to the center of the top bar, or moved around as needed to avoid running into other elements.
	/// </summary>
	private Control _container;

	/// <summary>
	/// Control which contains all the dotted paths.
	/// </summary>
	private Control _pathContainer;

	/// <summary>
	/// Control which contains all the map points.
	/// </summary>
	private Control _pointContainer;

	/// <summary>
	/// The top bar. Null until <see cref="M:MegaCrit.Sts2.Core.Nodes.CommonUi.NBlitzTopBarMap.Initialize(MegaCrit.Sts2.Core.Runs.IRunState,MegaCrit.Sts2.Core.Nodes.CommonUi.NTopBar)" />, which only Blitz runs call.
	/// </summary>
	private NTopBar? _topBar;

	public IReadOnlyList<NBlitzTopBarMapPoint> MapPoints => _mapPoints;

	public override void _Ready()
	{
		_container = GetNode<Control>("%BlitzMapContainer");
		_pathContainer = GetNode<Control>("%BlitzMapPaths");
		_pointContainer = GetNode<Control>("%BlitzMapPoints");
		_marker = GetNode<NMapMarker>("%BlitzMapMarker");
	}

	public void Initialize(IRunState runState, NTopBar topBar)
	{
		_runState = runState;
		_topBar = topBar;
		_topBar.LeftContainer.Connect(Container.SignalName.SortChildren, Callable.From(Reposition));
		_topBar.RightContainer.Connect(Container.SignalName.SortChildren, Callable.From(Reposition));
		_topBar.Timer.Connect(CanvasItem.SignalName.VisibilityChanged, Callable.From(Reposition));
		if (runState.GameMode != GameMode.Blitz)
		{
			throw new InvalidOperationException("Lightning top bar map should only used with Lightning mode!");
		}
		_marker.Initialize(LocalContext.GetMe(_runState));
		_marker.Invert();
		RunManager.Instance.MapChanged += OnMapChanged;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1)
		{
			RunManager.Instance.MapChanged -= OnMapChanged;
		}
		else if ((long)what == 40 && IsNodeReady())
		{
			TaskHelper.RunSafely(RepositionAfterOneFrame());
		}
	}

	private void OnMapChanged(bool clearDrawings)
	{
		SetMap(_runState.Map);
	}

	private void SetMap(ActMap actMap)
	{
		if (!(actMap is BlitzActMap blitzActMap))
		{
			throw new InvalidOperationException("Lightning top bar map should only used with Lightning mode!");
		}
		_mapPoints.Clear();
		foreach (Node child in _pathContainer.GetChildren())
		{
			child.QueueFreeSafely();
		}
		foreach (Node child2 in _pointContainer.GetChildren())
		{
			child2.QueueFreeSafely();
		}
		foreach (MapPoint mapPoint in blitzActMap.GetMapPoints())
		{
			NBlitzTopBarMapPoint nBlitzTopBarMapPoint = NBlitzTopBarMapPoint.Create(mapPoint, _runState);
			_pointContainer.AddChildSafely(nBlitzTopBarMapPoint);
			_mapPoints.Add(nBlitzTopBarMapPoint);
		}
		float x = (float)_mapPoints.Count * 35f + (float)(_mapPoints.Count - 1) * 35f;
		_container.Size = new Vector2(x, base.Size.Y);
		for (int i = 0; i < _mapPoints.Count; i++)
		{
			NBlitzTopBarMapPoint nBlitzTopBarMapPoint2 = _mapPoints[i];
			nBlitzTopBarMapPoint2.Position = (float)i * 70f * Vector2.Right + (base.Size.Y - nBlitzTopBarMapPoint2.Size.Y) * Vector2.Down * 0.5f;
			if (i > 0)
			{
				NBlitzTopBarMapPoint nBlitzTopBarMapPoint3 = _mapPoints[i - 1];
				NMapScreen.CreatePath(nBlitzTopBarMapPoint3.Position + nBlitzTopBarMapPoint3.Size * 0.5f, nBlitzTopBarMapPoint2.Position + nBlitzTopBarMapPoint2.Size * 0.5f, _pathContainer, StsColors.halfTransparentBlack);
			}
		}
		Reposition();
		IReadOnlyList<MapCoord> visitedMapCoords = _runState.VisitedMapCoords;
		MapCoord mapCoord;
		if (visitedMapCoords.Count <= 0)
		{
			mapCoord = blitzActMap.StartingMapPoint.coord;
		}
		else
		{
			mapCoord = visitedMapCoords[visitedMapCoords.Count - 1];
		}
		MapCoord currentCoord = mapCoord;
		NBlitzTopBarMapPoint nBlitzTopBarMapPoint4 = _mapPoints.FirstOrDefault((NBlitzTopBarMapPoint p) => p.Point.coord == currentCoord);
		if (nBlitzTopBarMapPoint4 != null)
		{
			SetMapNodeAsCurrent(nBlitzTopBarMapPoint4);
		}
	}

	public async Task RepositionAfterOneFrame()
	{
		await this.AwaitProcessFrame();
		Reposition();
	}

	private void Reposition()
	{
		if (_topBar == null)
		{
			return;
		}
		_container.Scale = Vector2.One;
		_container.Position = (base.Size - _container.Size) * 0.5f;
		float num = _topBar.LeftContainer.GlobalPosition.X + _topBar.LeftContainer.GetMinimumSize().X + 25f;
		if (!(num < _container.Position.X))
		{
			Control container = _container;
			Vector2 position = _container.Position;
			position.X = num;
			container.Position = position;
			float num2 = ((!_topBar.Timer.Visible) ? (_topBar.Deck.GlobalPosition.X - 25f) : (_topBar.Timer.GlobalPosition.X - 25f));
			if (!(_container.Position.X + _container.Size.X < num2))
			{
				_container.Scale = (num2 - num) / _container.Size.X * Vector2.One;
				Control container2 = _container;
				position = _container.Position;
				position.Y = (base.Size.Y - _container.Size.Y * _container.Scale.Y) * 0.5f;
				container2.Position = position;
			}
		}
	}

	public async Task TravelToMapCoord(MapCoord coord)
	{
		NBlitzTopBarMapPoint node = _mapPoints.FirstOrDefault((NBlitzTopBarMapPoint p) => p.Point.coord == coord);
		if (node != null)
		{
			SfxCmd.Play("event:/sfx/ui/map/map_select");
			SfxCmd.Play("event:/sfx/ui/wipe_map");
			SetMapNodeAsCurrent(node);
			node.ShowCircleVfx(playAnim: true);
		}
		await TaskHelper.RunSafely(RunManager.Instance.FadeOut());
		await RunManager.Instance.EnterMapCoord(coord);
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			node.HideCircleVfx();
		}
		TaskHelper.RunSafely(RunManager.Instance.FadeIn());
	}

	private void SetMapNodeAsCurrent(NBlitzTopBarMapPoint node)
	{
		_marker.HideMapPoint();
		_marker.SetMapPoint(node);
		NMapNodeSelectVfx nMapNodeSelectVfx = NMapNodeSelectVfx.Create(1f);
		node.AddChildSafely(nMapNodeSelectVfx);
		nMapNodeSelectVfx.Position += node.PivotOffset;
	}
}
