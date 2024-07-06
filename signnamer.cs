using System;
using XRL.UI;

namespace XRL.World.Parts
{
	[Serializable]
	public class acegiak_SignNamer : IPart
	{

		public override bool SameAs(IPart p)
		{
			return false;
		}

		public override bool AllowStaticRegistration()
		{
			return true;
		}

		public override void Register(GameObject Object, IEventRegistrar Registrar)
		{
			Object.RegisterPartEvent(this, "EnterCell");
			base.Register(Object, Registrar);
		}


		public override bool FireEvent(Event E)
		{
			if (E.ID == "EnterCell")
			{
				if(ParentObject.GetPart<Chat>() != null){
					ParentObject.GetPart<Chat>().Says = Popup.AskString("Write on sign...","");
				}
			}
			return base.FireEvent(E);
		}
	}
}
