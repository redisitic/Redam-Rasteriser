public class Program
{
    public struct float3
    {
        public float x;
        public float y;
        public float z;
        public float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public float r { get => x; set => x = value; }
        public float g { get => y; set => y = value; }
        public float b { get => z; set => z = value; }
        public static float Dot(float3 a, float3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static float3 operator +(float3 a, float3 b) => new float3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static float3 operator -(float3 a, float3 b) => new float3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float3 operator *(float3 a, float scalar) => new float3(a.x * scalar, a.y * scalar, a.z * scalar);
        public static float3 operator *(float scalar, float3 a) => a * scalar;
        public static float3 operator /(float3 a, float scalar) => new float3(a.x / scalar, a.y / scalar, a.z / scalar);
        public static implicit operator float3(float2 v) => new float3(v.x, v.y, 0);
        public override string ToString() => $"({x}, {y}, {z})";
    }
    public struct float2
    {
        public float x;
        public float y;
        public float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public static implicit operator float2(float3 v) => new float2(v.x, v.y);
        public static float2 operator +(float2 a, float2 b) => new float2(a.x + b.x, a.y + b.y);
        public static float2 operator -(float2 a, float2 b) => new float2(a.x - b.x, a.y - b.y);
        public static float2 operator *(float2 a, float scalar) => new float2(a.x * scalar, a.y * scalar);
        public static float2 operator *(float2 a, float2 b) => new float2(a.x * b.x, a.y * b.y);
        public static float2 operator *(float scalar, float2 a) => a * scalar;
        public static float2 operator /(float2 a, float scalar) => new float2(a.x / scalar, a.y / scalar);
        public override string ToString() => $"({x}, {y})";
    }
    public static float Dot(float2 a, float2 b) => a.x * b.x + a.y * b.y;
    public static float2 Perpendicular(float2 v) => new float2(-v.y, v.x);
    public static bool PointOnRight(float2 a, float2 b, float2 p)
    {
        float2 ab = new float2(b.x - a.x, b.y - a.y);
        float2 ap = new float2(p.x - a.x, p.y - a.y);
        float cross = ab.x * ap.y - ab.y * ap.x;
        return cross < 0;
    }
    public static float2 RandomFloat2(Random rng, float width, float height)
    {
        float x = (float)rng.NextDouble() * width;
        float y = (float)rng.NextDouble() * height;
        return new float2(x, y);
    }
    static void WriteImagetoFile(float3[,] image, string fileName)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(fileName + ".bmp", FileMode.Create)))
        {
            uint width = (uint)image.GetLength(0);
            uint height = (uint)image.GetLength(1);

            uint headerSize = 14;
            uint dibHeaderSize = 40;
            uint pixelDataSize = width * height * 4;
            uint fileSize = headerSize + dibHeaderSize + pixelDataSize;
            writer.Write("BM"u8.ToArray());
            writer.Write(fileSize);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write(headerSize + dibHeaderSize);
            writer.Write(dibHeaderSize);
            writer.Write(width);
            writer.Write(height);
            writer.Write((ushort)1);
            writer.Write((ushort)(8 * 4));
            writer.Write(0u);
            writer.Write(pixelDataSize);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float3 pixel = image[x, y];
                    writer.Write((byte)(pixel.b * 255));
                    writer.Write((byte)(pixel.g * 255));
                    writer.Write((byte)(pixel.r * 255));
                    writer.Write((byte)255);
                }
            }
        }
    }
    static IEnumerable<string> SplitByLine(string input)
    {
        return input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
    public static float3[] LoadOBJFile(string objString)
    {
        List<float3> allPoints = new List<float3>();
        List<float3> trianglePoints = new List<float3>();

        foreach (string line in SplitByLine(objString))
        {
            if (line.StartsWith("v "))
            {
                float[] axes = line[2..].Split(' ').Select(float.Parse).ToArray();
                allPoints.Add(new float3(axes[0], axes[1], axes[2]));
            }
            else if (line.StartsWith("f "))
            {
                string[] faceIndexGroups = line[2..].Split(' ');
                for (int i = 0; i < faceIndexGroups.Length; i++)
                {
                    int[] indexGroup = faceIndexGroups[i].Split('/').Select(int.Parse).ToArray();
                    int pointIndex = indexGroup[0] - 1;
                    if (i >= 3) trianglePoints.Add(trianglePoints[^3]);
                    if (i >= 3) trianglePoints.Add(trianglePoints[^2]);
                    trianglePoints.Add(allPoints[pointIndex]);
                }
            }
        }
        return trianglePoints.ToArray();
    }
    public class RenderTarget(int w, int h)
    {
        public readonly float3[,] ColourBuffer = new float3[w, h];
        public readonly float[,] DepthBuffer = new float[w, h];
        public readonly int Width = w;
        public readonly int Height = h;
        public readonly float2 Size = new float2(w, h);
    }
    public class Model(float3[] points, float3[] cols)
    {
        public readonly float3[] OriginalPoints = points;
        public readonly float3[] TriangleCols = cols;
        public Transform Transform = new Transform();
        public float3[] GetTransformedPoints()
        {
            float3[] transformed = new float3[OriginalPoints.Length];
            for (int i = 0; i < OriginalPoints.Length; i++)
            {
                transformed[i] = Transform.ToWorldPoint(OriginalPoints[i]);
            }
            return transformed;
        }
    }
    public class Transform
    {
        public float yaw;
        public float pitch;
        public float3 Position;
        public float3 ToWorldPoint(float3 p)
        {
            (float3 ihat, float3 jhat, float3 khat) = GetBasisVectors();
            return TransformVector(ihat, jhat, khat, p) + Position;
        }
        private (float3 ihat, float3 jhat, float3 khat) GetBasisVectors()
        {
            float3 ihat_yaw = new float3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw));
            float3 jhat_yaw = new float3(0, 1, 0);
            float3 khat_yaw = new float3(-(float)Math.Sin(yaw), 0, (float)Math.Cos(yaw));
            float3 ihat_pitch = new float3(1, 0, 0);
            float3 jhat_pitch = new float3(0, (float)Math.Cos(pitch), -(float)Math.Sin(pitch));
            float3 khat_pitch = new float3(0, (float)Math.Sin(pitch), (float)Math.Cos(pitch));
            float3 ihat = TransformVector(ihat_yaw, jhat_yaw, khat_yaw, ihat_pitch);
            float3 jhat = TransformVector(ihat_yaw, jhat_yaw, khat_yaw, jhat_pitch);
            float3 khat = TransformVector(ihat_yaw, jhat_yaw, khat_yaw, khat_pitch);
            return (ihat, jhat, khat);
        }
        private float3 TransformVector(float3 ihat, float3 jhat, float3 khat, float3 v)
        {
            return (v.x * ihat) + (v.y * jhat) + (v.z * khat);
        }
    }
    public static float SignedTriangleArea(float2 a, float2 b, float2 c)
    {
        float2 ac = c - a;
        float2 abPerp = Perpendicular(b - a);
        return Dot(ac, abPerp) / 2f;
    }
    static bool PointInTriangle(float2 a, float2 b, float2 c, float2 p, out float3 weights)
    {
        float areaABP = SignedTriangleArea(a, b, p);
        float areaBCP = SignedTriangleArea(b, c, p);
        float areaCAP = SignedTriangleArea(c, a, p);
        bool inTri = areaABP <= 0 && areaBCP <= 0 && areaCAP <= 0;
        float totalArea = Math.Abs(areaABP + areaBCP + areaCAP);
        if (totalArea == 0)
        {
            weights = new float3(0, 0, 0);
            return false;
        }
        float invArea = 1f / totalArea;
        float weightA = Math.Abs(areaBCP) * invArea;
        float weightB = Math.Abs(areaCAP) * invArea;
        float weightC = Math.Abs(areaABP) * invArea;
        weights = new float3(weightA, weightB, weightC);
        return inTri && totalArea > 0;
    }
    static float3 VertexToScreen(float3 vertex_world, float2 screenSize, float fov)
    {
        float screenHeight_world = (float)Math.Tan(fov / 2) * 2;
        float pixelsPerWorldUnit = screenSize.y / screenHeight_world / vertex_world.z;

        float2 pixelOffset = new float2(vertex_world.x, vertex_world.y) * pixelsPerWorldUnit;
        float2 vertexScreen = screenSize / 2 + pixelOffset;
        return new float3(vertexScreen.x, vertexScreen.y, vertex_world.z);
    }
    static void ClearBuffers(RenderTarget target)
    {
        int width = target.Width;
        int height = target.Height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                target.ColourBuffer[x, y] = new float3(0, 0, 0);
                target.DepthBuffer[x, y] = float.MaxValue;
            }
        }
    }
    static float3[,] Render(Model model, RenderTarget target, int frame, bool visualizeDepth = false)
    {
        int width = target.Width;
        int height = target.Height;
        float fov = 60f * (float)Math.PI / 180f;
        float3[,] image = target.ColourBuffer;
        float3[] points = model.GetTransformedPoints();
        float3[] triangleCols = model.TriangleCols;
        float nearPlane = 1f;
        float farPlane = 5f;
        for (int i = 0; i < points.Length; i += 3)
        {
            float3 a = VertexToScreen(points[i], target.Size, fov);
            float3 b = VertexToScreen(points[i + 1], target.Size, fov);
            float3 c = VertexToScreen(points[i + 2], target.Size, fov);
            float minXf = MathF.Min(a.x, MathF.Min(b.x, c.x));
            float maxXf = MathF.Max(a.x, MathF.Max(b.x, c.x));
            float minYf = MathF.Min(a.y, MathF.Min(b.y, c.y));
            float maxYf = MathF.Max(a.y, MathF.Max(b.y, c.y));
            int minX = (int)MathF.Max(0, MathF.Floor(minXf));
            int maxX = (int)MathF.Min(width - 1, MathF.Ceiling(maxXf));
            int minY = (int)MathF.Max(0, MathF.Floor(minYf));
            int maxY = (int)MathF.Min(height - 1, MathF.Ceiling(maxYf));
            float3 col = triangleCols[i / 3];
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float2 p = new float2(x, y);
                    float3 weights;
                    if (PointInTriangle((float2)a, (float2)b, (float2)c, p, out weights))
                    {
                        float3 depths = new float3(a.z, b.z, c.z);
                        float depth = float3.Dot(depths, weights);
                        if (depth < target.DepthBuffer[x, y])
                        {
                            if (visualizeDepth)
                            {
                                float normalizedDepth = MathF.Max(0, MathF.Min(1, (depth - nearPlane) / (farPlane - nearPlane)));
                                image[x, y] = new float3(normalizedDepth, normalizedDepth, normalizedDepth);
                            }
                            else
                            {
                                image[x, y] = col;
                            }
                            target.DepthBuffer[x, y] = depth;
                        }
                    }
                }
            }
        }
        return image;
    }
    public static void RenderModel()
    {
        string objPath = Path.Combine(Directory.GetCurrentDirectory(), "models", "suzanne.obj");
        string objString = File.ReadAllText(objPath);
        float3[] cubeModelPoints = LoadOBJFile(objString);
        Random rng = new();
        float3[] triangleCols = new float3[cubeModelPoints.Length / 3];
        for (int i = 0; i < triangleCols.Length; i++)
        {
            triangleCols[i] = new float3(((rng.Next(128) + 112) / 255f), ((rng.Next(128) + 112) / 255f), ((rng.Next(128) + 112) / 255f));
        }
        Model cubeModel = new Model(cubeModelPoints, triangleCols);
        cubeModel.Transform.Position = new float3(0, 0, 3f);
        float inYaw = (float)Math.PI;
        RenderTarget renderTarget = new RenderTarget(960, 540);
        bool renderDepth = false;
        const int frameCount = 120;
        for (int frame = 0; frame < frameCount; frame++)
        {
            cubeModel.Transform.yaw = (float)(frame * Math.PI * 2f / frameCount) + inYaw;
            ClearBuffers(renderTarget);
            Render(cubeModel, renderTarget, frame, renderDepth);
            WriteImagetoFile(renderTarget.ColourBuffer, $"Raster{frame:D2}");
            Console.WriteLine($"Raster image created for frame {frame}");
        }
    }
    public static void Main(string[] args)
    {
        RenderModel();
    }
}