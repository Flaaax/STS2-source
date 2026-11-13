using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.Vfx.Ui;

public partial class NChainVisualization : Control
{
	private enum State
	{
		None,
		Default,
		Breaking,
		Broken
	}

	private static readonly string _linkPath = SceneHelper.GetScenePath("screens/dread/chain_link");

	private static readonly string _breakSparkPath = SceneHelper.GetScenePath("screens/dread/chain_break_vfx");

	private static readonly string _linkCreateVfxPath = SceneHelper.GetScenePath("screens/dread/chain_link_create_vfx");

	private static readonly string[] _brokenLinkPaths = new string[4]
	{
		ImageHelper.GetImagePath("ui/chain_break_1.png"),
		ImageHelper.GetImagePath("ui/chain_break_2.png"),
		ImageHelper.GetImagePath("ui/chain_break_3.png"),
		ImageHelper.GetImagePath("ui/chain_break_4.png")
	};

	private static readonly float[] _blendWeights = new float[7] { 1f, 1f, 2f, 6f, 2f, 1f, 1f };

	private const float _stretchedLinkSeparation = 85f;

	private const float _defaultLinkSeparation = 70f;

	private const float _maxPerturb = 40f;

	private const float _maxLinkYOffset = 15f;

	private const float _linkMinScale = 0.8f;

	private const float _linkMaxScaleChange = 0.1f;

	private const float _xPosSpeed = 0.12f;

	private const float _yPosSpeed = 0.08f;

	private const float _scaleSpeed = 0.2f;

	private const float _minBreakStretchTime = 0.1f;

	private const float _maxBreakStretchTime = 1f;

	private const int _maxBreakStretchLinks = 5;

	private Texture2D _originalLinkImage;

	private float _blendWeightSum;

	private readonly List<Control> _links = new List<Control>();

	private double _time;

	private double _breakStartTime = -1.0;

	private FastNoiseLite _noise = new FastNoiseLite();

	private FastNoiseLite _lowFreqNoise = new FastNoiseLite();

	private float _xOffset;

	private readonly List<float> _samples = new List<float>();

	private readonly List<float> _blendedSamples = new List<float>();

	private readonly List<Vector2> _points = new List<Vector2>();

	private int _windupSoundId = -1;

	private State _state = State.Default;

	private TaskCompletionSource? _breakCompletedSource;

	private TaskCompletionSource? _breakFadeOutCompletedSource;

	private OwnedTween _tween;

	public bool IsBreaking
	{
		get
		{
			if (_state != State.Breaking)
			{
				return _state == State.Broken;
			}
			return true;
		}
	}

	public override void _Ready()
	{
		_blendWeightSum = _blendWeights.Sum();
		_noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_noise.Frequency = 1f;
		_lowFreqNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		_lowFreqNoise.Frequency = 1f;
		_lowFreqNoise.FractalOctaves = 8;
		_tween = new OwnedTween(this);
	}

	public void Initialize(int count)
	{
		CancelPendingBreak();
		_state = State.Default;
		_breakStartTime = -1.0;
		_points.Clear();
		foreach (Control link in _links)
		{
			link.QueueFreeSafely();
		}
		_links.Clear();
		for (int i = 0; i < count; i++)
		{
			Control control = PreloadManager.Cache.GetScene(_linkPath).Instantiate<Control>(PackedScene.GenEditState.Disabled);
			this.AddChildSafely(control);
			_links.Add(control);
			_originalLinkImage = control.GetNode<TextureRect>("TextureRect").Texture;
		}
		_xOffset = (float)(-_links.Count) * 70f * 0.5f;
	}

	public Task Break(float delay)
	{
		CancelPendingBreak();
		_state = State.Default;
		Update();
		_breakStartTime = _time + (double)delay;
		_breakCompletedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_breakFadeOutCompletedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		return _breakCompletedSource.Task;
	}

	/// <summary>
	/// Abandons a break that has not finished. The caller awaiting it sees an
	/// <see cref="T:System.OperationCanceledException" />.
	/// </summary>
	private void CancelPendingBreak()
	{
		_breakCompletedSource?.TrySetCanceled();
		_breakFadeOutCompletedSource?.TrySetCanceled();
		_tween.Kill();
		StopWindupSound();
	}

	/// <summary>
	/// <see cref="M:MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Stop(System.Int32,System.Single)" /> warns about ids it cannot find, so only stop a live windup.
	/// </summary>
	private void StopWindupSound()
	{
		if (_windupSoundId >= 0)
		{
			NDebugAudioManager.Instance.Stop(_windupSoundId, 0.1f);
			_windupSoundId = -1;
		}
	}

	public async Task AnimateInLastLink(float delay, TaskCompletionSource? animateInCompleteSource)
	{
		if (_links.Count == 0)
		{
			animateInCompleteSource?.SetResult();
			return;
		}
		List<Control> links = _links;
		Control link = links[links.Count - 1];
		Color originalColor = link.Modulate;
		link.Modulate = Colors.Transparent;
		Control control = PreloadManager.Cache.GetScene(_linkCreateVfxPath).Instantiate<Control>(PackedScene.GenEditState.Disabled);
		link.AddChildSafely(control);
		control.Position = Vector2.Zero;
		control.Size = link.Size;
		TextureRect additive = control.GetNode<TextureRect>("%Additive");
		GpuParticles2D shineVfx = control.GetNode<GpuParticles2D>("%ShineVfx");
		GpuParticles2D[] sparkVfx = control.GetNode("%Spark").GetChildren().OfType<GpuParticles2D>()
			.ToArray();
		additive.Modulate = Colors.Transparent;
		await _tween.Play(delegate(Tween tween)
		{
			tween.TweenInterval(delay);
			if (_links.Count > 1)
			{
				float xOffset = _xOffset;
				SetXOffset((float)(-(_links.Count - 1)) * 70f * 0.5f);
				tween.TweenMethod(Callable.From<float>(SetXOffset), _xOffset, xOffset, 0.5).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Expo);
				tween.Parallel().TweenCallback(Callable.From(() => NDebugAudioManager.Instance.Play("chain_get.wav"))).SetDelay(0.30000001192092896);
			}
			else
			{
				tween.TweenCallback(Callable.From(() => NDebugAudioManager.Instance.Play("chain_get.wav")));
				tween.TweenInterval(0.20000000298023224);
			}
			tween.TweenCallback(Callable.From(delegate
			{
				link.Modulate = originalColor;
				additive.Modulate = Colors.White;
				GpuParticles2D[] array = sparkVfx;
				foreach (GpuParticles2D gpuParticles2D in array)
				{
					gpuParticles2D.Emitting = true;
				}
				animateInCompleteSource?.SetResult();
			}));
			tween.TweenProperty(additive, "self_modulate:a", 0f, 0.3).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Expo);
			tween.TweenCallback(Callable.From(delegate
			{
				if (shineVfx.IsValid())
				{
					shineVfx.Emitting = true;
				}
			}));
		});
		animateInCompleteSource?.TrySetResult();
	}

	private void SetXOffset(float xOffset)
	{
		_xOffset = xOffset;
	}

	public void Reset()
	{
		_state = State.Default;
		foreach (Control link in _links)
		{
			link.GetNode<TextureRect>("TextureRect").Texture = _originalLinkImage;
			Color modulate = link.Modulate;
			modulate.A = 1f;
			link.Modulate = modulate;
		}
	}

	public override void _Process(double delta)
	{
		_time += delta;
		if (_state == State.Default && _breakStartTime >= 0.0 && _time >= _breakStartTime)
		{
			_state = State.Breaking;
			_windupSoundId = NDebugAudioManager.Instance.Play("chain_break_windup.wav");
		}
		if (_state == State.Breaking)
		{
			BreakingUpdate();
		}
		else if (_state == State.Default)
		{
			Update();
		}
	}

	private void Update()
	{
		_samples.Clear();
		_blendedSamples.Clear();
		_points.Clear();
		if (_links.Count == 0)
		{
			return;
		}
		float item = (_lowFreqNoise.GetNoise3D((float)_time * 0.12f, 0f, 0f) + 1f) * 0.5f;
		_samples.Add(item);
		for (int i = 0; i < _links.Count - 1; i++)
		{
			float noise3D = _lowFreqNoise.GetNoise3D((float)_time * 0.12f, (i + 1) * 7, i + 1);
			_samples.Add(noise3D);
		}
		float item2 = (0f - (_lowFreqNoise.GetNoise3D((float)_time * 0.12f, (_links.Count + 1) * 7, _links.Count + 1) + 1f)) * 0.5f;
		_samples.Add(item2);
		for (int j = 0; j < _samples.Count; j++)
		{
			float num = 0f;
			float num2 = 0f;
			int num3 = _blendWeights.Length / 2;
			for (int k = Mathf.Max(j - num3, 0); k <= Mathf.Min(j + num3, _samples.Count - 1); k++)
			{
				int num4 = k - j + num3;
				float num5 = _blendWeights[num4] / _blendWeightSum;
				num += num5 * _samples[k];
				num2 += num5;
			}
			_blendedSamples.Add(num / num2);
		}
		for (int l = 0; l < _blendedSamples.Count; l++)
		{
			float num6 = _blendedSamples[l];
			float num7 = (float)l * 70f;
			float noise3D2 = _noise.GetNoise3D((float)_time * 0.08f, l * 41, l * 9);
			_points.Add(new Vector2(num7 + 40f * num6, noise3D2 * 15f));
		}
		for (int m = 0; m < _points.Count - 1; m++)
		{
			Control control = _links[m];
			Vector2 vector = _points[m];
			Vector2 vector2 = _points[m + 1];
			control.Position = _xOffset * Vector2.Right + (vector + vector2) * 0.5f - control.Size * 0.5f;
			Vector2 vector3 = vector2 - vector;
			control.Rotation = Mathf.Atan2(vector3.Y, vector3.X);
			float num8 = _noise.GetNoise3D((float)_time * 0.2f, m * 63, m * 13) * 0.5f;
			if (m % 2 == 0)
			{
				control.Scale = new Vector2(1f, 0.8f + num8 * 0.1f);
			}
		}
	}

	private void BreakingUpdate()
	{
		double num = _time - _breakStartTime;
		float num2 = Ease.SineOut(Mathf.Clamp(Mathf.Lerp(0.1f, 1f, (float)_links.Count / 5f), 0f, 1f));
		if (num >= (double)num2)
		{
			_breakCompletedSource?.TrySetResult();
			TaskHelper.RunSafely(BrokenTween());
			_state = State.Broken;
			return;
		}
		float weight = Ease.SineOut((float)num / num2);
		float num3 = Ease.QuadIn(Mathf.Clamp((float)num / 0.7f, 0f, 1f));
		Vector2 vector = -new Vector2((float)_links.Count * 85f, 0f) * 0.5f;
		for (int i = 0; i < _points.Count - 1; i++)
		{
			Control control = _links[i];
			Vector2 vector2 = _points[i];
			Vector2 vector3 = _points[i + 1];
			Vector2 vector4 = _xOffset * Vector2.Right + (vector2 + vector3) * 0.5f - control.Size * 0.5f;
			Vector2 vector5 = (float)i * 85f * Vector2.Right;
			Vector2 vector6 = (float)(i + 1) * 85f * Vector2.Right;
			Vector2 to = vector + (vector5 + vector6) * 0.5f - control.Size * 0.5f;
			Vector2 vector7 = num3 * Vector2.Right.Rotated((float)Math.PI * 2f * Rng.Chaotic.NextFloat()) * 2f;
			Vector2 position = vector4.Lerp(to, weight) + vector7;
			Vector2 vector8 = vector3 - vector2;
			float num4 = Mathf.Atan2(vector8.Y, vector8.X);
			float to2 = 0f;
			control.Position = position;
			control.Rotation = Mathf.Lerp(num4, to2, weight);
		}
	}

	private async Task BrokenTween()
	{
		PackedScene sparkScene = PreloadManager.Cache.GetScene(_breakSparkPath);
		StopWindupSound();
		NDebugAudioManager.Instance.Play("chain_break.wav");
		await _tween.Play(delegate(Tween tween)
		{
			tween.SetParallel();
			for (int i = 0; i < _links.Count; i++)
			{
				Control control = _links[i];
				float num = ((float)i - (float)_links.Count * 0.5f) * 0.2f;
				Vector2 vector = Vector2.Right.Rotated(Rng.Chaotic.NextFloat((float)Math.PI * 2f));
				vector = (vector + Vector2.Right * num).Normalized();
				Node2D node2D = sparkScene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
				this.AddChildSafely(node2D);
				node2D.Position = control.Position;
				node2D.Rotation = Mathf.Atan2(vector.Y, vector.X) + (float)Math.PI / 2f;
				node2D.GetNode<GpuParticles2D>("Vfx").Emitting = true;
				float num2 = Rng.Chaotic.NextFloat((float)Math.PI * 2f);
				Vector2 vector2 = control.Position + vector * Rng.Chaotic.NextFloat(200f, 300f);
				control.GetNode<TextureRect>("TextureRect").Texture = PreloadManager.Cache.GetTexture2D(Rng.Chaotic.NextItem(_brokenLinkPaths));
				tween.TweenProperty(control, "rotation", num2, 1.4).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
				tween.TweenProperty(control, "position", vector2, 1.4).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
				tween.TweenProperty(control, "modulate:a", 0f, 0.3).SetDelay(Rng.Chaotic.NextFloat(0.5f, 0.7f));
			}
		});
		_breakFadeOutCompletedSource?.TrySetResult();
	}
}
