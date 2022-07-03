using System;
using System.Collections.Generic;
using System.Linq;
using XRL.UI;
using qudbuilding;
using qudbuilding.Utilities;

namespace XRL.World.Parts.Skill
{
	class qudbuilding_ConstructionSkill : BaseSkill
	{
		public Guid BuildAbilityID = Guid.Empty;
		public Guid RebuildAbilityID = Guid.Empty;
		private BuildableEntry LastBuilt;
		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "CommandQudBuild");
			Object.RegisterPartEvent(this, "CommandQudBuildLast");
			base.Register(Object);
		}

		public override bool FireEvent(Event E)
		{
			BuildableEntry toBuild = null;
			if (E.ID == "CommandQudBuild")
			{
				if (ParentObject.IsPlayer())
				{
					toBuild = ChooseBuild(BuildableFactory.BuildableList);
				} // could add AI building later, but that's a lot of work
				if (toBuild == null)
				{
					return false;
				}
			}
			if (E.ID == "CommandQudBuildLast") {
				toBuild = LastBuilt;
			}
			if (toBuild != null) {
				return TryBuildPlayer(toBuild);
			}
			return base.FireEvent(E);
		}

		// TODO: Add AI building??
		public bool TryBuildPlayer(BuildableEntry toBuild) {
			if (!toBuild.CanBuild(The.Player))
			{
				Popup.Show($"You don't have the materials to build {XRL.Language.Grammar.A(toBuild.DisplayName)}! You're missing {toBuild.GetMissingIngredients(ParentObject)}!");
				return false;
			}
			if (!toBuild.IsItem)
			{
				string dirstring = XRL.UI.PickDirection.ShowPicker("Build where?");
				if (dirstring == null)
				{
					return false;
				}
				Cell targetCell = The.Player.CurrentCell.GetCellFromDirection(dirstring);
				if (targetCell == null)
				{
					return false;
				}
				if (!targetCell.IsEmpty())
				{
					Popup.Show("There's something in the way!");
					return false;
				}
				XRL.Messages.MessageQueue.AddPlayerMessage("You begin building " + XRL.Language.Grammar.A(toBuild.DisplayName + "."), "&y");
				toBuild.StartAssembling(The.Player, targetCell: targetCell);
			} else {
				int amountToBuild = Popup.AskNumber("How many do you want to craft?", 1, 0, toBuild.MaxBuildCount(The.Player)) ?? 0;
				if (amountToBuild == 0)
				{
					return false;
				}
				XRL.Messages.MessageQueue.AddPlayerMessage("You begin crafting " + QudBuilding_Grammar.NumericalPluralize(toBuild.DisplayName, amountToBuild) + ".", "&y");
				toBuild.StartAssembling(The.Player, amountToBuild);
			}
			LastBuilt = toBuild;
			return true;
		}

		public BuildableEntry ChooseBuild(List<BuildableEntry> possibleBuilds, bool ShowUnavailable = true)
		{
			if (possibleBuilds.IsNullOrEmpty())
			{
				return null;
			}
			List<BuildableEntry> BuildChoices = new List<BuildableEntry>();
			List<string> ChoiceList = new List<string>();
			List<char> HotkeyList = new List<char>();
			char ch = 'a';
			foreach(BuildableEntry buildableEntry in possibleBuilds.OrderByDescending(x => x.CanBuild(ParentObject)))
			{
				BuildChoices.Add(buildableEntry);
				HotkeyList.Add(((ch <= 'z') ? ch++ : ' '));
				if (buildableEntry.CanBuild(ParentObject))
				{
					string itemTag = buildableEntry.IsItem ? "{{gray|[item]}} " : "";
					ChoiceList.Add($"{itemTag}{buildableEntry.DisplayName}");
				} else if (ShowUnavailable) {
					string itemTag = buildableEntry.IsItem ? "[item] " : "";
					ChoiceList.Add("{{dark gray|" + itemTag + buildableEntry.DisplayName + "}} {{dark red|(unavailable)}}");
				}
			}
			int selectedidx = Popup.ShowOptionList("Building", ChoiceList.ToArray(), HotkeyList.ToArray(), 0, "Choose what to build.", 78, RespectOptionNewlines: false, AllowEscape: true);
			if (selectedidx < 0)
			{
				return null;
			}
			return BuildChoices[selectedidx];
		}
		
		public override bool AddSkill(GameObject GO)
		{
			BuildAbilityID = AddMyActivatedAbility("Build", "CommandQudBuild", "Skill", "You build an object from raw materials.", "-", null, Toggleable: false, DefaultToggleState: false, ActiveToggle: false, IsAttack: false);
			RebuildAbilityID = AddMyActivatedAbility("Rebuild Last", "CommandQudBuildLast", "Skill", "Re-build what you built last.", "-", null, Toggleable: false, DefaultToggleState: false, ActiveToggle: false, IsAttack: false);
			return base.AddSkill(GO);
		}

		public override bool RemoveSkill(GameObject GO)
		{
			RemoveMyActivatedAbility(ref BuildAbilityID);
			RemoveMyActivatedAbility(ref RebuildAbilityID);
			return base.RemoveSkill(GO);
		}
	}
}
