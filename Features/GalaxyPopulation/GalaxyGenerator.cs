namespace DensityWaveTheory.Features.GalaxyPopulation;

public sealed class GalaxyGenerator
{
    public Star[] Generate(GalaxyParams p)
    {
        var rng = new Random((int)p.Seed);
        float Rnum() => (float)rng.NextDouble();

        var cdf = new CumulativeDistribution();
        cdf.SetupRealistic(
            1.0,
            0.02,
            p.RadGalaxy / 3.0,
            p.RadCore,
            0,
            p.RadFarField,
            1000);

        var countEstimate = p.NumStars + p.NumDust + p.NumDust / 100 * 50 + p.NumH2 * 2 + p.NumDustLanes;
        var stars = new List<Star>(countEstimate);

        for (var i = 0; i < p.NumStars; i++)
        {
            var rad = (float)cdf.ValFromProb(Rnum());
            var mag = 0.1f + 0.4f * Rnum();
            if (i < p.NumStars / 60)
                mag = MathF.Min(mag + 0.1f + Rnum() * 0.4f, 1f);

            var star = new Star
            {
                A = rad,
                B = rad * OrbitMath.GetExcentricity(p, rad),
                TiltAngle = OrbitMath.GetAngularOffset(p, rad),
                Theta0 = 360f * Rnum(),
                VelTheta = OrbitMath.GetOrbitalVelocity(p, rad),
                Temp = 6000f + (4000f * Rnum() - 2000f),
                Mag = mag,
                Type = (int)ParticleType.Star,
            };
            star.SetColorFromTemperature();
            stars.Add(star);
        }

        for (var i = 0; i < p.NumDust; i++)
        {
            float rad;
            if (i % 2 == 0)
            {
                rad = (float)cdf.ValFromProb(Rnum());
            }
            else
            {
                var x = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
                var y = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
                rad = MathF.Sqrt(x * x + y * y);
            }

            var dust = new Star
            {
                A = rad,
                B = rad * OrbitMath.GetExcentricity(p, rad),
                TiltAngle = OrbitMath.GetAngularOffset(p, rad),
                Theta0 = 360f * Rnum(),
                VelTheta = OrbitMath.GetOrbitalVelocity(p, (rad + rad * OrbitMath.GetExcentricity(p, rad)) / 2f),
                Temp = p.BaseTemp + rad / 4.5f,
                Mag = 0.02f + 0.15f * Rnum(),
                Type = (int)ParticleType.Dust,
            };
            dust.SetColorFromTemperature();
            // Photo look: kill muddy orange blackbody in the inner disk.
            if (p.PhotoLook && rad < p.RadGalaxy * 0.75f)
            {
                var t = Math.Clamp(rad / (p.RadGalaxy * 0.75f), 0f, 1f);
                static float Lerp(float a, float b, float u) => a + (b - a) * u;
                // Inner: cream, mid: soft tan, outer: cool.
                var u = t < 0.45f ? 0f : (t - 0.45f) / 0.55f;
                var tr = t < 0.45f ? 1.0f : Lerp(1.0f, 0.70f, u);
                var tg = t < 0.45f ? 0.95f : Lerp(0.95f, 0.82f, u);
                var tb = t < 0.45f ? 0.88f : Lerp(0.88f, 1.15f, u);
                var w = 0.55f * (1f - 0.35f * t);
                dust.ColorR = Math.Clamp(Lerp(dust.ColorR, tr, w), 0f, 1.6f);
                dust.ColorG = Math.Clamp(Lerp(dust.ColorG, tg, w), 0f, 1.6f);
                dust.ColorB = Math.Clamp(Lerp(dust.ColorB, tb, w), 0f, 1.6f);
            }
            stars.Add(dust);
        }

        var filamentBundles = p.NumDust / 100;
        for (var i = 0; i < filamentBundles; i++)
        {
            var x = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
            var y = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
            var rad = MathF.Sqrt(x * x + y * y);
            var theta = 360f * Rnum();
            var mag = 0.1f + 0.05f * Rnum();
            var num = (int)(100 * Rnum());

            for (var j = 0; j < num; j++)
            {
                rad += 200f - 400f * Rnum();
                var filament = new Star
                {
                    A = rad,
                    B = rad * OrbitMath.GetExcentricity(p, rad),
                    TiltAngle = OrbitMath.GetAngularOffset(p, rad),
                    Theta0 = theta + 10f - 20f * Rnum(),
                    VelTheta = OrbitMath.GetOrbitalVelocity(p, (rad + rad * OrbitMath.GetExcentricity(p, rad)) / 2f),
                    Temp = p.BaseTemp + rad / 4.5f - 1000f,
                    Mag = mag + 0.025f * Rnum(),
                    Type = (int)ParticleType.Filament,
                };
                filament.SetColorFromTemperature();
                stars.Add(filament);
            }
        }

        for (var i = 0; i < p.NumH2; i++)
        {
            var x = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
            var y = 2f * p.RadGalaxy * Rnum() - p.RadGalaxy;
            var rad = MathF.Sqrt(x * x + y * y);
            var particle = new Star
            {
                A = rad,
                B = rad * OrbitMath.GetExcentricity(p, rad),
                TiltAngle = OrbitMath.GetAngularOffset(p, rad),
                Theta0 = 360f * Rnum(),
                VelTheta = OrbitMath.GetOrbitalVelocity(p, (rad + rad * OrbitMath.GetExcentricity(p, rad)) / 2f),
                Temp = 6000f + 6000f * Rnum() - 3000f,
                Mag = 0.1f + 0.05f * Rnum(),
                Type = (int)ParticleType.H2Halo,
            };
            particle.SetColorFromTemperature();
            stars.Add(particle);
            particle.Type = (int)ParticleType.H2Core;
            stars.Add(particle);
        }

        // Continuous spiral dust filaments (smooth walk - random jumps made blotches).
        var laneBundles = Math.Max(0, p.NumDustLanes / 60);
        for (var i = 0; i < laneBundles; i++)
        {
            var rad = p.RadCore * 0.8f + (p.RadGalaxy * 0.55f - p.RadCore * 0.5f) * Rnum();
            // Prefer inner edges of the two arms.
            var theta = (i % 2) * 180f + 8f * Rnum();
            var num = 28 + (int)(36 * Rnum());
            var mag = 0.12f + 0.14f * Rnum();
            for (var j = 0; j < num; j++)
            {
                rad += 40f + 30f * Rnum();
                if (rad > p.RadGalaxy * 0.92f)
                    break;
                // Follow density-wave pitch so the filament stays continuous.
                theta += (OrbitMath.GetAngularOffset(p, rad) - OrbitMath.GetAngularOffset(p, Math.Max(rad - 50f, p.RadCore)))
                         / MathF.PI * 180f * 0.35f;
                var lane = new Star
                {
                    A = rad,
                    B = rad * OrbitMath.GetExcentricity(p, rad),
                    TiltAngle = OrbitMath.GetAngularOffset(p, rad) + 0.035f + 0.02f * Rnum(),
                    Theta0 = theta + 1.5f - 3f * Rnum(),
                    VelTheta = OrbitMath.GetOrbitalVelocity(p, (rad + rad * OrbitMath.GetExcentricity(p, rad)) / 2f),
                    Temp = 1800f,
                    Mag = mag * (0.7f + 0.4f * Rnum()),
                    Type = (int)ParticleType.DustLane,
                    ColorR = 0.70f + 0.08f * Rnum(),
                    ColorG = 0.56f + 0.06f * Rnum(),
                    ColorB = 0.46f + 0.05f * Rnum(),
                    ColorA = 1f,
                };
                stars.Add(lane);
            }
        }

        return stars.ToArray();
    }
}
