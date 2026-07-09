using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Random;

namespace MegaCrit.Sts2.Core.Nodes.CommonUi;

/// <summary>
/// A card preview container intended to be used when lots and lots of cards are previewed and the player isn't
/// meant to be able to parse every single card. Example usage in the Reflections event
/// </summary>
public partial class NMessyCardPreviewContainer : Control
{
	public class PoissonDiscSampler
	{
		/// Helper struct to calculate the x and y indices of a sample in the grid
		private struct GridPos
		{
			public readonly int x;

			public readonly int y;

			public GridPos(Vector2 sample, float cellSize)
			{
				x = (int)(sample.X / cellSize);
				y = (int)(sample.Y / cellSize);
			}
		}

		private const int _maxAttempts = 30;

		private readonly Rect2 _rect;

		private readonly float _radius2;

		private readonly float _cellSize;

		private readonly Vector2[,] _grid;

		private readonly List<Vector2> _activeSamples = new List<Vector2>();

		/// Create a sampler with the following parameters:
		///
		/// width:  each sample's x coordinate will be between [0, width]
		/// height: each sample's y coordinate will be between [0, height]
		/// radius: each sample will be at least `radius` units away from any other sample, and at most 2 * `radius`.
		public PoissonDiscSampler(float width, float height, float radius)
		{
			_rect = new Rect2(0f, 0f, width, height);
			_radius2 = radius * radius;
			_cellSize = radius / Mathf.Sqrt(2f);
			_grid = new Vector2[Mathf.CeilToInt(width / _cellSize), Mathf.CeilToInt(height / _cellSize)];
		}

		/// Return a lazy sequence of samples.
		public IEnumerator<Vector2> Samples()
		{
			yield return AddSample(_rect.Size / 2f);
			while (_activeSamples.Count > 0)
			{
				int i = (int)(Rng.Chaotic.NextFloat() * (float)_activeSamples.Count);
				Vector2 vector = _activeSamples[i];
				bool found = false;
				for (int j = 0; j < 30; j++)
				{
					float s = (float)Math.PI * 2f * Rng.Chaotic.NextFloat();
					float num = Mathf.Sqrt(Rng.Chaotic.NextFloat() * 3f * _radius2 + _radius2);
					Vector2 vector2 = vector + num * new Vector2(Mathf.Cos(s), Mathf.Sin(s));
					if (_rect.HasPoint(vector2) && IsFarEnough(vector2))
					{
						found = true;
						yield return AddSample(vector2);
						break;
					}
				}
				if (!found)
				{
					_activeSamples[i] = _activeSamples[_activeSamples.Count - 1];
					_activeSamples.RemoveAt(_activeSamples.Count - 1);
				}
			}
		}

		private bool IsFarEnough(Vector2 sample)
		{
			GridPos gridPos = new GridPos(sample, _cellSize);
			int num = Mathf.Max(gridPos.x - 2, 0);
			int num2 = Mathf.Max(gridPos.y - 2, 0);
			int num3 = Mathf.Min(gridPos.x + 2, _grid.GetLength(0) - 1);
			int num4 = Mathf.Min(gridPos.y + 2, _grid.GetLength(1) - 1);
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					Vector2 vector = _grid[j, i];
					if (vector != Vector2.Zero)
					{
						Vector2 vector2 = vector - sample;
						if (vector2.X * vector2.X + vector2.Y * vector2.Y < _radius2)
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		/// Adds the sample to the active samples queue and the grid before returning it
		private Vector2 AddSample(Vector2 sample)
		{
			_activeSamples.Add(sample);
			GridPos gridPos = new GridPos(sample, _cellSize);
			_grid[gridPos.x, gridPos.y] = sample;
			return sample;
		}
	}

	private const float _spacing = 150f;

	private const int _resetNewCardMsec = 2000;

	private ulong _resetNewCardTimer;

	private float _currentMaxPosition;

	private IEnumerator<Vector2>? _samples;

	public override void _Ready()
	{
		Connect(Node.SignalName.ChildEnteredTree, Callable.From<Node>(PositionNewChild));
	}

	private void PositionNewChild(Node node)
	{
		if (Time.GetTicksMsec() > _resetNewCardTimer)
		{
			_currentMaxPosition = 0f;
			ResetSamples();
		}
		_resetNewCardTimer = Time.GetTicksMsec() + 2000;
		if (!_samples.MoveNext())
		{
			ResetSamples();
		}
		Vector2 current = _samples.Current;
		if (node is Control control)
		{
			control.Position = current;
		}
		else if (node is Node2D node2D)
		{
			node2D.Position = current;
		}
	}

	private void ResetSamples()
	{
		PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(base.Size.X, base.Size.Y, 150f);
		_samples = poissonDiscSampler.Samples();
	}
}
