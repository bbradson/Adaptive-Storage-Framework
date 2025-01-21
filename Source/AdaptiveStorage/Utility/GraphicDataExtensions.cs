namespace AdaptiveStorage.Utility;

public static class GraphicDataExtensions
{
	public static Graphic GraphicColoredFor(this GraphicData graphicData, in Color drawColor, in Color drawColorTwo)
		=> graphicData.Graphic is var graphic
			&& drawColor.IndistinguishableFrom(graphic.Color)
			&& drawColorTwo.IndistinguishableFrom(graphic.ColorTwo)
				? graphic
				: graphic.GetColoredVersion(graphic.Shader, drawColor, drawColorTwo);

	public static GraphicScaleScope Scaled(this Graphic graphic, Vector2 drawScale) => new(graphic, drawScale);

	public static GraphicTransformScope Transformed(this Graphic graphic, in TransformData transform)
		=> new(graphic, transform);
}