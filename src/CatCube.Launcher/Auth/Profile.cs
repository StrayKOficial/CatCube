using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models; // Sometimes nested under Supabase namespace in older/bundle versions

namespace CatCube.Launcher.Auth;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("body_type")]
    public int BodyType { get; set; }

    [Column("hair_style")]
    public int HairStyle { get; set; }

    [Column("shirt_color")]
    public string ShirtColor { get; set; } = "#CC3333";

    [Column("pants_color")]
    public string PantsColor { get; set; } = "#264073";

    [Column("skin_color")]
    public string SkinColor { get; set; } = "#FFD9B8";
}
