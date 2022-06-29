using System;
using qudbuilding.Utilities;

namespace XRL.World.Parts
{
	[Serializable]
	public class ItemDrop : IPart
	{
		/// PartsToDrop: A semicolon-separated list of entries of the form "item,chance,amount".
		public string PartsToDrop = "";
		public override bool WantEvent(int ID, int cascade)
		{
			if (!base.WantEvent(ID, cascade))
			{
				return ID == BeforeDeathRemovalEvent.ID;
			}
			return true;
		}

		protected string[] getPartsToDrop(BeforeDeathRemovalEvent E)
		{
			return PartsToDrop.Split(';');
		}

		public override bool HandleEvent(BeforeDeathRemovalEvent E)
		{
			GameObject InventoryObject = ParentObject.InInventory;
			if (InventoryObject?.IsCreature == false)
			{
				InventoryObject = null;
			}
			Cell cell = ((InventoryObject == null) ? ParentObject.GetDropCell() : null);
			if (InventoryObject != null || cell != null)
			{
				foreach (string entry in getPartsToDrop(E))
				{
					string[] parts = entry.Split(',');
					if (parts.Length < 1 || parts.Length > 3)
					{
						throw new Exception("Invalid parts to drop entry (wrong number of entries): " + entry);
					}
					string item = parts[0];
					int chance = (parts.Length > 1) ? Convert.ToInt32(parts[1]) : 100;
					int amount = (parts.Length > 2) ? XRL.Rules.Stat.RollCached(parts[2]) : 1;
					if (chance.in100())
					{
						for (int i = 0; i < amount; i++)
						{
							GameObject newObject = GameObject.create(item);
							XRL.World.Parts.Temporary.CarryOver(ParentObject, newObject);
							XRL.World.Capabilities.Phase.carryOver(ParentObject, newObject);
							if (InventoryObject != null)
							{
								InventoryObject.ReceiveObject(newObject);
							}
							else
							{
								cell.AddObject(newObject);
							}
						}
					}
				}
			}
			return base.HandleEvent(E);
		}
	}
}