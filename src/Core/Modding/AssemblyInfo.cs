using System.Collections.Generic;
using System.Reflection;

namespace MegaCrit.Sts2.Core.Modding;

public class AssemblyInfo
{
	/// <summary>
	/// Helper dictionary mapping assemblies to the mods they represent.
	/// </summary>
	public static Dictionary<Assembly, Mod>? ModMap { get; private set; }

	/// <summary>
	/// The base game assembly.
	/// </summary>
	public static Assembly? BaseGame { get; private set; }

	public static void Init()
	{
		BaseGame = Assembly.GetExecutingAssembly();
		ModMap = new Dictionary<Assembly, Mod>();
		foreach (Mod mod in ModManager.Mods)
		{
			if (mod.state != ModLoadState.Loaded)
			{
				continue;
			}
			foreach (Assembly assembly in mod.assemblies)
			{
				ModMap[assembly] = mod;
			}
		}
	}

	/// <summary>
	/// This should only be called by tests.
	/// Typically, AssemblyInfo is initialized once at the start of the game and never cleared.
	/// </summary>
	public static void ClearForTests()
	{
		BaseGame = null;
		ModMap = null;
	}
}
