using System;
using System.Diagnostics;
using ConsoleLib.Console;
using qudbuilding;
using qudbuilding.Utilities;
using XRL.Core;
using XRL.UI;

namespace XRL.World.Effects
{
	[Serializable]
	public class Building : Effect
	{
		public Building()
		{
			base.DisplayName = "Building";
		}

		private BuildableEntry toBuild;
		private int Amount = 1;
		private int OriginalAmount = 1;
		private Cell targetCell;
		[NonSerialized]
		private static Stopwatch TurnTimer = new Stopwatch();
		private double Leftover = 0.0; /// The remainder of items that were not built in the last turn.

		public Building(BuildableEntry toBuild, int Amount = 1, Cell targetCell = null)
			: this()
		{
			this.Duration = toBuild.EnergyCost / 1000;
			this.toBuild = toBuild;
			this.Amount = Amount;
			this.OriginalAmount = Amount;
			this.targetCell = targetCell;
			MetricsManager.LogWarning($"Building {this.Amount}x {this.toBuild.DisplayName} for {this.toBuild.EnergyCost} energy (duration {this.Duration}, total {this.toBuild.EnergyCost * Amount / 1000})");
		}

		public override string GetDetails()
		{
			return $"Building {QudBuilding_Grammar.NumericalPluralize(toBuild.DisplayName, Amount)}.\nCan be interrupted if damage is taken.";
		}

		public override string GetDescription()
		{
			return "&gbuilding";
		}

		public override bool Apply(GameObject Object)
		{
			Object.ForfeitTurn();
			return base.Apply(Object);
		}

		public override void Remove(GameObject Object)
		{
			// Only use energy if we completed it; be lenient if we've been interrupted.
			if (Duration == 0 && Amount == 0)
			{
				int remainingEnergy = (toBuild.EnergyCost * OriginalAmount) % 1000;
				if (remainingEnergy > 0) Object.UseEnergy((toBuild.EnergyCost * OriginalAmount) % 1000, "Building");
			}
			if (Amount < OriginalAmount) // we've done work!
			{
				XRL.Messages.MessageQueue.AddPlayerMessage($"&yYou finish {((targetCell != null) ? "building" : "crafting")}.");
			}
			TurnTimer.Stop();
			base.Remove(Object);
		}

		public void FinishAssembling(int num)
		{
			for (int i = 0; i < num; i++)
			{
				GameObject resultObject = toBuild.Assemble(Object);
				if (targetCell != null)
				{
					targetCell.AddObject(resultObject);
				}
				else
				{
					Object.ReceiveObject(resultObject);
				}
			}
			string verb = (targetCell != null) ? "build" : "craft";
			XRL.Messages.MessageQueue.AddPlayerMessage($"&yYou {verb} {QudBuilding_Grammar.NumericalPluralize(toBuild.DisplayName, num)}.");
		}

		public override bool WantEvent(int ID, int cascade)
		{
			if (ID == BeforeTakeActionEvent.ID || ID == EndTurnEvent.ID)
			{
				return true;
			}
			return base.WantEvent(ID, cascade);
		}

		public override void Register(GameObject Object)
		{
			Object.RegisterEffectEvent(this, "TakeDamage");
			base.Register(Object);
		}

		public override void Unregister(GameObject Object)
		{
			Object.UnregisterEffectEvent(this, "TakeDamage");
			base.Unregister(Object);
		}

		public override bool Render(RenderEvent E)
		{
			if (base.Duration > 0)
			{
				int num = XRLCore.CurrentFrame % 60;
				if (num > 25 && num < 35)
				{
					E.Tile = null;
					E.RenderString = "B";
					E.ColorString = "&g";
				}
			}
			return true;
		}

		public override bool HandleEvent(EndTurnEvent E)
		{
			if (TurnTimer.IsRunning)
			{
				TurnTimer.Stop();
				int num33 = 1000 / 20;
				long elapsedMilliseconds = TurnTimer.ElapsedMilliseconds;
				if (elapsedMilliseconds < num33)
				{
					System.Threading.Thread.Sleep((int)(num33 - elapsedMilliseconds));
				}
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(BeforeTakeActionEvent E)
		{
			TurnTimer.Reset();
			TurnTimer.Start();
			if (Keyboard.kbhit()) // cancelled by keystroke
			{
				Object.RemoveEffect(this);
				Keyboard.getch();
				return false;
			}
			if (!Object.CanMoveExtremities())
			{
				Object.RemoveEffect(this);
				return false;
			}
			else if (Duration > 0)
			{
				Duration--;
				E.PreventAction = true;
				Object.ForfeitTurn();
				if (Duration == 0)
				{
					int amountToMake = 1;
					if (toBuild.EnergyCost < 1000)
					{
						Leftover += 1000.0 / (double) toBuild.EnergyCost;
						amountToMake = Math.Min((int) Leftover, Amount);
						Leftover -= amountToMake;
					}
					if (amountToMake > 0)
					{
						FinishAssembling(amountToMake);
						Amount -= amountToMake;
						if(Amount > 0)
						{
							Duration = toBuild.EnergyCost / 1000;
						}
					}
				}
				return false;
			}
			return base.HandleEvent(E);
		}

		public override bool FireEvent(Event E)
		{
			if (E.ID == "TakeDamage")
			{
				if ((E.GetParameter("Damage") as Damage).Amount > 0)
				{
					if (!base.Object.IsPlayer() || (Popup.ShowYesNo("You're taking damage, stop building?", AllowEscape: true, DialogResult.Yes) == DialogResult.Yes))
                    {
					    Object.RemoveEffect(this);
						return false;
					}
				}
			}
			return base.FireEvent(E);
		}
	}
}
