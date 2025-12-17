using System.Linq;
using System.Text.Json;

class Relation
{
    public string name, arity;
    public int[] args;
    public float loss;
    public Zec force;
    public bool active;

    public Relation(JsonElement xelem)
    {
        name = xelem.GetProperty("name").GetString();
        arity = xelem.GetProperty("arity").GetString();
        loss = xelem.GetProperty("loss").GetSingle();

        if (xelem.TryGetProperty("force", out JsonElement xforce)) force = new Zec(xforce.EnumerateArray().Select(x => x.GetInt32()).ToArray());

        if (xelem.TryGetProperty("active", out JsonElement xactive)) active = xactive.GetBoolean();
        else active = true;

        args = xelem.GetProperty("args").EnumerateArray().Select(x => x.GetInt32()).ToArray();
    }
}
