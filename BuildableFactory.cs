// Compile with `dotnet build -p:DefineConstants="PREVIEW_FEATURES"` to enable experimental liquid support.
#define PREVIEW_FEATURES

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

		protected static Action<object> handleError = MetricsManager.LogError;

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
			ModManager.ForEachFile("Buildables.xml", delegate(string path, ModInfo modInfo)
			{
				handleError = modInfo.Error;
				XmlTextReader xmlTextReader = new XmlTextReader(path);
				xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
				while (xmlTextReader.Read())
				{
					if (xmlTextReader.Name == "buildables")
					{
						LoadBuildablesNode(xmlTextReader, isPrimary: false);
					}
					else if(xmlTextReader.NodeType == XmlNodeType.Element)
					{
						handleError($"Unknown node in Buildables.xml:{xmlTextReader.LineNumber} (expected 'buildables'): {xmlTextReader.Name}");
					}
				}
			});
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

		public static void LoadBuildablesNode(XmlTextReader Reader, bool isPrimary = true)
		{
			while (Reader.Read())
			{
				if (Reader.Name == "buildable")
				{
					LoadBuildableNode(Reader, isPrimary);
				}
				else if (Reader.NodeType == XmlNodeType.Element)
				{
					handleError($"Unknown node in Buildables.xml:{Reader.LineNumber}:{Reader.LinePosition} (expected 'buildables'): {Reader.Name}");
				}
			}
		}

		public static void LoadBuildableNode(XmlTextReader Reader, bool isPrimary = true)
		{
			BuildableEntry buildableEntry;
			string resultString = Reader.GetAttribute("Result");
			string recipeName = Reader.GetAttribute("Name") ?? resultString;
			if (!BuildablesByName.TryGetValue(recipeName, out buildableEntry))
			{
				BuildablesByName[recipeName] = buildableEntry = new BuildableEntry
				{
					Name = recipeName
				};
				BuildableList.Add(buildableEntry);
			}
			if (!resultString.IsNullOrEmpty())
			{
				GameObjectBlueprint resultBlueprint = GameObjectFactory.Factory.GetBlueprint(resultString);
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
			// TODO: Implement liquid requirements handling
			#if PREVIEW_FEATURES
			string requiredLiquidsString = Reader.GetAttribute("RequiredLiquids");
			if (!requiredLiquidsString.IsNullOrEmpty())
			{
				buildableEntry._LiquidsString = requiredLiquidsString;
			}
			#endif
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
			#if PREVIEW_FEATURES
			BaseLiquid liquid = LiquidVolume.getLiquid(ingredient);
			if(liquid != null)
			{
				return GetBuildsWithLiquid(liquid);
			}
			#endif
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
		#if PREVIEW_FEATURES
		public static List<BuildableEntry> GetBuildsWithLiquid(BaseLiquid liquid)
		{
			return BuildableFactory.BuildableList.Where(
				buildableEntry => buildableEntry.LiquidRequirements.ContainsKey(liquid.ID)
			).ToList();
		}
		#endif
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
			#if PREVIEW_FEATURES
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
			#endif
			if (Ingredients.Contains(obj.Blueprint))
			{
				return true;
			}
			return false;
		}
	}
}
