using System;

class Camera
{
    Vec origin, lowerLeftCorner, horizontal, vertical;

    Vec forward, up, right;
    const float fov = MathF.PI / 4.0f;

    public Camera(float distance, float axisangle, float rotangle, float aspectRatio)
    {
        float cos = MathF.Cos(rotangle);
        float sin = MathF.Sin(rotangle);

        //origin = distance * new Vec(sin * MathF.Sin(axisangle), MathF.Cos(axisangle), -cos * MathF.Sin(axisangle));
        //forward = -origin.Normalized(); //(-cos, -sin)
        //up = forward.Y == 0.0f ? new Vec(0, 0, 1) : new Vec(-sin, (sin * forward.X - cos * forward.Z) / forward.Y, cos).Normalized();
        //right = Vec.CrossProduct(up, forward);

        origin = distance * new Vec(sin * MathF.Sin(axisangle), MathF.Cos(axisangle), cos * MathF.Sin(axisangle));
        forward = -origin.Normalized(); //(-cos, -sin)
        up = forward.Y == 0.0f ? new Vec(0, 0, -1) : new Vec(-sin, (sin * forward.X + cos * forward.Z) / forward.Y, -cos).Normalized();
        right = -Vec.CrossProduct(up, forward);

        float vlength = 1.0f, hlength = aspectRatio * vlength;
        horizontal = hlength * right;
        vertical = vlength * up;

        float tan = MathF.Tan(fov / 2.0f);
        float depth = 0.5f * hlength / tan;
        lowerLeftCorner = origin + depth * forward - 0.5f * horizontal - 0.5f * vertical;
    }

    public (float, float) Projection(Vec point)
    {
        Vec relativePoint = point - origin;
        Vec cameraSpacePoint = new(Vec.DotProduct(relativePoint, right), Vec.DotProduct(relativePoint, up), Vec.DotProduct(relativePoint, forward));

        float aspectRatio = horizontal.Length() / vertical.Length();
        float tan = MathF.Tan(fov / 2.0f);
        float x = cameraSpacePoint.X / (tan * cameraSpacePoint.Z);
        float y = cameraSpacePoint.Y * aspectRatio / (tan * cameraSpacePoint.Z);
        return (0.5f * (x + 1.0f), 0.5f * (y + 1.0f));
    }

    public Ray CameraRay(float u, float v)
    {
        Ray ray = new(origin, lowerLeftCorner + u * horizontal + v * vertical - origin);
        ray.vector.Normalize();
        return ray;
    }
}
