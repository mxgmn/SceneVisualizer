struct Ray
{
    public Vec origin, vector;

    public Ray(Vec origin, Vec vector)
    {
        this.origin = origin;
        this.vector = vector;
    }

    public readonly Vec Point(float t) => origin + t * vector;
}

class Hit
{
    public Ray ray;
    public float t;
    public Vec normal, color;
    //public int cuboidIndex;

    public Hit(Ray ray, float t, Vec normal, Vec color)//, int cuboidIndex)
    {
        this.ray = ray;
        this.t = t;
        this.normal = normal;
        this.color = color;
        //this.cuboidIndex = cuboidIndex;
    }

    public Vec GetPoint() => ray.Point(t);
}
