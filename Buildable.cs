using System;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts.Effects;
using System.Collections.Generic;
using System.Linq;

namespace XRL.World.Parts
{
	[Serializable]
	public class acegiak_Buildable : IPart
	{
		public int Count;


		public string Result;
		public string ResultName;

		public bool bNoSmartUse;

		public acegiak_Buildable()
		{
		}

		public override bool SameAs(IPart p)
		{

			return base.SameAs(p);
		}

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterPartEvent(this, "GetInventoryActions");
			Object.RegisterPartEvent(this, "InvCommandBuild");
			base.Register(Object);
		}

        public void Build(GameObject who){
            Cell cell = ParentObject.CurrentCell;
            if(cell == null){
                Popup.Show("Put things on the ground to build with them.");
                return;
            }
			GameObjectBlueprint bp = ChooseBuild(getBuilds());
			if(bp == null){
                Popup.Show("There's nothing that uses those parts.");
                return;
			}
			GameObject go = bp.createOne();
            go.GetPart<acegiak_CanBuild>().Build(cell, who);
        }

		public List<GameObjectBlueprint> getBuilds(){
			List<GameObjectBlueprint> ret = new List<GameObjectBlueprint>();
			foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList)
			{
				if(acegiak_CanBuild.ExplodeNeeds(blueprint.GetPartParameter("acegiak_CanBuild","Needs")).ContainsKey(ParentObject.GetBlueprint().Name)){
					ret.Add(blueprint);
				}
			}
			return ret;
		}

		public GameObjectBlueprint ChooseBuild(List<GameObjectBlueprint> bps){
			List<XRL.World.GameObjectBlueprint> ObjectChoices = new List<XRL.World.GameObjectBlueprint>();
            List<string> ChoiceList = new List<string>();
            List<char> HotkeyList = new List<char>();
            char ch = 'a';
            foreach(XRL.World.GameObjectBlueprint GO in bps)
            {
					GameObject sample = GameObjectFactory.Factory.CreateObject(GO.Name,-9999);
                    ObjectChoices.Add(GO);
                    HotkeyList.Add(ch);
                    ChoiceList.Add(sample.DisplayNameOnly);
                    ch = (char)(ch + 1);
            }
            if (ObjectChoices.Count == 0)
            {
                return null;
            }
            int num12 = Popup.ShowOptionList(string.Empty, ChoiceList.ToArray(), HotkeyList.ToArray(), 0, "Select a gift to give.", 60, bRespectOptionNewlines: false, bAllowEscape: true);
            if (num12 < 0)
            {
                return null;
            }
			return ObjectChoices[num12];
		}


		public override bool FireEvent(Event E)
		{
				// if (E.ID == "GetInventoryActions")
				// {
				// 	if (ParentObject.Understood() && ParentObject.pPhysics.CurrentCell != null)
				// 	{
				// 		E.GetParameter<EventParameterGetInventoryActions>("Actions").AddAction("Build", 'B', false, "&WB&yuild", "InvCommandBuild", 5);
				// 	}
				// }
				// else
				if (E.ID == "InvCommandBuild")
				{
					Build(E.GetParameter<GameObject>("Owner"));
                    E.RequestInterfaceExit();
				}
			
			return base.FireEvent(E);
		}
	}
}
