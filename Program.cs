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
        public static float2 operator +(float2 a, float2 b) => new float2(a.x + b.x, a.y + b.y);
        public static float2 operator -(float2 a, float2 b) => new float2(a.x - b.x, a.y - b.y);
        public static float2 operator *(float2 a, float scalar) => new float2(a.x * scalar, a.y * scalar);
        public static float2 operator *(float2 a, float2 b) => new float2(a.x*b.x, a.y*b.y);
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
    static bool PointInTriangle(float2 a, float2 b, float2 c, float2 p)
    {
        bool sideAB = PointOnRight(a, b, p);
        bool sideBC = PointOnRight(b, c, p);
        bool sideCA = PointOnRight(c, a, p);
        return (sideAB && sideBC && sideCA);
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
    public class RenderTarget(int w, int h)
    {
        public readonly float3[,] ColourBuffer = new float3[w, h];
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
        public float roll;
        public float3 ToWorldPoint(float3 p)
        {
            (float3 ihat, float3 jhat, float3 khat) = GetBasisVectors();
            return TransformVector(ihat, jhat, khat, p);
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
            return v.x * ihat + v.y * jhat + v.z * khat;
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
    static float2 WorldtoScreen(float3 vertex, float2 numPixels)
    {
        float screenHeightWorld = 5f;
        float pixelsPerWorldUnit = numPixels.y / screenHeightWorld;
        float2 pixelOffset = new float2(vertex.x, vertex.y) * pixelsPerWorldUnit;
        return numPixels / 2f + pixelOffset;
    }
    static float3[,] Render(Model model, RenderTarget target, int frame)
    {
        int width = target.Width;
        int height = target.Height;
        float3[,] image = target.ColourBuffer;
        float3[] points = model.GetTransformedPoints();
        float3[] triangleCols = model.TriangleCols; 
        for (int i = 0; i < points.Length; i += 3)
        {
            float2 a = WorldtoScreen(points[i], target.Size);
            float2 b = WorldtoScreen(points[i + 1], target.Size);
            float2 c = WorldtoScreen(points[i + 2], target.Size);
            float minXf = MathF.Min(a.x, MathF.Min(b.x, c.x));
            float maxXf = MathF.Max(a.x, MathF.Max(b.x, c.x));
            float minYf = MathF.Min(a.y, MathF.Min(b.y, c.y));
            float maxYf = MathF.Max(a.y, MathF.Max(b.y, c.y));
            int minX = (int)MathF.Max(0, MathF.Floor(minXf));
            int maxX = (int)MathF.Min(width - 1, MathF.Ceiling(maxXf));
            int minY = (int)MathF.Max(0, MathF.Floor(minYf));
            int maxY = (int)MathF.Min(height - 1, MathF.Ceiling(maxYf));
            float2 v0 = b - a;
            float2 v1 = c - a;
            float denom = v0.x * v1.y - v1.x * v0.y;
            if (denom == 0) continue;
            float3 col = triangleCols[i / 3];
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float2 p = new float2(x, y);
                    float2 v2 = p - a;
                    float u = (v2.x * v1.y - v1.x * v2.y) / denom;
                    float v = (v0.x * v2.y - v2.x * v0.y) / denom;
                    if (PointInTriangle(a, b, c, p))
                    {
                        image[x, y] = col;
                    }
                }
            }
        }
        return image;
    }
    static float3[] ToFloat3(float2[] points)
    {
        float3[] result = new float3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            result[i] = new float3(points[i].x, points[i].y, 0);
        }
        return result;
    }
    static void ClearImage(float3[,] image)
    {
        int width = image.GetLength(0);
        int height = image.GetLength(1);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                image[x, y] = new float3(0, 0, 0);
    }
    public static void CreateTestAnimation()
    {
        const int width = 960;
        const int height = 540;
        const int triangleCount = 250;
        const int frameCount = 60;
        const float speed = 0.1f;
        const float worldWidth = 5f;
        float worldHeight = worldWidth * height / (float)width;
        float2 halfWorld = new float2(worldWidth, worldHeight);
        float2 quarterWorld = halfWorld / 2f;
        float2[] points = new float2[triangleCount * 3];
        float2[] velocities = new float2[points.Length];
        float3[] triangleCols = new float3[triangleCount];
        Random rng = new();
        for (int i = 0; i < points.Length; i++)
        {
            float2 offset = (RandomFloat2(rng, 1, 1) - new float2(0.5f, 0.5f)) * 2f * quarterWorld;
            points[i] = offset;
        }
        for (int i = 0; i < velocities.Length; i += 3)
        {
            float2 velocity = (RandomFloat2(rng, 1, 1) - new float2(0.5f, 0.5f)) * 1.5f;
            velocities[i] = velocity;
            velocities[i + 1] = velocity;
            velocities[i + 2] = velocity;

            triangleCols[i / 3] = new float3(rng.Next(256) / 255f, rng.Next(256) / 255f, rng.Next(256) / 255f);
        }
        RenderTarget target = new RenderTarget(width, height);
        for (int frame = 0; frame < frameCount; frame++)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] += velocities[i] * speed;
                if (points[i].x < -halfWorld.x || points[i].x > halfWorld.x)

                    velocities[i].x *= -1;
                if (points[i].y < -halfWorld.y || points[i].y > halfWorld.y)
                    velocities[i].y *= -1;
            }
            Model model = new Model(ToFloat3(points), triangleCols);
            ClearImage(target.ColourBuffer);
            Render(model, target, frame);
            WriteImagetoFile(target.ColourBuffer, $"art_{frame:D2}");
            Console.WriteLine($"Test image created with dimensions {width}x{height}. Frame Number: {frame}");
        }
    }


    public static void RenderCube()
    {
        string objPath = Path.Combine(Directory.GetCurrentDirectory(), "models", "cube.obj");
        string objString = File.ReadAllText(objPath);
        float3[] cubeModelPoints = LoadOBJFile(objString);
        Random rng = new();
        float3[] triangleCols = new float3[cubeModelPoints.Length / 3];
        for (int i = 0; i < triangleCols.Length; i++)
        {
            triangleCols[i] = new float3(rng.Next(256) / 255f, rng.Next(256) / 255f, rng.Next(256) / 255f);
        }
        Model cubeModel = new Model(cubeModelPoints, triangleCols);
        RenderTarget renderTarget = new RenderTarget(960, 540);
        float3[,] image = new float3[renderTarget.Width, renderTarget.Height];
        const int frameCount = 60;
        for (int frame = 0; frame < frameCount; frame++)
        {
            cubeModel.Transform.yaw = (float)(frame * Math.PI * -2 / frameCount);
            cubeModel.Transform.pitch = (float)(Math.PI / 4 * Math.Sin(frame * Math.PI * 2 / frameCount));
            ClearImage(renderTarget.ColourBuffer);
            WriteImagetoFile(Render(cubeModel, renderTarget, frame), $"cube_frame_{frame:D2}");
            Console.WriteLine($"Cube image created for frame {frame}");
        }
    }
    public static void Main(string[] args)
    {
        RenderCube();
    }
}