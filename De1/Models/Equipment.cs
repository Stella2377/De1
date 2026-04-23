namespace De1.Models;

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; } // Available/Borrowed
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; }
}