// Compile with `dotnet build -p:DefineConstants="PREVIEW_FEATURES"` to enable experimental feature support.
// To play with those features, uncomment the following define IN ALL FILES.
//#define PREVIEW_FEATURES

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using XRL;
using XRL.Liquids;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace qudbuilding
{
	[HasModSensitiveStaticCache]
	public class BuildableFactory
	{

		private static readonly Dictionary<string, Action<XmlDataHelper>> rootHandlers = new Dictionary<string, Action<XmlDataHelper>> { { "buildables", HandleBuildablesNode } };

		private static readonly Dictionary<string, Action<XmlDataHelper>> leafHandlers = new Dictionary<string, Action<XmlDataHelper>>
		{
			{ "buildable", HandleBuildableNode }
		};
		
		[ModSensitiveStaticCache(false)]
		public static List<BuildableEntry> BuildableList;

		[ModSensitiveStaticCache(false)]
		public static Dictionary<GameObjectBlueprint, List<BuildableEntry>> BuildablesByResult;

		[ModSensitiveStaticCache(false)]
		public static Dictionary<string, BuildableEntry> BuildablesByName;

		[ModSensitiveStaticCache(false)]
		public static Dictionary<string, List<BuildableEntry>> BuildablesByIngredient; // can be a blueprint name or liquid name

		[ModSensitiveStaticCache(false)]
		public static List<string> Ingredients;

		[ModSensitiveCacheInit]
		public static void Init()
		{
			if (BuildablesByResult == null)
			{
				Loading.LoadTask("Loading Buildables.xml", LoadBuildables);
			}
		}

		private static void LoadBuildables()
		{
			BuildableList = new List<BuildableEntry>();
			BuildablesByResult = new Dictionary<GameObjectBlueprint, List<BuildableEntry>>();
			BuildablesByName = new Dictionary<string, BuildableEntry>();
			foreach (XmlDataHelper item in DataManager.YieldXMLStreamsWithRoot("buildables"))
			{
				try
				{
					item.HandleNodes(rootHandlers);
				}
				catch (Exception message)
				{
					MetricsManager.LogPotentialModError(item.modInfo, message);
				}
			}
			PostLoad();
		}

		private static void PostLoad()
		{
			BuildablesByIngredient = new Dictionary<string, List<BuildableEntry>>();
			Ingredients = new List<string>();
			foreach (BuildableEntry buildableEntry in BuildableList)
			{
				foreach (string ingredient in buildableEntry.BlueprintRequirements.Keys.Select(x => x.Name))
				{
					List<BuildableEntry> list;
					if (!BuildablesByIngredient.TryGetValue(ingredient, out list))
					{
						BuildablesByIngredient[ingredient] = list = new List<BuildableEntry>{ buildableEntry };
					}
					else
					{
						list.Add(buildableEntry);
					}
					if (!Ingredients.Contains(ingredient))
					{
						Ingredients.Add(ingredient);
					}
				}
			}
		}

		public static void HandleBuildablesNode(XmlDataHelper xml)
		{
			xml.HandleNodes(leafHandlers);
		}

		public static void HandleBuildableNode(XmlTextReader Reader)
		{
			BuildableEntry buildableEntry;
			string resultString = Reader.GetAttribute("Result");
			string recipeName = Reader.GetAttribute("Name");
			string recipeKey = recipeName ?? resultString;
			string loadMode = Reader.GetAttribute("Load");
			if (loadMode == "Replace" || !BuildablesByName.TryGetValue(recipeKey, out buildableEntry))
			{
				BuildablesByName[recipeKey] = buildableEntry = new BuildableEntry
				{
					Name = recipeName
				};
				BuildableList.Add(buildableEntry);
			}
			if (!resultString.IsNullOrEmpty())
			{
				GameObjectBlueprint resultBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(resultString);
				if(resultBlueprint is null)
				{
					MetricsManager.LogError($"Undefined result blueprint in Buildables.xml:{Reader.LineNumber}: {resultString}");
				}
				else
				{
					List<BuildableEntry> items;
					if(BuildablesByResult.TryGetValue(resultBlueprint, out items))
					{
						items.Add(buildableEntry);
					}
					else
					{
						BuildablesByResult.Add(resultBlueprint, new List<BuildableEntry>{ buildableEntry });
					}
					if (!resultString.IsNullOrEmpty())
					{
						buildableEntry.Result = resultBlueprint;
					}
				}
			}
			string energyCostString = Reader.GetAttribute("EnergyCost");
			if (!energyCostString.IsNullOrEmpty())
			{
				buildableEntry.EnergyCost = Convert.ToInt32(energyCostString);
			}
			string isItemString = Reader.GetAttribute("IsItem");
			buildableEntry.IsItem = isItemString.EqualsNoCase("true");
			string requiredBlueprintsString = Reader.GetAttribute("RequiredBlueprints");
			if (!requiredBlueprintsString.IsNullOrEmpty())
			{
				buildableEntry._BlueprintsString = requiredBlueprintsString;
			}
			string requiredLiquidsString = Reader.GetAttribute("RequiredLiquids");
			if (!requiredLiquidsString.IsNullOrEmpty())
			{
				buildableEntry._LiquidsString = requiredLiquidsString;
			}
			// if we're not at the end
			if (Reader.NodeType != XmlNodeType.EndElement && !Reader.IsEmptyElement)
			{
				// consume tokens until we find the end of the element or the end of the file
				while (Reader.Read() && (Reader.NodeType != XmlNodeType.EndElement || (!(Reader.Name == "") && !(Reader.Name == "buildable"))))
				{
				}
			}
		}
		public static bool IsBuildable(GameObjectBlueprint blueprint)
		{
			return BuildablesByResult.ContainsKey(blueprint);
		}
		public static bool IsIngredient(string blueprint)
		{
			return BuildablesByIngredient.ContainsKey(blueprint);
		}
		public static List<BuildableEntry> GetBuildsWithIngredient(string ingredient)
		{
			BaseLiquid liquid = LiquidVolume.getLiquid(ingredient);
			if(liquid != null)
			{
				return GetBuildsWithLiquid(liquid);
			}
			GameObjectBlueprint blueprint = GameObjectFactory.Factory.GetBlueprintIfExists(ingredient);
			if(blueprint != null)
			{
				return GetBuildsWithBlueprint(blueprint);
			}
			return null;
		}
		public static List<BuildableEntry> GetBuildsWithBlueprint(GameObjectBlueprint blueprint)
		{
			return BuildableFactory.BuildableList.Where(
				buildableEntry => buildableEntry.BlueprintRequirements.ContainsKey(blueprint)
			).ToList();
		}
		public static List<BuildableEntry> GetBuildsWithLiquid(BaseLiquid liquid)
		{
			return BuildableFactory.BuildableList.Where(
				buildableEntry => buildableEntry.LiquidRequirements.ContainsKey(liquid.ID)
			).ToList();
		}
		public static bool IsValidBuildingIngredient(GameObject obj)
		{
			if (obj.IsTemporary && obj.GetPart<XRL.World.Parts.Temporary>()?.Duration != -1)
			{
				return false;
			}
			if (obj.IsImportant())
			{
				return false;
			}
			LiquidVolume liquidVolume = obj.LiquidVolume;
			if (liquidVolume != null && !liquidVolume.EffectivelySealed() && liquidVolume.Volume > 0 && obj.GetEpistemicStatus() != 0)
			{
				foreach (BaseLiquid liquidIngredient in liquidVolume.GetComponentLiquids())
				{
					if (Ingredients.Contains(liquidIngredient.ID))
					{
						return true;
					}
				}
				return true;
			}
			if (Ingredients.Contains(obj.Blueprint))
			{
				return true;
			}
			return false;
		}
	}
}
