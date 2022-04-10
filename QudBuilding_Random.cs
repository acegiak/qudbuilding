using System;
using XRL;
using XRL.Core;
using XRL.Rules;

namespace qudbuilding.Utilities
{
	[HasGameBasedStaticCache]
	public static class QudBuilding_Random
	{
		private static Random _rand;
		public static Random Rand
		{
			get
			{
				if (_rand == null)
				{
					if (XRLCore.Core?.Game == null)
					{
						throw new Exception("QudBuilding mod attempted to retrieve Random, but Game is not created yet.");
					}
					else if (XRLCore.Core.Game.IntGameState.ContainsKey("QudBuilding:Random"))
					{
						int seed = XRLCore.Core.Game.GetIntGameState("QudBuilding:Random");
						_rand = new Random(seed);
					}
					else
					{
						_rand = Stat.GetSeededRandomGenerator("QudBuilding");
					}
					XRLCore.Core.Game.SetIntGameState("QudBuilding:Random", _rand.Next());
				}
				return _rand;
			}
		}

		[GameBasedCacheInit]
		public static void ResetRandom()
		{
			_rand = null;
		}

		public static int Next(int minInclusive, int maxInclusive)
		{
			return Rand.Next(minInclusive, maxInclusive + 1);
		}
	}
}