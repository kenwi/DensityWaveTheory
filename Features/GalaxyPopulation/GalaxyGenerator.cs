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

        var countEstimate = p.NumStars + p.NumDust + p.NumDust / 100 * 50 + p.NumH2 * 2;
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

        return stars.ToArray();
    }
}
