using System.Runtime.InteropServices;

namespace RayTracing;

public class Scene : IDisposable
{
    private readonly SceneSafeHandle _handle = NativeMethods.CreateScene();
    private readonly List<Material> _materials = new();

    public void AddSphere(double x, double y, double z, double r, Material mat)
    {
        _materials.Add(mat);
        NativeMethods.AddSphere(_handle, x, y, z, r, mat.Handle);
    }

    public void Render(CameraSettings settings, Action<int, ReadOnlySpan<byte>> onUpdate)
    {
        var height = (int)(settings.Width / settings.AspectRatio);
        var bufferSize = settings.Width * height * 4;
        var buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            var config = new NativeMethods.CameraConfig
            {
                AspectRatio = settings.AspectRatio,
                ImageWidth = settings.Width,
                SamplesPerPixel = settings.Samples,
                MaxDepth = settings.MaxDepth,
                Vfov = settings.Vfov,
                LookFromX = settings.LookFrom.X, LookFromY = settings.LookFrom.Y, LookFromZ = settings.LookFrom.Z,
                LookAtX = settings.LookAt.X, LookAtY = settings.LookAt.Y, LookAtZ = settings.LookAt.Z,
                VupX = 0, VupY = 1, VupZ = 0,
                DefocusAngle = settings.DefocusAngle,
                FocusDist = settings.FocusDist
            };

            NativeMethods.RenderCallback callback = (samples, ptr) =>
            {
                unsafe { onUpdate(samples, new ReadOnlySpan<byte>(ptr.ToPointer(), bufferSize)); }
            };

            NativeMethods.RenderScene(_handle, config, buffer, callback);
            NativeMethods.SavePng("output.png", settings.Width, height, buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose() => GC.SuppressFinalize(this);
}

public record struct CameraSettings(
    double AspectRatio, int Width, int Samples, int MaxDepth, 
    double Vfov, System.Numerics.Vector3 LookFrom, System.Numerics.Vector3 LookAt,
    double DefocusAngle, double FocusDist);