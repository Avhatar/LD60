using UnityEngine;

namespace LD60.Signals
{
    public static class ViewBounds
    {
        public static bool IsOutside(Camera camera, Vector2 point, float margin)
        {
            if (camera == null)
                return false;

            Vector2 centre = camera.transform.position;
            float halfHeight = camera.orthographic
                ? camera.orthographicSize
                : Mathf.Abs(camera.transform.position.z) * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            float halfWidth = halfHeight * camera.aspect;

            return Mathf.Abs(point.x - centre.x) > halfWidth + margin
                || Mathf.Abs(point.y - centre.y) > halfHeight + margin;
        }
    }
}
