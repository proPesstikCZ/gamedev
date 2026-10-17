using System;
using System.Collections.Generic;

namespace Game.Core
{
    [Serializable]
    public struct YearPoint
    {
        public int Year;
        public float Value;

        public YearPoint(int year, float value)
        {
            Year = year;
            Value = value;
        }
    }

    /// <summary>
    /// Piecewise linear curve over years. Returns 0 before the first point
    /// and holds the last value after the last point.
    /// </summary>
    public sealed class YearCurve
    {
        private readonly YearPoint[] _points;

        public YearCurve(IEnumerable<YearPoint> points)
        {
            var list = new List<YearPoint>(points ?? Array.Empty<YearPoint>());
            list.Sort((a, b) => a.Year.CompareTo(b.Year));
            _points = list.ToArray();
        }

        public IReadOnlyList<YearPoint> Points => _points;

        public float Evaluate(float year)
        {
            if (_points.Length == 0 || year < _points[0].Year) return 0f;
            for (int i = 0; i < _points.Length - 1; i++)
            {
                var a = _points[i];
                var b = _points[i + 1];
                if (year <= b.Year)
                {
                    if (b.Year == a.Year) return b.Value;
                    float t = (year - a.Year) / (b.Year - a.Year);
                    return a.Value + (b.Value - a.Value) * t;
                }
            }
            return _points[_points.Length - 1].Value;
        }
    }
}
