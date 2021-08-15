using System;
using System.Collections.Generic;
using XRL.World;
using XRL.UI;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts.Skill
{
	[Serializable]
	internal class acegiak_Tinker0 : BaseSkill
	{
		public Guid ActivatedAbilityID = Guid.Empty;
		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "CommandQudBuild");
			base.Register(Object);
		}

		public static List<GameObject> BuildablesInInventory(GameObject gameObject)
		{
			return gameObject.GetInventoryDirect((GameObject obj) => obj.HasPart("acegiak_Buildable"));
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "CommandQudBuild")
			{
				List<GameObject> buildables = BuildablesInInventory(ParentObject);
				if (buildables.Count == 0)
				{
					Popup.Show("You have no items you can build with!");
					return false;
				}
				List<GameObjectBlueprint> possibleBuilds = new List<GameObjectBlueprint>();
				foreach (GameObject obj in buildables)
				{
					List<GameObjectBlueprint> bps = obj.GetPart<acegiak_Buildable>().getBuilds();
					if (bps == null) continue;
					foreach (GameObjectBlueprint bp in bps)
					{
						if (possibleBuilds.Contains(bp)) continue;
						string needs = bp.GetTag("acegiak_Buildable");
						if (needs == null) continue;
						if (!CanBuild(needs, ParentObject)) continue;
						possibleBuilds.Add(bp);
					}
				}
				if (possibleBuilds == null || possibleBuilds.Count == 0)
				{
					Popup.Show("You don't know how to build anything with what you have on hand!");
					return false;
				}
				GameObjectBlueprint toBuild = null;
				if (ParentObject.IsPlayer())
				{
					toBuild = acegiak_Tinker0.ChooseBuild(possibleBuilds);
				} // could add AI building later, but that's a lot of work
				if (toBuild == null)
				{
					return false;
				}
				string dirstring = XRL.UI.PickDirection.ShowPicker("Build where?");
				if (dirstring == null)
				{
					return false;
				}
				Cell targetCell = ParentObject.CurrentCell.GetCellFromDirection(dirstring);
				if (targetCell == null)
				{
					return false;
				}
				if (!targetCell.IsEmpty())
				{
					Popup.Show("There's already something there!");
					return false;
				}
				Assemble(toBuild.GetTag("acegiak_Buildable"), toBuild.Name, targetCell, ParentObject);
				return true;
			}
			return base.FireEvent(E);
		}

		public static GameObjectBlueprint ChooseBuild(List<GameObjectBlueprint> possibleBuilds)
		{
			if (possibleBuilds == null || possibleBuilds.Count == 0)
			{
				return null;
			}
			List<XRL.World.GameObjectBlueprint> ObjectChoices = new List<XRL.World.GameObjectBlueprint>();
			List<string> ChoiceList = new List<string>();
			List<char> HotkeyList = new List<char>();
			char ch = 'a';
			foreach(XRL.World.GameObjectBlueprint GO in possibleBuilds)
			{
				ObjectChoices.Add(GO);
				HotkeyList.Add(ch++);
				ChoiceList.Add(GO.DisplayName());
			}
			int selectedidx = Popup.ShowOptionList(string.Empty, ChoiceList.ToArray(), HotkeyList.ToArray(), 0, "Choose what to build.", 60, RespectOptionNewlines: false, AllowEscape: true);
			if (selectedidx < 0)
			{
				return null;
			}
			return ObjectChoices[selectedidx];
		}
		
		public static bool CanBuild(string needs, GameObject who)
		{
			foreach(KeyValuePair<string, int> entry in acegiak_Buildable.ExplodeNeeds(needs))
			{
				List<GameObject> bits = who.GetInventoryDirect((GameObject obj) => obj.Blueprint == entry.Key || obj.GetBlueprint().InheritsFrom(entry.Key));
				int bitcount = 0;
				foreach (GameObject gameObject in bits)
				{
					if (gameObject.HasPart("Stacker")) bitcount += gameObject.GetPart<Stacker>().StackCount;
					else bitcount++;
				}
				if (bitcount < entry.Value) return false;
			}
			return true;
		}

		public static bool Assemble(string needs, string blueprint, Cell cell, GameObject who)
		{
			foreach(KeyValuePair<string, int> entry in acegiak_Buildable.ExplodeNeeds(needs))
			{
				List<GameObject> bits = who.GetInventoryDirect((GameObject obj) => obj.Blueprint == entry.Key || obj.GetBlueprint().InheritsFrom(entry.Key));
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
			who.UseEnergy(10000,"Building");
			cell.AddObject(GameObject.create(blueprint));
			return true;
		}

		public override bool AddSkill(GameObject GO)
		{
			ActivatedAbilityID = AddMyActivatedAbility("Build", "CommandQudBuild", "Skill", "You build an object from raw materials.", "-", null, Toggleable: false, DefaultToggleState: false, ActiveToggle: false, IsAttack: false);
			return base.AddSkill(GO);
		}

		public override bool RemoveSkill(GameObject GO)
		{
			RemoveMyActivatedAbility(ref ActivatedAbilityID);
			return base.RemoveSkill(GO);
		}
	}
}
