#define PREVIEW_FEATURES

using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Parts;
using XRL.Language;
using qudbuilding.Utilities;
#if PREVIEW_FEATURES
using XRL.Liquids;
#endif

namespace qudbuilding
{
	public class BuildableEntry
	{
		public string Name;

		public GameObjectBlueprint Result;

		public string DisplayName => Name ?? Result.DisplayName();

		public int EnergyCost = 10000;

		public bool IsItem = false; /// Does the output go in our inventory, and can we craft multiple at once?

		public string _BlueprintsString = ""; 

		[NonSerialized]
		private Dictionary<GameObjectBlueprint, int> _BlueprintRequirements;

		public Dictionary<GameObjectBlueprint, int> BlueprintRequirements
		{
			get
			{
				if (this._BlueprintRequirements == null)
				{
					this._BlueprintRequirements = new Dictionary<GameObjectBlueprint, int>();
					if (!_BlueprintsString.IsNullOrEmpty())
					{
						foreach(string blueprintEntry in _BlueprintsString.Split(';'))
						{
							string[] entries = blueprintEntry.Split(':').Select(s => s.Trim()).ToArray();
							this._BlueprintRequirements.Add(GameObjectFactory.Factory.GetBlueprint(entries[0].Trim()),Convert.ToInt32(entries[1]));
						}
					}
				}
				return this._BlueprintRequirements;
			}
		}

		#if PREVIEW_FEATURES
		public string _LiquidsString = ""; 

		[NonSerialized]
		private Dictionary<string, int> _LiquidRequirements;

		public Dictionary<string, int> LiquidRequirements
		{
			get
			{
				if (this._LiquidRequirements == null)
				{
					this._LiquidRequirements = new Dictionary<string, int>();
					if (!_LiquidsString.IsNullOrEmpty())
					{
						foreach(string liquidEntry in _LiquidsString.Split(';'))
						{
							string[] entries = liquidEntry.Split(':');
							this._LiquidRequirements.Add(entries[0].Trim(),Convert.ToInt32(entries[1]));
						}
					}
				}
				return this._LiquidRequirements;
			}
		}
		#endif

		public void StartAssembling(GameObject who, int amount = 1, Cell targetCell = null)
		{
			int totalCost = this.EnergyCost * amount;
			if (totalCost > 1000) // requires >1 turn
			{
				// Apply the Building effect; once it's done, it will call Assemble.
				who.ApplyEffect(new XRL.World.Effects.Building(this, amount, targetCell));
			}
			else // Assemble it ourselves, since we don't need a full turn.
			{
				if (targetCell != null)
				{
					targetCell.AddObject(Assemble(who));
				}
				else
				{
					for (int i = 0; i < amount; i++)
					{
						who.ReceiveObject(Assemble(who));
					}
				}
				who.UseEnergy(totalCost);
				string verb = (targetCell != null) ? "build" : "craft";
				XRL.Messages.MessageQueue.AddPlayerMessage($"&yYou {verb} {QudBuilding_Grammar.NumericalPluralize(DisplayName, amount)}.");
			}
		}

		public GameObject Assemble(GameObject who)
		{
			foreach(KeyValuePair<GameObjectBlueprint, int> entry in BlueprintRequirements)
			{
				string blueprintName = entry.Key.Name;
				List<GameObject> bits = who.GetInventoryDirect((GameObject obj) => obj.GetBlueprint().DescendsFrom(blueprintName));
				int bitcount = 0;
				foreach (GameObject bit in bits)
				{
					if(bit.HasPart("Stacker"))
					{
						Stacker stacker = bit.GetPart<Stacker>();
						int remaining = (entry.Value - bitcount);
						if (stacker.StackCount <= remaining)
						{
							bitcount += stacker.StackCount;
							bit.Destroy();
						}
						else
						{
							stacker.StackCount -= remaining;
						}
					}
					else
					{
						bit.Destroy();
						bitcount++;
					}
				}
			}
			#if PREVIEW_FEATURES
			foreach (KeyValuePair<string, int> entry in LiquidRequirements)
			{
				List<GameObject> liquidContainers = who.GetInventoryDirect(item => item.LiquidVolume?.IsPureLiquid(entry.Key) ?? false);
				int liquidCount = 0;
				foreach (GameObject liquidContainer in liquidContainers)
				{
					LiquidVolume liquidVolume = liquidContainer.LiquidVolume;
					int UseAmount = Math.Min(entry.Value - liquidCount, liquidVolume.Amount(entry.Key));
					if(liquidVolume.UseDrams(entry.Key, UseAmount))
					{
						liquidCount += UseAmount;
						if(liquidCount >= entry.Value)
						{
							break;
						}
					}
				}
			}
			#endif
			return GameObject.create(Result.Name, beforeObjectCreated: delegate(GameObject obj)
				{
					obj.RemovePart<Graffitied>();
				}
			);
		}
		/// Check the GameObject's inventory to see if all ingredients are present
		public bool CanBuild(GameObject who)
		{
			foreach (KeyValuePair<GameObjectBlueprint, int> entry in BlueprintRequirements)
			{
				List<GameObject> bits = who.GetInventoryDirect(item => BuildableFactory.IsValidBuildingIngredient(item) && item.GetBlueprint().DescendsFrom(entry.Key.Name));
				if (bits.Select(item => item.Count).Sum() < entry.Value) return false;
			}
			#if PREVIEW_FEATURES
			foreach (KeyValuePair<string, int> entry in LiquidRequirements)
			{
				List<GameObject> liquidContainers = who.GetInventoryDirect(item => item.LiquidVolume?.IsPureLiquid(entry.Key) ?? false);
				if(liquidContainers.Select(item => item.LiquidVolume.Amount(entry.Key)).Sum() < entry.Value) return false;
			}
			#endif
			return true;
		}
		/// Return the maximum number of times this item can be built with the ingredients in the inventory
		public int MaxBuildCount(GameObject who)
		{
			// Calling CanBuild here would be wasteful, since it's basically what we do anyway.
			int count = Int32.MaxValue;
			foreach (KeyValuePair<GameObjectBlueprint, int> entry in BlueprintRequirements)
			{
				List<GameObject> bits = who.GetInventoryDirect(item => BuildableFactory.IsValidBuildingIngredient(item) && item.GetBlueprint().DescendsFrom(entry.Key.Name));
				count = Math.Min(count, bits.Select(item => item.Count).Sum()/entry.Value);
				if (count == 0) return 0;
			}
			#if PREVIEW_FEATURES
			foreach (KeyValuePair<string, int> entry in LiquidRequirements)
			{
				List<GameObject> liquidContainers = who.GetInventoryDirect(item => item.LiquidVolume?.IsPureLiquid(entry.Key) ?? false);
				count = Math.Min(count, liquidContainers.Select(item => item.LiquidVolume.Amount(entry.Key)).Sum() / entry.Value);
				if (count == 0) return 0;
			}
			#endif
			return count;
		}
		/// Return a description of what needed ingredients are missing
		public string GetMissingIngredients(GameObject who)
		{
			List<string> missingNames = new List<string>();
			foreach (KeyValuePair<GameObjectBlueprint, int> entry in BlueprintRequirements)
			{
				List<GameObject> bits = who.GetInventoryDirect(item => BuildableFactory.IsValidBuildingIngredient(item) && item.GetBlueprint().DescendsFrom(entry.Key.Name));
				int remaining = entry.Value - bits.Select(item => item.Count).Sum();
				if (remaining > 0)
				{
					missingNames.Add(QudBuilding_Grammar.NumericalPluralize(entry.Key.DisplayName(), remaining));
				}
			}
			#if PREVIEW_FEATURES
			foreach (KeyValuePair<string, int> entry in LiquidRequirements)
			{
				List<GameObject> liquidContainers = who.GetInventoryDirect(item => item.LiquidVolume?.IsPureLiquid(entry.Key) ?? false);
				if (liquidContainers.Select(item => item.LiquidVolume.Amount(entry.Key)).Sum() < entry.Value)
				{
					int remaining = entry.Value - liquidContainers.Select(item => item.LiquidVolume.Amount(entry.Key)).Sum();
					if (remaining > 0)
					{
						missingNames.Add(QudBuilding_Grammar.NumericalPluralize("dram", remaining) + " of " + LiquidVolume.getLiquid(entry.Key).Name);
					}
				}
			}
			#endif
			return Grammar.MakeAndList(missingNames);
		}
	}
}
