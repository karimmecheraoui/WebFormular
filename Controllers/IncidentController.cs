using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Mail;
using WebformularFuerMit.Models;

public class IncidentController : Controller
{
    // ✅ CONFIG LADEN (EINMAL!)
    private readonly AppDbContext _db;

    public IncidentController(AppDbContext db)
    {
        _db = db;
    }
    private Config LoadConfig()
    {
        var json = System.IO.File.ReadAllText(
            Path.Combine(Directory.GetCurrentDirectory(), "config.json")
        );

        return JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    // ✅ FORM LADEN
    public IActionResult Create(string ticket)
    {
        var config = LoadConfig();

        ViewBag.Priorities = config.Priorities;
        ViewBag.DistributionLists = config.DistributionLists;

        var model = new Incident();

        if (!string.IsNullOrEmpty(ticket))
        {
            model.IncidentNumber = ticket;
        }

        return View(model);
    }
    public IActionResult List()
    {
        var incidents = _db.Incidents.ToList();
        return View(incidents);
    }
    public IActionResult Edit(string id)
    {
        var incident = _db.Incidents
            .FirstOrDefault(i => i.IncidentNumber == id);

        if (incident == null)
            return RedirectToAction("List");

        var config = LoadConfig();

        ViewBag.Priorities = config.Priorities;
        ViewBag.DistributionLists = config.DistributionLists;

        return View("Create", incident);
    }
    // ✅ SEND + PREVIEW + SMTP
    [HttpPost]
    public IActionResult Send(Incident model, List<string> selectedLists)
    {
        var config = LoadConfig();

        var recipients = selectedLists != null && selectedLists.Any()
            ? selectedLists
            : new List<string> { "DEINE.EMAIL@FIRMA.COM" };

        var subject = $"INCIDENT {model.IncidentNumber} - {model.Title}";

        // ✅ HEADER aus JSON
        var headerPath = GetHeaderImageFromConfig(model, config);

        // ✅ FOOTER fix
        var footerPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot/Images/Footer/IMG_IT_Signature_Banner.jpg"
        );
        var existing = _db.Incidents
    .FirstOrDefault(i => i.IncidentNumber == model.IncidentNumber);

        if (existing == null)
        {
            _db.Incidents.Add(model);
        }
        else
        {
            existing.Status = model.Status;
            existing.Title = model.Title;
        }

        // ✅ speichern
        _db.SaveChanges();

        // ✅ HTML BODY
        var htmlBody = $@"
<html>
<body style='margin:0;font-family:Arial;'>

<img src='cid:headerImage' style='width:100%;height:auto;' />

<div style='padding:20px;'>

    <h2>Major Incident Update</h2>

    <p><b>Ticket:</b> {model.IncidentNumber}</p>
    <p><b>Priority:</b> {model.Priority}</p>
    <p><b>Status:</b> {model.Status}</p>

    <hr/>

    <p><b>Title:</b></p>
    <p>{model.Title}</p>

    <p><b>Update:</b></p>
    <p><h3>{model.Description}</h3></p>
    <p>{model.UpdateText}</p>

</div>

<img src='cid:footerImage' style='width:100%;height:auto;' />


</body>
</html>";


        // ✅ PREVIEW
        if (Request.Query.ContainsKey("preview"))
        {
            var headerUrl = ConvertToUrl(headerPath);
            var footerUrl = ConvertToUrl(footerPath);

            var previewHtml = $@"
<html>
<body style='font-family:Arial;background:#eef2f7;padding:20px'>

<div style='max-width:800px;margin:auto;background:white;border-radius:8px;overflow:hidden'>

    <img src='{headerUrl}' style='width:100%;height:auto;' />

    <div style='padding:20px'>

        <h2>Major Incident Update</h2>

        <p><b>Ticket:</b> {model.IncidentNumber}</p>
        <p><b>Priority:</b> {model.Priority}</p>
        <p><b>Status:</b> {model.Status}</p>

        <hr/>

        <p>{model.Title}</p>
        <p><h3>{model.Description}</h3></p>
        <p>{model.UpdateText}</p>

    </div>

    <img src='{footerUrl}' style='width:100%;height:auto;' />

</div>

</body>
</html>";

            return Content(previewHtml, "text/html");
        }


        // ✅ SMTP MAIL
        var mail = new MailMessage();

        mail.From = new MailAddress("Karim.Test@Test.com");

        foreach (var email in recipients)
        {
            mail.To.Add(email);
        }

        mail.Subject = subject;
        mail.IsBodyHtml = true;

        var alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

        // ✅ HEADER IMAGE (SAFE)
        if (System.IO.File.Exists(headerPath))
        {
            var headerResource = new LinkedResource(headerPath);
            headerResource.ContentId = "headerImage";
            alternateView.LinkedResources.Add(headerResource);
        }

        // ✅ FOOTER IMAGE (SAFE)
        if (System.IO.File.Exists(footerPath))
        {
            var footerResource = new LinkedResource(footerPath);
            footerResource.ContentId = "footerImage";
            alternateView.LinkedResources.Add(footerResource);
        }

        mail.AlternateViews.Add(alternateView);


        // ✅ SMTP (DEIN FUNKTIONIERENDER!)
        var client = new SmtpClient("mail.rwe.com")
        {
            Port = 25,
            EnableSsl = false
        };

        client.Send(mail);

        return RedirectToAction("Create");
    }


    // ✅ HEADER AUS JSON HOLEN
    private string GetHeaderImageFromConfig(Incident model, Config config)
    {
        if (config.headers == null || config.headers.Count == 0)
            return GetDefaultHeader();

        var match = config.headers.FirstOrDefault(h =>
            h.Priority.Equals(model.Priority, StringComparison.OrdinalIgnoreCase) &&
            h.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase)
        );

        if (match != null)
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                match.Path.TrimStart('/')
            );
        }

        return GetDefaultHeader();
    }


    // ✅ DEFAULT HEADER (wichtig gegen Fehler)
    private string GetDefaultHeader()
    {
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot/Images/Header/rwe_header-v2_2_major_problem_report.png"
        );
    }


    // ✅ PFAD → URL (für Preview)
    private string ConvertToUrl(string fullPath)
    {
        return fullPath
            .Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "")
            .Replace("\\", "/");
    }
}