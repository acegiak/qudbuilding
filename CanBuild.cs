using System;
using XRL.Rules;
using XRL.UI;
using XRL.Core;
using XRL.World.Parts.Effects;
using XRL.World.Parts.Skill;
using System.Collections.Generic;

namespace XRL.World.Parts
{
	[Serializable]
	public class acegiak_CanBuild : IPart
	{
		public string Needs;

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

		public acegiak_CanBuild()
		{
		}

        public bool Build(Cell cell, GameObject who){
            foreach(KeyValuePair<string, int> entry in ExplodeNeeds(this.Needs))
            {
                List<GameObject> bits = cell.GetObjects(entry.Key);
                int bitcount = 0;
                foreach(GameObject go in bits){
                    bitcount+= go.Count;
                }
                if(bitcount < entry.Value){
                    GameObject sample = GameObjectFactory.Factory.CreateObject(entry.Key,-9999);

                    Popup.Show("You need "+sample.DisplayNameOnly+" x "+entry.Value.ToString()+" to build: "+ParentObject.DisplayNameOnly+".");
                    return false;
                }
            }

            foreach(KeyValuePair<string, int> entry in ExplodeNeeds(this.Needs))
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
            cell.AddObject(ParentObject);
            return true;
        }

	}
}
