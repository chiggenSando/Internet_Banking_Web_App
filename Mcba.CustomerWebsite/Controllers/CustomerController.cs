using Mcba.Data;
using Mcba.Data.Models;
using Mcba.CustomerWebsite.Filters;
using Microsoft.AspNetCore.Mvc;


namespace Mcba.CustomerWebsite.Controllers;

[AuthorizeCustomer]
public class CustomerController : Controller
{
    private readonly McbaContext _context;

    private int CustomerID => HttpContext.Session.GetInt32(nameof(Customer.CustomerID)).Value;

    public CustomerController(McbaContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Customer.Accounts is lazy loaded when the view uses it
        var customer = await _context.Customers.FindAsync(CustomerID);

        return View(customer);
    }
}
