using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MegaCrit.Sts2.Core.Nodes.Screens.Shops;

/// <summary>
/// Manages the shop items for the fake merchant shop. We need this because we need to override the default controller navigation
/// </summary>
public partial class NFakeMerchantInventory : NMerchantInventory
{
	protected override void UpdateNavigation()
	{
		List<NMerchantSlot> list = _relicContainer?.GetChildren().OfType<NMerchantSlot>().ToList() ?? new List<NMerchantSlot>();
		List<NMerchantSlot> list2 = new List<NMerchantSlot>(new global::_003C_003Ez__ReadOnlyArray<NMerchantSlot>(new NMerchantSlot[2]
		{
			list[0],
			list[1]
		})).Where((NMerchantSlot r) => r.Entry.IsStocked).ToList();
		List<NMerchantSlot> list3 = new List<NMerchantSlot>(new global::_003C_003Ez__ReadOnlyArray<NMerchantSlot>(new NMerchantSlot[3]
		{
			list[2],
			list[3],
			list[4]
		})).Where((NMerchantSlot r) => r.Entry.IsStocked).ToList();
		List<NMerchantSlot> list4 = new List<NMerchantSlot>(new global::_003C_003Ez__ReadOnlySingleElementList<NMerchantSlot>(list[5])).Where((NMerchantSlot r) => r.Entry.IsStocked).ToList();
		List<List<NMerchantSlot>> list5 = new List<List<NMerchantSlot>>(new global::_003C_003Ez__ReadOnlyArray<List<NMerchantSlot>>(new List<NMerchantSlot>[3] { list2, list3, list4 })).Where((List<NMerchantSlot> r) => r.Count > 0).ToList();
		for (int num = 0; num < list5.Count; num++)
		{
			for (int num2 = 0; num2 < list5[num].Count; num2++)
			{
				list5[num][num2].FocusNeighborLeft = ((num2 > 0) ? list5[num][num2 - 1].GetPath() : list5[num][num2].GetPath());
				list5[num][num2].FocusNeighborRight = ((num2 < list5[num].Count - 1) ? list5[num][num2 + 1].GetPath() : list5[num][num2].GetPath());
				if (num > 0)
				{
					list5[num][num2].FocusNeighborTop = ((num2 < list5[num - 1].Count) ? list5[num - 1][num2].GetPath() : list5[num - 1][list5[num - 1].Count - 1].GetPath());
				}
				else
				{
					list5[num][num2].FocusNeighborTop = list5[num][num2].GetPath();
				}
				if (num < list5.Count - 1)
				{
					list5[num][num2].FocusNeighborBottom = ((num2 < list5[num + 1].Count) ? list5[num + 1][num2].GetPath() : list5[num + 1][list5[num + 1].Count - 1].GetPath());
				}
				else
				{
					list5[num][num2].FocusNeighborBottom = list5[num][num2].GetPath();
				}
			}
		}
	}
}
