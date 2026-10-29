namespace ChainedBeasts.DivineNumbers;

// Keep visible offers separate from their focus score: an unfocused or
// unsupported potion must not reveal an unrelated collapsed-HUD skill.
internal sealed class InspectionSelection<T> where T : class
{
    private T? _best, _bestWithoutHud;
    private int _score, _scoreWithoutHud;
    internal bool OffersVisible { get; private set; }

    internal void Consider(T value, int score, bool hud = false, bool offer = false)
    {
        OffersVisible |= offer;
        if (score > _score) { _score = score; _best = value; }
        if (!hud && score > _scoreWithoutHud) { _scoreWithoutHud = score; _bestWithoutHud = value; }
    }

    internal T? Selected(bool controller) => controller && OffersVisible ? _bestWithoutHud : _best;
}
