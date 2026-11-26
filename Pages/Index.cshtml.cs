using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private const string CartKey = "Cart";
    private const string ConsentCookie = "CookieConsent";
    private const string LanguageCookie = "Language";

    public List<Product> Products { get; } = new()
    {
        new Product { ProductId = 1, Name = "Datormus", Price = 199, Description = "Trådlös datormus" },
        new Product { ProductId = 2, Name = "Tangentbord", Price = 499, Description = "Mekaniskt tangentbord" },
        new Product { ProductId = 3, Name = "Skärm", Price = 1999, Description = "27 tum Full HD-skärm" }
    };

    public Cart Cart { get; set; } = new();
    public bool HasConsent { get; set; }
    public string Language { get; set; } = "Svenska";
    public bool WasReset { get; set; }

    // ---------- Hjälpmetoder ----------

    private Cart LoadCart()
    {
        string? json = HttpContext.Session.GetString(CartKey);
        if (json != null)
        {
            return JsonSerializer.Deserialize<Cart>(json)!;
        }
        return new Cart { CartId = 1 };
    }

    private void SaveCart(Cart cart)
    {
        string json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CartKey, json);
    }

    private void LoadSettings()
    {
        HasConsent = Request.Cookies[ConsentCookie] != null;
        Language = Request.Cookies[LanguageCookie] ?? "Svenska";
    }

    public string T(string sv, string en) => Language == "English" ? en : sv;

    public string Kr(decimal amount) =>
        amount.ToString("N0", new CultureInfo("sv-SE")) + " kr";

    // Produkterna slås upp på ProductId vid visning, så texten följer valt språk
    // (varukorgen i Session innehåller den svenska texten från när produkten lades till)
    public string ProductName(Product p) => p.ProductId switch
    {
        1 => T("Datormus", "Computer mouse"),
        2 => T("Tangentbord", "Keyboard"),
        3 => T("Skärm", "Monitor"),
        _ => p.Name
    };

    public string ProductDesc(Product p) => p.ProductId switch
    {
        1 => T("Trådlös datormus", "Wireless computer mouse"),
        2 => T("Mekaniskt tangentbord", "Mechanical keyboard"),
        3 => T("27 tum Full HD-skärm", "27-inch Full HD monitor"),
        _ => p.Description
    };

    // ---------- Sidan ----------

    public void OnGet(bool reset = false)
    {
        WasReset = reset;
        Cart = LoadCart();
        LoadSettings();
    }

    // ---------- Varukorg (Session) ----------

    public IActionResult OnPostAdd(int id)
    {
        Product? product = Products.FirstOrDefault(p => p.ProductId == id);
        if (product == null) return RedirectToPage();

        Cart cart = LoadCart();
        CartItem? item = cart.Items.FirstOrDefault(i => i.Product.ProductId == id);

        if (item == null)
        {
            cart.Items.Add(new CartItem
            {
                CartItemId = cart.Items.Count + 1,
                Product = product,
                Quantity = 1
            });
        }
        else
        {
            item.IncreaseQuantity();
        }

        SaveCart(cart);
        return RedirectToPage();
    }

    public IActionResult OnPostDecrease(int id)
    {
        Cart cart = LoadCart();
        CartItem? item = cart.Items.FirstOrDefault(i => i.Product.ProductId == id);

        if (item != null)
        {
            item.DecreaseQuantity();
            if (item.Quantity == 0)
            {
                cart.Items.Remove(item);
            }
        }

        SaveCart(cart);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        Cart cart = LoadCart();
        cart.Clear();
        SaveCart(cart);
        return RedirectToPage();
    }

    // ---------- Inställningar (Cookies) ----------

    public IActionResult OnPostAccept()
    {
        // Samtyckescookien sparas bara om användaren accepterar
        Response.Cookies.Append(ConsentCookie, "true",
            new CookieOptions { Expires = DateTime.Now.AddDays(30) });
        return RedirectToPage();
    }

    public IActionResult OnPostDeny()
    {
        // Ingen cookie sparas
        return RedirectToPage();
    }

    public IActionResult OnPostSetLanguage(string lang)
    {
        // Språk får bara sparas om användaren har samtyckt
        if (Request.Cookies[ConsentCookie] != null && (lang == "Svenska" || lang == "English"))
        {
            Response.Cookies.Append(LanguageCookie, lang,
                new CookieOptions { Expires = DateTime.Now.AddDays(30) });
        }
        return RedirectToPage();
    }

    public IActionResult OnPostReset()
    {
        Response.Cookies.Delete(ConsentCookie);
        Response.Cookies.Delete(LanguageCookie);
        return RedirectToPage(new { reset = true });
    }
}
