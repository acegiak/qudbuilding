using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts.Skill
{
	[Serializable]
	internal class acegiak_Tinker0 : BaseSkill
	{
		public acegiak_Tinker0()
		{
			DisplayName = "acegiak_Tinker0";
		}

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
            Object.RegisterPartEvent(this, "OwnerGetInventoryActions");

			base.Register(Object);
		}

		public override bool FireEvent(Event E)
		{
				if (E.ID == "OwnerGetInventoryActions")
				{
                    GameObject GO = E.GetGameObjectParameter("Object");
					if (GO.Understood() && GO.GetPart<acegiak_Buildable>() != null && GO.pPhysics.CurrentCell != null)
					{
						E.GetParameter<EventParameterGetInventoryActions>("Actions").AddAction("Build", 'B', false, "&WB&yuild", "InvCommandBuild", 5);
					}
				}
			return base.FireEvent(E);
		}

		public override bool AddSkill(GameObject GO)
		{
			return true;
		}

		public override bool RemoveSkill(GameObject GO)
		{
			return true;
		}
	}
}
