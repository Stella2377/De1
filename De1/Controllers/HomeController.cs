using De1.Data;
using De1.DTOs;
using De1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace De1.Controllers;

// Sử dụng Primary Constructor 
public class HomeController(AppDbContext context) : Controller
{

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> Index()
    {
        // REQ-01 & REQ-04: LINQ Projection [cite: 15, 27]
        var list = await context.Equipments
            .Select(e => new EquipmentVM(e.Id, e.Name, e.Status, e.Category.Name))
            .ToListAsync();
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Borrow(int id)
    {
        var item = await context.Equipments.FindAsync(id);

        // REQ-02: Concurrency Check [cite: 19]
        if (item == null || item.Status == "Borrowed")
        {
            return BadRequest("Thiết bị đã được mượn hoặc không tồn tại!");
        }

        item.Status = "Borrowed";
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var item = await context.Equipments.FindAsync(id);
        if (item != null)
        {
            context.Equipments.Remove(item); // Sẽ được DbContext chuyển thành Soft Delete [cite: 25]
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}