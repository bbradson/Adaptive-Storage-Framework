namespace AdaptiveStorage;

public static class GraphicDataExtensions
{
	public static Graphic GraphicColoredFor(this GraphicData graphicData, in Color drawColor, in Color drawColorTwo)
		=> drawColor.IndistinguishableFrom(graphicData.Graphic.Color)
			&& drawColorTwo.IndistinguishableFrom(graphicData.Graphic.ColorTwo)
				? graphicData.Graphic
				: graphicData.Graphic.GetColoredVersion(graphicData.Graphic.Shader, drawColor, drawColorTwo);
}