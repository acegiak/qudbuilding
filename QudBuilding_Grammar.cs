using XRL.Language;
using XRL.World;

namespace qudbuilding.Utilities
{
	public static class QudBuilding_Grammar
	{
		public static string NumericalPluralize(string name, int count, string glue=" ", string pre="", string post="")
		{
			return count > 1 ? $"{pre}{Grammar.Cardinal(count)}{glue}{Grammar.Pluralize(name)}{post}" : $"{pre}{Grammar.A(name)}{post}";
		}
		public static string NumericalPluralize(GameObject gameObj, int count, string glue=" ", string pre="", string post="")
		{
			string name = gameObj.ShortDisplayName;
			if (gameObj.IsPlural)
			{
				post = $" of {name}{post}";
				name = gameObj.GetxTag("Grammar", "groupTerm", "handful");
			}
			return NumericalPluralize(name, count, glue, pre, post);
		}
	}
}