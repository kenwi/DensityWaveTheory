namespace DensityWaveTheory.Features.GalaxyPopulation;

/// <summary>
/// Port of beltoforion CumulativeDistributionFunction for realistic star radii.
/// </summary>
public sealed class CumulativeDistribution
{
    private double _fMin;
    private double _fMax;
    private int _nSteps;
    private double _i0;
    private double _k;
    private double _a;
    private double _rBulge;

    private readonly List<double> _vM1 = [];
    private readonly List<double> _vY1 = [];
    private readonly List<double> _vX1 = [];
    private readonly List<double> _vM2 = [];
    private readonly List<double> _vY2 = [];
    private readonly List<double> _vX2 = [];

    public void SetupRealistic(double i0, double k, double a, double rBulge, double min, double max, int nSteps)
    {
        _fMin = min;
        _fMax = max;
        _nSteps = nSteps;
        _i0 = i0;
        _k = k;
        _a = a;
        _rBulge = rBulge;
        BuildCdf(nSteps);
    }

    public double ValFromProb(double fVal)
    {
        if (fVal is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(fVal));

        var h = 1.0 / (_vY2.Count - 1);
        var i = (int)(fVal / h);
        if (i >= _vM2.Count)
            i = _vM2.Count - 1;
        var remainder = fVal - i * h;
        return _vY2[i] + _vM2[i] * remainder;
    }

    private void BuildCdf(int nSteps)
    {
        var h = (_fMax - _fMin) / nSteps;
        double y = 0;

        _vX1.Clear();
        _vY1.Clear();
        _vX2.Clear();
        _vY2.Clear();
        _vM1.Clear();
        _vM2.Clear();

        _vY1.Add(0.0);
        _vX1.Add(0.0);

        for (var i = 0; i < nSteps; i += 2)
        {
            var x = h * (i + 2);
            y += h / 3.0 * (Intensity(_fMin + i * h) + 4 * Intensity(_fMin + (i + 1) * h) + Intensity(_fMin + (i + 2) * h));
            _vM1.Add((y - _vY1[^1]) / (2 * h));
            _vX1.Add(x);
            _vY1.Add(y);
        }

        _vM1.Add(0.0);

        var norm = _vY1[^1];
        for (var i = 0; i < _vY1.Count; i++)
        {
            _vY1[i] /= norm;
            _vM1[i] /= norm;
        }

        _vX2.Add(0.0);
        _vY2.Add(0.0);

        h = 1.0 / nSteps;
        for (int i = 1, k = 0; i < nSteps; i++)
        {
            var p = i * h;
            while (k + 1 < _vY1.Count && _vY1[k + 1] <= p)
                k++;

            var yy = _vX1[k] + (p - _vY1[k]) / _vM1[k];
            _vM2.Add((yy - _vY2[^1]) / h);
            _vX2.Add(p);
            _vY2.Add(yy);
        }

        _vM2.Add(0.0);
    }

    private double IntensityBulge(double r, double i0, double k) => i0 * Math.Exp(-k * Math.Pow(r, 0.25));

    private double IntensityDisc(double r, double i0, double a) => i0 * Math.Exp(-r / a);

    private double Intensity(double x) =>
        x < _rBulge
            ? IntensityBulge(x, _i0, _k)
            : IntensityDisc(x - _rBulge, IntensityBulge(_rBulge, _i0, _k), _a);
}
