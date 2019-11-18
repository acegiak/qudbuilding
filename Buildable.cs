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
			Object.RegisterPartEvent(this, "GetShortDescription");
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
			Assemble(bp.GetTag("acegiak_Buildable"),bp.Name,cell, who);
        }

		public List<GameObjectBlueprint> getBuilds(){
			List<GameObjectBlueprint> ret = new List<GameObjectBlueprint>();
			foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.GetBlueprintsWithTag("acegiak_Buildable"))
			{
				if(ExplodeNeeds(blueprint.GetTag("acegiak_Buildable")).ContainsKey(ParentObject.GetBlueprint().Name)){
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
            int num12 = Popup.ShowOptionList(string.Empty, ChoiceList.ToArray(), HotkeyList.ToArray(), 0, "Choose what to build.", 60, bRespectOptionNewlines: false, bAllowEscape: true);
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
			if (E.ID == "GetShortDescription")
			{
				E.SetParameter("Postfix", E.GetStringParameter("Postfix") + "\n&CA simple construction material.");
			}
			
			return base.FireEvent(E);
		}

		public static Dictionary<string,int> ExplodeNeeds(string needs){
            Dictionary<string,int> ret = new Dictionary<string,int>();
            if(needs == null){
                return ret;
            }
            foreach(string row in needs.Split(';')){
                string[] bits = row.Split(':');
                ret.Add(bits[0],Convert.ToInt32(bits[1]));
            }
            return ret;
        }

		public bool Assemble(string needs, string blueprint, Cell cell, GameObject who){
            foreach(KeyValuePair<string, int> entry in ExplodeNeeds(needs))
            {
                List<GameObject> bits = cell.GetObjects(entry.Key);
                int bitcount = 0;
                foreach(GameObject go in bits){
                    bitcount+= go.Count;
                }
                if(bitcount < entry.Value){
                    GameObject sample = GameObjectFactory.Factory.CreateObject(entry.Key,-9999);

                    Popup.Show("You need "+sample.DisplayNameOnly+" x "+entry.Value.ToString()+" to build: "+GameObject.create(blueprint).DisplayNameOnly+".");
                    return false;
                }
            }

            foreach(KeyValuePair<string, int> entry in ExplodeNeeds(needs))
            {
                List<GameObject> bits = cell.GetObjects(entry.Key);
                int bitcount = 0;
                foreach(GameObject go in bits){
                    if(go.GetPart<Stacker>() == null){
                        cell.RemoveObject(go);
                        bitcount++;
                    }else{
                        if(bitcount+go.Count <= entry.Value){
                            bitcount+= go.Count;
                            cell.RemoveObject(go);
                        }else{
                            go.GetPart<Stacker>().StackCount -= (entry.Value - bitcount);
                            bitcount += entry.Value - bitcount;
                        }
                    }
                }
            }
            who.UseEnergy(10000,"Building");
			
            cell.AddObject(GameObject.create(blueprint));
            return true;
        }
	}
}
