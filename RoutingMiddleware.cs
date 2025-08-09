using System.Text;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _pagesPath;
    private readonly string _imagesPath;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
        _pagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Pages");
        _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

        if (!Directory.Exists(_imagesPath))
            Directory.CreateDirectory(_imagesPath);
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "/";
        var method = context.Request.Method;

        if (path == "/")
        {
            string htmlPath = Path.Combine(_pagesPath, "index.html");
            var html = File.ReadAllText(htmlPath);

            var rows = string.Join("", ProductRepository.GetAll()
                .Select(p => $"<tr>" +
                             $"<td>{p.Id}</td>" +
                             $"<td>{p.Name}</td>" +
                             $"<td>{p.Price} грн.</td>" +
                             $"<td><img src='{p.ImagePath}' class='thumb'></td>" +
                             $"<td>" +
                             $"<a class='btn btn-small' href='/product/{p.Id}'>Детали</a> " +
                             $"<a class='btn btn-small' href='/edit/{p.Id}'>Изменить</a> " +
                             $"<a class='btn btn-small btn-danger' href='/delete/{p.Id}' onclick='return confirm(\"Удалить?\")'>Удалить</a>" +
                             $"</td>" +
                             $"</tr>"));

            html = html.Replace("{{PRODUCT_ROWS}}", rows);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html, Encoding.UTF8);
        }
        else if (path == "/add" && method == "GET")
        {
            var html = File.ReadAllText(Path.Combine(_pagesPath, "add.html"));
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html, Encoding.UTF8);
        }
        else if (path == "/add" && method == "POST")
        {
            var form = await context.Request.ReadFormAsync();
            string name = form["name"];
            decimal price = decimal.TryParse(form["price"], out var p) ? p : 0;

            if (string.IsNullOrWhiteSpace(name) || price <= 0)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Ошибка: заполните корректно все поля!");
                return;
            }

            var file = form.Files["photo"];
            string imagePath = "/images/no-image.png"; // заглушка

            if (file != null && file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png" };

                if (!allowed.Contains(ext) || file.Length > 2 * 1024 * 1024)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Ошибка: допустимы JPG/PNG и размер ≤ 2 МБ");
                    return;
                }

                string fileName = Guid.NewGuid().ToString() + ext;
                string savePath = Path.Combine(_imagesPath, fileName);

                using (var stream = File.Create(savePath))
                {
                    await file.CopyToAsync(stream);
                }

                imagePath = "/images/" + fileName;
            }

            ProductRepository.Add(name, price, imagePath);
            context.Response.Redirect("/");
        }
        else if (path.StartsWith("/product/") && method == "GET")
        {
            if (int.TryParse(path.Replace("/product/", ""), out int id))
            {
                var product = ProductRepository.GetById(id);
                if (product == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Товар не найден");
                    return;
                }

                string html = $"<h1>Товар #{product.Id}</h1>" +
                              $"<p><b>Название:</b> {product.Name}</p>" +
                              $"<p><b>Цена:</b> {product.Price} руб.</p>" +
                              $"<img src='{product.ImagePath}' style='width:200px;border-radius:6px;'><br>" +
                              $"<a href='/'>Назад</a>";
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(html, Encoding.UTF8);
            }
        }
        else if (path.StartsWith("/edit/") && method == "GET")
        {
            if (int.TryParse(path.Replace("/edit/", ""), out int id))
            {
                var product = ProductRepository.GetById(id);
                if (product == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Товар не найден");
                    return;
                }

                var html = File.ReadAllText(Path.Combine(_pagesPath, "edit.html"))
                    .Replace("{{ID}}", product.Id.ToString())
                    .Replace("{{NAME}}", product.Name)
                    .Replace("{{PRICE}}", product.Price.ToString());
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(html, Encoding.UTF8);
            }
        }
        else if (path.StartsWith("/edit/") && method == "POST")
        {
            if (int.TryParse(path.Replace("/edit/", ""), out int id))
            {
                var form = await context.Request.ReadFormAsync();
                string name = form["name"];
                decimal price = decimal.TryParse(form["price"], out var p) ? p : 0;

                if (string.IsNullOrWhiteSpace(name) || price <= 0)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Ошибка валидации");
                    return;
                }

                string imagePath = "";
                var file = form.Files["photo"];
                if (file != null && file.Length > 0)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png" };
                    if (allowed.Contains(ext) && file.Length <= 2 * 1024 * 1024)
                    {
                        string fileName = Guid.NewGuid() + ext;
                        string savePath = Path.Combine(_imagesPath, fileName);
                        using var stream = File.Create(savePath);
                        await file.CopyToAsync(stream);
                        imagePath = "/images/" + fileName;
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Ошибка: допустимы JPG/PNG и размер ≤ 2 МБ");
                        return;
                    }
                }

                ProductRepository.Update(id, name, price, imagePath);
                context.Response.Redirect("/");
            }
        }
        else if (path.StartsWith("/delete/") && method == "GET")
        {
            if (int.TryParse(path.Replace("/delete/", ""), out int id))
            {
                ProductRepository.Delete(id);
                context.Response.Redirect("/");
            }
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Страница не найдена");
        }
    }
}