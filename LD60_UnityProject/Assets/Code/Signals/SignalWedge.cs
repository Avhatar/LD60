using UnityEngine;

namespace LD60.Signals
{
    public static class SignalWedge
    {
        public static void BuildFilledWedge(LineRenderer line, Vector2 origin, Vector2 direction,
            float halfAngleDegrees, float radius, int arcSegments)
        {
            int segments = Mathf.Max(2, arcSegments);
            line.loop = true;
            line.positionCount = segments + 1;
            line.SetPosition(0, origin);

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < segments; i++)
            {
                float t = segments == 1 ? 0.5f : i / (float)(segments - 1);
                float angle = (baseAngle - halfAngleDegrees + 2f * halfAngleDegrees * t) * Mathf.Deg2Rad;
                line.SetPosition(i + 1, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        public static void BuildFrontArc(LineRenderer line, Vector2 origin, Vector2 direction,
            float halfAngleDegrees, float radius, int arcSegments)
        {
            int segments = Mathf.Max(2, arcSegments);
            line.loop = false;
            line.positionCount = segments;

            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float angle = (baseAngle - halfAngleDegrees + 2f * halfAngleDegrees * t) * Mathf.Deg2Rad;
                line.SetPosition(i, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }
}
