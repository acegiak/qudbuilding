using System;
using XRL.Rules;
using XRL.World.Parts;

namespace XRL.World.PartBuilders
{
	public class NoGraffiti : IPartBuilder
	{
		public void BuildPart(IPart iPart, string Context = null)
		{
            if (iPart is Graffitied)
            {
                iPart.ParentObject.RemovePart(iPart);
            }
		}
	}
}
