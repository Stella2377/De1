namespace De1.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Quan hệ 1:N
    public virtual ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
}