using System.Diagnostics;
using System.Numerics;
using RayTracing;
using Windowing;

namespace RayTracingDemo;

class Program
{
    static void Main(string[] args)
    {
        const double aspect = 16.0 / 9.0;
        const int width = 1440;
        const int height = (int)(width / aspect);

        var settings = new CameraSettings(
            AspectRatio: aspect,
            Width: width,
            Samples: 100,
            MaxDepth: 100,
            Vfov: 20,
            LookFrom: new Vector3(13, 2, 5),
            LookAt: new Vector3(0, 0, 0),
            DefocusAngle: 0.6,
            FocusDist: 10.0
        );

        Viewer.Show(width, height, "RayTracing", (updater) =>
        {
            using var scene = CreateCoverScene();
            var sw = Stopwatch.StartNew();

            scene.Render(settings, (sample, data) =>
            {
                if (updater.IsClosed) return;
                updater.UpdateImage(data);
                updater.UpdateStatus($"Samples: {sample}/{settings.Samples} | Time: {sw.Elapsed:mm\\:ss}");
            });
            
            updater.UpdateStatus($"Done! Saved image to output.png | Total time: {sw.Elapsed:mm\\:ss}");
        });
    }

    static Scene CreateCoverScene()
    {
        var scene = new Scene();
        var rnd = new Random();

        scene.AddSphere(0, -1000, 0, 1000, new Lambertian(0.5, 0.5, 0.5));

        const float deadzone = 2f;

        for (var a = -10; a < 10; a++) {
            for (var b = -10; b < 10; b++) {
                var chooseMat = rnd.NextDouble();
                Vector3 center = new(
                    (float)(a + (rnd.NextDouble() - 0.5f) * 0.8f),
                    0.2f,
                    (float)(b + (rnd.NextDouble() - 0.5f) * 0.8f)
                    );

                if ((center - new Vector3(0, 0.2f, 0)).Length() > deadzone) {
                    Material mat = chooseMat switch {
                        < 0.5 => new Lambertian(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble()),
                        < 0.9 => new Metal(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), 0.3 * rnd.NextDouble()),
                        _ => new Dielectric(1.5)
                    };
                    scene.AddSphere(center.X, center.Y, center.Z, 0.2, mat);
                }
            }
        }

        scene.AddSphere(0, 1, 0, 1.0, new Dielectric(1.5));
        scene.AddSphere(-4, 1, 0, 1.0, new Lambertian(0.2, 0.8, 0.6));
        scene.AddSphere(4, 1, 0, 1.0, new Metal(0.7, 0.6, 0.4, 0.1));

        return scene;
    }
}