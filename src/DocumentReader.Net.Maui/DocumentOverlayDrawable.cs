namespace DocumentReader.Net.Maui;

public sealed class DocumentOverlayDrawable : IDrawable
{
    public IReadOnlyList<DocPoint>? Corners { get; set; }
    public bool Locked { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Corners == null || Corners.Count < 4)
        {
            return;
        }

        var path = new PathF();
        path.MoveTo((float)Corners[0].X, (float)Corners[0].Y);
        path.LineTo((float)Corners[1].X, (float)Corners[1].Y);
        path.LineTo((float)Corners[2].X, (float)Corners[2].Y);
        path.LineTo((float)Corners[3].X, (float)Corners[3].Y);
        path.Close();
        canvas.StrokeColor = Locked ? DemoTheme.Accent : Color.FromArgb("#60A5FA");
        canvas.StrokeSize = Locked ? 8 : 5;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
    }
}
