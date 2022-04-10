namespace XRL.World.Parts
{
	public class PickableFlower : IPart
	{
        public override bool WantEvent(int ID, int cascade)
        {
            if (ID == BeforeApplyDamageEvent.ID) return true;
            return base.WantEvent(ID, cascade);
        }

		public override bool HandleEvent(BeforeApplyDamageEvent E)
		{
            if(E.Damage.Amount < 0) return base.HandleEvent(E);
            GameObject flowerItem = ParentObject.CurrentCell.AddObject("Flower"); // spawn flower item
            Render render = ParentObject.GetPart<Render>();
            flowerItem.GetPart<Render>().ColorString = render.ColorString; // sync up the color
            ParentObject.CurrentCell.AddObject("DirtPath"); // replace parent floor with dirt path
            ParentObject.Destroy(); // destroy our parent!
            return base.HandleEvent(E);
		}
	}
}
