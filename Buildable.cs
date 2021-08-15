using System;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;
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
			Object.RegisterPartEvent(this, "GetShortDescription");
			Object.RegisterPartEvent(this, "GetBuilds");
			base.Register(Object);
		}

		public List<GameObjectBlueprint> getBuilds()
		{
			List<GameObjectBlueprint> ret = new List<GameObjectBlueprint>();
			foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.GetBlueprintsWithTag("acegiak_Buildable"))
			{
				if (ExplodeNeeds(blueprint.GetTag("acegiak_Buildable")).ContainsKey(ParentObject.GetBlueprint().Name) && !ret.Contains(blueprint))
				{
					ret.Add(blueprint);
				}
			}
			return ret;
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "GetShortDescription")
			{
				E.SetParameter("Postfix", E.GetStringParameter("Postfix") + "\n&CA simple construction material.");
			}
			
			return base.FireEvent(E);
		}

		public static Dictionary<string,int> ExplodeNeeds(string needs)
		{
			Dictionary<string,int> ret = new Dictionary<string,int>();
			if (needs == null)
			{
				return ret;
			}
			foreach(string row in needs.Split(';'))
			{
				string[] bits = row.Split(':');
				ret.Add(bits[0],Convert.ToInt32(bits[1]));
			}
			return ret;
		}
	}
}
