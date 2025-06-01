using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public static float3 Cross(float3 a, float3 b) => new float3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float3 Normalize(float3 v)
        {
            float len = MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            return len > 0 ? v / len : new float3(0, 0, 0);
        }
        public static float3 operator +(float3 a, float3 b) => new float3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static float3 operator -(float3 a, float3 b) => new float3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float3 operator *(float3 a, float scalar) => new float3(a.x * scalar, a.y * scalar, a.z * scalar);
        public static float3 operator *(float3 a, float3 b) => new float3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static float3 operator *(float scalar, float3 a) => a * scalar;
        public static float3 operator /(float3 a, float scalar) => new float3(a.x / scalar, a.y / scalar, a.z / scalar);
        public static implicit operator float3(float2 v) => new float3(v.x, v.y, 0);
        public override string ToString() => $"({x}, {y}, {z})";
        public override int GetHashCode()
        {
            const float epsilon = 1e-6f;
            int hashX = ((int)(x / epsilon)).GetHashCode();
            int hashY = ((int)(y / epsilon)).GetHashCode();
            int hashZ = ((int)(z / epsilon)).GetHashCode();
            return HashCode.Combine(hashX, hashY, hashZ);
        }
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
                    writer.Write((byte)(MathF.Max(0, MathF.Min(255, pixel.b * 255))));
                    writer.Write((byte)(MathF.Max(0, MathF.Min(255, pixel.g * 255))));
                    writer.Write((byte)(MathF.Max(0, MathF.Min(255, pixel.r * 255))));
                    writer.Write((byte)255);
                }
            }
        }
    }
    static IEnumerable<string> SplitByLine(string input)
    {
        return input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
    public struct OBJData
    {
        public float3[] trianglePoints;
        public float3[] triangleNormals;
    }
    public static OBJData LoadOBJFile(string objString)
    {
        List<float3> allPoints = new List<float3>();
        List<float3> trianglePoints = new List<float3>();
        List<float3> triangleNormals = new List<float3>();

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
                List<int> pointIndices = new List<int>();
                foreach (string group in faceIndexGroups)
                {
                    string[] parts = group.Split('/');
                    pointIndices.Add(int.Parse(parts[0]) - 1);
                }
                for (int i = 1; i < pointIndices.Count - 1; i++)
                {
                    float3 p0 = allPoints[pointIndices[0]];
                    float3 p1 = allPoints[pointIndices[i]];
                    float3 p2 = allPoints[pointIndices[i + 1]];

                    trianglePoints.Add(p0);
                    trianglePoints.Add(p1);
                    trianglePoints.Add(p2);

                    float3 faceNormal = float3.Normalize(float3.Cross(p1 - p0, p2 - p0));
                    triangleNormals.Add(faceNormal);
                    triangleNormals.Add(faceNormal);
                    triangleNormals.Add(faceNormal);
                }
            }
        }

        return new OBJData
        {
            trianglePoints = trianglePoints.ToArray(),
            triangleNormals = triangleNormals.ToArray()
        };
    }

    public class RenderTarget(int w, int h)
    {
        public readonly float3[,] ColourBuffer = new float3[w, h];
        public readonly float[,] DepthBuffer = new float[w, h];
        public readonly float3[,] NormalBuffer = new float3[w, h];
        public readonly int Width = w;
        public readonly int Height = h;
        public readonly float2 Size = new float2(w, h);
    }
    public class Model(float3[] points, float3[] normals, float3[] cols)
    {
        public readonly float3[] OriginalPoints = points;
        public readonly float3[] OriginalNormals = normals;
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
        public float3[] GetTransformedNormals()
        {
            float3[] transformed = new float3[OriginalNormals.Length];
            for (int i = 0; i < OriginalNormals.Length; i++)
            {
                transformed[i] = Transform.ToWorldNormal(OriginalNormals[i]);
            }
            return transformed;
        }
    }
    public class Light(float3 Direction, float3 Color)
    {
        public float3 Direction { get; } = Direction;
        public float3 Color { get; } = Color/255f;
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
        public float3 ToWorldNormal(float3 n)
        {
            (float3 ihat, float3 jhat, float3 khat) = GetBasisVectors();
            return float3.Normalize(TransformVector(ihat, jhat, khat, n));
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
                target.NormalBuffer[x, y] = new float3(0, 0, 0);
            }
        }
    }
    static float3 CalculateLighting(float3 normal, float3 baseColor, Light[] Lights, float ambientStrength = 0.1f)
    {
        float3 result = new float3(0, 0, 0);
        foreach (Light light in Lights)
        {
            float3 lightDir = float3.Normalize(light.Direction);
            float3 lightColor = light.Color;
            float diff = MathF.Max(float3.Dot(normal, lightDir), 0.0f);
            float3 diffuse = lightColor * diff;
            result += (baseColor / 255f) * diffuse;
        }
        result += ambientStrength * baseColor / 255f;
        result.x = MathF.Max(0, MathF.Min(1, result.x));
        result.y = MathF.Max(0, MathF.Min(1, result.y));
        result.z = MathF.Max(0, MathF.Min(1, result.z));
        return result;
    }
    static float3[,] Render(Model model, RenderTarget target, Light[] Lights, int frame, bool visualizeDepth = false, bool enableLighting = true)
    {
        int width = target.Width;
        int height = target.Height;
        float fov = 60f * (float)Math.PI / 180f;
        float3[,] image = target.ColourBuffer;

        float3[] points = model.GetTransformedPoints();
        float3[] normals = model.GetTransformedNormals();
        float3[] triangleCols = model.TriangleCols;

        float nearPlane = 1f;
        float farPlane = 5f;
        float3 lightDir = float3.Normalize(new float3(0.5f, -1f, 0.3f));
        float3 lightColor = new float3(1f, 1f, 1f);

        int triangleCount = points.Length / 3;
        int[] triangleIndices = Enumerable.Range(0, triangleCount).ToArray();

        Parallel.ForEach(triangleIndices, i =>
        {
            int pointIndex = i * 3;
            float3 a = VertexToScreen(points[pointIndex], target.Size, fov);
            float3 b = VertexToScreen(points[pointIndex + 1], target.Size, fov);
            float3 c = VertexToScreen(points[pointIndex + 2], target.Size, fov);
            float minXf = MathF.Min(a.x, MathF.Min(b.x, c.x));
            float maxXf = MathF.Max(a.x, MathF.Max(b.x, c.x));
            float minYf = MathF.Min(a.y, MathF.Min(b.y, c.y));
            float maxYf = MathF.Max(a.y, MathF.Max(b.y, c.y));
            int minX = (int)MathF.Max(0, MathF.Floor(minXf));
            int maxX = (int)MathF.Min(width - 1, MathF.Ceiling(maxXf));
            int minY = (int)MathF.Max(0, MathF.Floor(minYf));
            int maxY = (int)MathF.Min(height - 1, MathF.Ceiling(maxYf));
            float3 col = triangleCols[i];
            float3 faceNormal = normals[pointIndex];
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
                            target.NormalBuffer[x, y] = faceNormal;
                            if (visualizeDepth)
                            {
                                float normalizedDepth = MathF.Max(0, MathF.Min(1, (depth - nearPlane) / (farPlane - nearPlane)));
                                image[x, y] = new float3(normalizedDepth, normalizedDepth, normalizedDepth);
                            }
                            else
                            {
                                float3 finalColor = col;
                                if (enableLighting)
                                {
                                    finalColor = CalculateLighting(faceNormal, col, Lights);
                                }
                                image[x, y] = finalColor;
                            }
                            target.DepthBuffer[x, y] = depth;
                        }
                    }
                }
            }
        });
        return image;
    }
    public static void RenderModel()
    {
        string objPath = Path.Combine(Directory.GetCurrentDirectory(), "models", "suzanne.obj");
        string objString = File.ReadAllText(objPath);
        OBJData objData = LoadOBJFile(objString);

        Random rng = new();
        float3[] triangleCols = new float3[objData.trianglePoints.Length / 3];
        for (int i = 0; i < triangleCols.Length; i++)
        {
            triangleCols[i] = new float3(255, 255, 255);
        }
        Model model = new Model(objData.trianglePoints, objData.triangleNormals, triangleCols);
        model.Transform.Position = new float3(0, 0, 3f);
        float inYaw = (float)Math.PI;
        RenderTarget renderTarget = new RenderTarget(960, 540);
        Light[] Lights = new Light[2]
        {
            new Light(new float3(-0.3f, 0.6f, -0.1f), new float3(255, 206, 166)),
            new Light(new float3(0.3f, 0.6f, -0.1f), new float3(210, 223, 255))
        };
        bool renderDepth = false;
        bool enableLighting = true;
        const int frameCount = 120;
        for (int frame = 0; frame < frameCount; frame++)
        {
            model.Transform.yaw = (float)(frame * Math.PI * 2f / frameCount) + inYaw;
            ClearBuffers(renderTarget);
            Render(model, renderTarget, Lights, frame, renderDepth, enableLighting);
            WriteImagetoFile(renderTarget.ColourBuffer, $"Raster{frame:D2}");
            Console.WriteLine($"Raster image created for frame {frame}");
        }
    }
    public static void Main(string[] args)
    {
        RenderModel();
    }
}