using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using MegaCrit.Sts2.SourceGeneration;

namespace MegaCrit.Sts2.Core.Timeline;

/// <summary>
/// An abstract class which contains data for a single Epoch.
/// </summary>
[GenerateSubtypes(DynamicallyAccessedMemberTypes = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public abstract class EpochModel
{
	/// <summary>
	/// List of all valid epochs currently in the game
	/// </summary>
	private static readonly List<Type> _allEpochs;

	private static List<string>? _allEpochIds;

	private static HashSet<string>? _epochIdsHashSet;

	private string? _resolvedPortraitPath;

	private static readonly Dictionary<string, Type> _epochTypeDictionary;

	private static readonly Dictionary<Type, string> _typeToIdDictionary;

	public static IReadOnlyList<Type> AllEpochs => _allEpochs;

	public static IReadOnlyList<string> AllEpochIds => _allEpochIds ?? (_allEpochIds = _allEpochs.Select(GetId).ToList());

	public static IReadOnlySet<string> EpochIdsHashSet => _epochIdsHashSet ?? (_epochIdsHashSet = AllEpochIds.ToHashSet());

	public abstract string Id { get; }

	/// <summary>
	/// Not used as of this time except for the header of the hovertip when peeking at the unlock requirement.
	/// </summary>
	public LocString Title => new LocString("epochs", Id + ".title");

	/// <summary>
	/// The fancy lore text.
	/// </summary>
	public string Description => new LocString("epochs", Id + ".description").GetFormattedText();

	public string? StoryTitle
	{
		get
		{
			if (StoryId == null)
			{
				return null;
			}
			return new LocString("epochs", "STORY_" + StoryId.ToUpperInvariant()).GetRawText();
		}
	}

	public virtual string? StoryId => null;

	/// <summary>
	/// The text shown in the hovertip when you hover this Epoch while it's not yet obtained.
	/// </summary>
	public LocString UnlockInfo => new LocString("epochs", Id + ".unlockInfo");

	/// <summary>
	/// The text which shows up at the bottom of the Epoch Inspect Screen, describing what had been unlocked.
	/// </summary>
	public virtual string UnlockText => new LocString("epochs", Id + ".unlockText").GetFormattedText();

	public abstract EpochEra Era { get; }

	/// <summary>
	/// The "row" which this Era supposedly resides in. 0 is the bottom, 4 is the top!
	/// </summary>
	public abstract int EraPosition { get; }

	public Texture2D Portrait => ResourceLoader.Load<Texture2D>(PackedPortraitPath, null, ResourceLoader.CacheMode.Reuse);

	private string PackedPortraitPath => ImageHelper.GetImagePath("atlases/epoch_atlas.sprites/" + Id.ToLowerInvariant() + ".tres");

	public Texture2D RealPortrait => ResourceLoader.Load<Texture2D>(ResolvedPortraitPath, null, ResourceLoader.CacheMode.Reuse);

	public bool HasRealPortrait => ResourceLoader.Exists(RealPortraitPath);

	private string RealPortraitPath => ImageHelper.GetImagePath("timeline/epoch_portraits/" + Id.ToLowerInvariant() + ".png");

	private string PlaceholderPortraitPath => ImageHelper.GetImagePath("timeline/epoch_portraits/placeholder/" + Id.ToLowerInvariant() + ".png");

	public string ResolvedPortraitPath
	{
		get
		{
			if (_resolvedPortraitPath != null)
			{
				return _resolvedPortraitPath;
			}
			if (ResourceLoader.Exists(RealPortraitPath))
			{
				_resolvedPortraitPath = RealPortraitPath;
			}
			else
			{
				_resolvedPortraitPath = PlaceholderPortraitPath;
			}
			return _resolvedPortraitPath;
		}
	}

	/// <summary>
	/// Grabs the index of a given Epoch. If invalid, returns -1
	/// </summary>
	public int ChapterIndex
	{
		get
		{
			if (StoryId == null)
			{
				return -1;
			}
			EpochModel[] epochs = StoryModel.Get(StringHelper.Slugify(StoryId)).Epochs;
			for (int i = 0; i < epochs.Length; i++)
			{
				if (epochs[i].Id == Id)
				{
					return i + 1;
				}
			}
			return -1;
		}
	}

	static EpochModel()
	{
		int num = 57;
		List<Type> list = new List<Type>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Type> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = typeof(Act2BEpoch);
		num2++;
		span[num2] = typeof(Act3BEpoch);
		num2++;
		span[num2] = typeof(Colorless1Epoch);
		num2++;
		span[num2] = typeof(Colorless2Epoch);
		num2++;
		span[num2] = typeof(Colorless3Epoch);
		num2++;
		span[num2] = typeof(Colorless4Epoch);
		num2++;
		span[num2] = typeof(Colorless5Epoch);
		num2++;
		span[num2] = typeof(CustomAndSeedsEpoch);
		num2++;
		span[num2] = typeof(DailyRunEpoch);
		num2++;
		span[num2] = typeof(DarvEpoch);
		num2++;
		span[num2] = typeof(Defect1Epoch);
		num2++;
		span[num2] = typeof(Defect2Epoch);
		num2++;
		span[num2] = typeof(Defect3Epoch);
		num2++;
		span[num2] = typeof(Defect4Epoch);
		num2++;
		span[num2] = typeof(Defect5Epoch);
		num2++;
		span[num2] = typeof(Defect6Epoch);
		num2++;
		span[num2] = typeof(Defect7Epoch);
		num2++;
		span[num2] = typeof(Event1Epoch);
		num2++;
		span[num2] = typeof(Event2Epoch);
		num2++;
		span[num2] = typeof(Event3Epoch);
		num2++;
		span[num2] = typeof(Ironclad2Epoch);
		num2++;
		span[num2] = typeof(Ironclad3Epoch);
		num2++;
		span[num2] = typeof(Ironclad4Epoch);
		num2++;
		span[num2] = typeof(Ironclad5Epoch);
		num2++;
		span[num2] = typeof(Ironclad6Epoch);
		num2++;
		span[num2] = typeof(Ironclad7Epoch);
		num2++;
		span[num2] = typeof(Necrobinder1Epoch);
		num2++;
		span[num2] = typeof(Necrobinder2Epoch);
		num2++;
		span[num2] = typeof(Necrobinder3Epoch);
		num2++;
		span[num2] = typeof(Necrobinder4Epoch);
		num2++;
		span[num2] = typeof(Necrobinder5Epoch);
		num2++;
		span[num2] = typeof(Necrobinder6Epoch);
		num2++;
		span[num2] = typeof(Necrobinder7Epoch);
		num2++;
		span[num2] = typeof(NeowEpoch);
		num2++;
		span[num2] = typeof(OrobasEpoch);
		num2++;
		span[num2] = typeof(Potion1Epoch);
		num2++;
		span[num2] = typeof(Potion2Epoch);
		num2++;
		span[num2] = typeof(Regent1Epoch);
		num2++;
		span[num2] = typeof(Regent2Epoch);
		num2++;
		span[num2] = typeof(Regent3Epoch);
		num2++;
		span[num2] = typeof(Regent4Epoch);
		num2++;
		span[num2] = typeof(Regent5Epoch);
		num2++;
		span[num2] = typeof(Regent6Epoch);
		num2++;
		span[num2] = typeof(Regent7Epoch);
		num2++;
		span[num2] = typeof(Relic1Epoch);
		num2++;
		span[num2] = typeof(Relic2Epoch);
		num2++;
		span[num2] = typeof(Relic3Epoch);
		num2++;
		span[num2] = typeof(Relic4Epoch);
		num2++;
		span[num2] = typeof(Relic5Epoch);
		num2++;
		span[num2] = typeof(Silent1Epoch);
		num2++;
		span[num2] = typeof(Silent2Epoch);
		num2++;
		span[num2] = typeof(Silent3Epoch);
		num2++;
		span[num2] = typeof(Silent4Epoch);
		num2++;
		span[num2] = typeof(Silent5Epoch);
		num2++;
		span[num2] = typeof(Silent6Epoch);
		num2++;
		span[num2] = typeof(Silent7Epoch);
		num2++;
		span[num2] = typeof(UnderdocksEpoch);
		_allEpochs = list;
		_epochTypeDictionary = new Dictionary<string, Type>();
		_typeToIdDictionary = new Dictionary<Type, string>();
		for (int i = 0; i < EpochModelSubtypes.Count; i++)
		{
			Type type = EpochModelSubtypes.Get(i);
			EpochModel epochModel = (EpochModel)Activator.CreateInstance(type);
			_epochTypeDictionary[epochModel.Id] = type;
			_typeToIdDictionary[type] = epochModel.Id;
		}
	}

	/// <summary>
	/// Returns the list of epochs whose slots are revealed when this epoch is revealed on the timeline.
	/// Used by <see cref="M:MegaCrit.Sts2.Core.Timeline.EpochModel.QueueTimelineExpansion(MegaCrit.Sts2.Core.Timeline.EpochModel[])" /> at runtime and by save validation to detect missing slots.
	/// </summary>
	public virtual EpochModel[] GetTimelineExpansion()
	{
		return Array.Empty<EpochModel>();
	}

	/// <summary>
	/// WARN: Currently, every Epoch MUST unlock something. Whether it be information, cards, relics, etc.
	/// Without an unlock, if the player slots 2 Epochs at once, the game will function incorrectly.
	/// </summary>
	public virtual void QueueUnlocks()
	{
	}

	/// <summary>
	/// Static method to get the Id for a given type
	/// </summary>
	public static string GetId<T>() where T : EpochModel
	{
		return _typeToIdDictionary[typeof(T)];
	}

	public static string GetId(Type t)
	{
		return _typeToIdDictionary[t];
	}

	public static bool IsValid(string id)
	{
		return EpochIdsHashSet.Contains(id);
	}

	public static EpochModel Get(string id)
	{
		if (_epochTypeDictionary.TryGetValue(id, out Type value))
		{
			return (EpochModel)Activator.CreateInstance(value);
		}
		throw new ArgumentException("Epoch with id '" + id + "' does not exist.");
	}

	public static EpochModel Get<T>() where T : EpochModel
	{
		return Get(GetId<T>());
	}

	protected static void QueueTimelineExpansion(EpochModel[] epochs)
	{
		Log.Info("Queueing a Timeline expansion...");
		List<EpochSlotData> list = new List<EpochSlotData>();
		foreach (EpochModel epoch in epochs)
		{
			SerializableEpoch serializableEpoch = SaveManager.Instance.Progress.Epochs.FirstOrDefault((SerializableEpoch e) => e.Id == epoch.Id);
			if (serializableEpoch != null && serializableEpoch.State == EpochState.ObtainedNoSlot)
			{
				Log.Info("We have it already Yay: " + serializableEpoch.Id);
				list.Add(new EpochSlotData(epoch, EpochSlotState.Obtained));
			}
			else
			{
				list.Add(new EpochSlotData(epoch, EpochSlotState.NotObtained));
			}
		}
		NTimelineScreen.Instance.QueueTimelineExpansion(list);
		foreach (EpochModel epochModel in epochs)
		{
			SaveManager.Instance.UnlockSlot(epochModel.Id);
		}
	}

	protected string CreateCardUnlockText(List<CardModel> cards)
	{
		LocString locString = new LocString("timeline", "UNLOCK_TEXT.cards");
		cards = cards.OrderBy((CardModel c) => c.Rarity).ToList();
		for (int num = 0; num < 3; num++)
		{
			locString.Add($"Card{num + 1}", GetColoredCardName(cards[num]));
		}
		return locString.GetFormattedText();
	}

	/// <summary>
	/// Little helper method for coloring the text.
	/// </summary>
	private string GetColoredCardName(CardModel card)
	{
		if (card.Rarity == CardRarity.Common)
		{
			return card.TitleLocString.GetRawText();
		}
		if (card.Rarity == CardRarity.Uncommon)
		{
			return "[blue]" + card.TitleLocString.GetRawText() + "[/blue]";
		}
		if (card.Rarity == CardRarity.Rare)
		{
			return "[gold]" + card.TitleLocString.GetRawText() + "[/gold]";
		}
		return "ERROR";
	}

	protected string CreateRelicUnlockText(List<RelicModel> relics)
	{
		LocString locString = new LocString("timeline", "UNLOCK_TEXT.relics");
		relics = relics.OrderBy((RelicModel r) => r.Rarity).ToList();
		for (int num = 0; num < 3; num++)
		{
			locString.Add($"Relic{num + 1}", GetColoredRelicName(relics[num]));
		}
		return locString.GetFormattedText();
	}

	/// <summary>
	/// Little helper method for coloring the text.
	/// </summary>
	private string GetColoredRelicName(RelicModel relic)
	{
		if (relic.Rarity == RelicRarity.Common)
		{
			return relic.Title.GetRawText();
		}
		if (relic.Rarity == RelicRarity.Uncommon)
		{
			return "[blue]" + relic.Title.GetRawText() + "[/blue]";
		}
		if (relic.Rarity == RelicRarity.Rare)
		{
			return "[gold]" + relic.Title.GetRawText() + "[/gold]";
		}
		return "ERROR";
	}

	protected string CreatePotionUnlockText(List<PotionModel> potions)
	{
		LocString locString = new LocString("timeline", "UNLOCK_TEXT.potions");
		potions = potions.OrderBy((PotionModel r) => r.Rarity).ToList();
		for (int num = 0; num < 3; num++)
		{
			locString.Add($"Potion{num + 1}", GetColoredPotionName(potions[num]));
		}
		return locString.GetFormattedText();
	}

	/// <summary>
	/// Little helper method for coloring the text.
	/// </summary>
	private string GetColoredPotionName(PotionModel potion)
	{
		if (potion.Rarity == PotionRarity.Common)
		{
			return potion.Title.GetRawText();
		}
		if (potion.Rarity == PotionRarity.Uncommon)
		{
			return "[blue]" + potion.Title.GetRawText() + "[/blue]";
		}
		if (potion.Rarity == PotionRarity.Rare)
		{
			return "[gold]" + potion.Title.GetRawText() + "[/gold]";
		}
		return "ERROR";
	}
}
