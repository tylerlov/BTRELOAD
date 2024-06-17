using UnityEngine;
using Unity.Mathematics;

namespace ProjectDawn.Impostor
{
    /// <summary>
    /// Single capture angle.
    /// </summary>
    public struct CapturePoint
    {
        /// <summary>
        /// Start position of ray.
        /// </summary>
        public float3 From;
        /// <summary>
        /// End position of ray.
        /// </summary>
        public float3 To;
        /// <summary>
        /// Viewport coordinates in texture atlas.
        /// </summary>
        public Rect Uv;
    }

    public class CapturePoints
    {
        /// <summary>
        /// Capture angles.
        /// </summary>
        public CapturePoint[] Points;
        /// <summary>
        /// Square root of capture angles.
        /// </summary>
        public int Frames;

        public BoundingSphere Bounds;

        public float Radius => Bounds.radius;

        public static CapturePoints HemiOctahedral(float3 center, float radius, int frames)
        {
            var result = new CapturePoints();
            result.Points = CreatePoints(center, radius,frames, true);
            result.Frames = frames;
            result.Bounds = new BoundingSphere(center, radius);
            return result;
        }

        static CapturePoint[] CreatePoints(float3 center, float radius, int frames, bool isHemi = true)
        {
            var points = new CapturePoint[frames * frames];

            float framesMinusOne = frames - 1;

            var i = 0;
            for (var y = 0; y < frames; y++)
            {
                for (var x = 0; x < frames; x++)
                {
                    float2 vec;
                    float3 ray;
                    if (isHemi)
                    {
                        vec = new float2(
                            (x / framesMinusOne) * 2f - 1f,
                            (y / framesMinusOne) * 2f - 1f
                        );
                        ray = UVToDirectionHemi(vec);
                    }
                    else
                    {
                        vec = new float2(
                            (0.5f / frames + x / (float)frames) * 2f - 1f,
                            (0.5f / frames + y / (float)frames) * 2f - 1f
                        );
                        ray = UVToDirection(vec);
                    }

                    ray = math.normalize(ray);

                    points[i].From = center + ray * radius;
                    points[i].To = center;
                    points[i].Uv = new Rect((float)x / frames, (float)y / frames, 1f / frames, 1f / frames);
                    i++;
                }
            }

            return points;
        }

        static float3 UVToDirectionHemi(float2 uv)
        {
            // TODO Clean this up
            uv = new float2(uv.x + uv.y, uv.x - uv.y) * 0.5f;
            var vec = new float3(
                uv.x,
                1.0f - math.dot(1,
                    new float2(math.abs(uv.x), math.abs(uv.y))
                    ),
                uv.y
            );
            return math.normalize(vec);
        }

        static float3 UVToDirection(float2 uv)
        {
            // TODO Clean this up
            var n = new float3(uv.x, 1f - math.abs(uv.x) - math.abs(uv.y), uv.y);
            var t = math.saturate(-n.y);
            n.x += n.x >= 0f ? -t : t;
            n.z += n.z >= 0f ? -t : t;
            return n;
        }
    }
}