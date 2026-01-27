using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RayTracing;

internal static partial class NativeMethods
{
    private const string LibName = "rt";

    [StructLayout(LayoutKind.Sequential)]
    internal struct CameraConfig
    {
        public double AspectRatio;
        public int ImageWidth;
        public int SamplesPerPixel;
        public int MaxDepth;
        public double Vfov;
        public double LookFromX, LookFromY, LookFromZ;
        public double LookAtX, LookAtY, LookAtZ;
        public double VupX, VupY, VupZ;
        public double DefocusAngle;
        public double FocusDist;
    }

    internal delegate void RenderCallback(int samples, IntPtr buffer);

    [LibraryImport(LibName)]
    internal static partial SceneSafeHandle CreateScene();

    [LibraryImport(LibName)]
    internal static partial void DestroyScene(IntPtr scene);

    [LibraryImport(LibName)]
    internal static partial void AddSphere(SceneSafeHandle scene, double cx, double cy, double cz, double r, MaterialSafeHandle mat);

    [LibraryImport(LibName)]
    internal static partial MaterialSafeHandle CreateLambertian(double r, double g, double b);

    [LibraryImport(LibName)]
    internal static partial MaterialSafeHandle CreateMetal(double r, double g, double b, double fuzz);

    [LibraryImport(LibName)]
    internal static partial MaterialSafeHandle CreateDielectric(double ir);

    [LibraryImport(LibName)]
    internal static partial void DestroyMaterial(IntPtr mat);

    [LibraryImport(LibName)]
    internal static partial void RenderScene(SceneSafeHandle world, CameraConfig config, IntPtr buffer, RenderCallback callback);

    [LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SavePng(string filename, int w, int h, IntPtr buffer);
}

internal class SceneSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SceneSafeHandle() : base(true) { }
    protected override bool ReleaseHandle()
    {
        NativeMethods.DestroyScene(handle);
        return true;
    }
}

internal class MaterialSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public MaterialSafeHandle() : base(true) { }
    protected override bool ReleaseHandle()
    {
        NativeMethods.DestroyMaterial(handle);
        return true;
    }
}