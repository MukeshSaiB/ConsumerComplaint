using ConsumerComplaint.Data;
using ConsumerComplaint.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ConsumerComplaint.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly static string Api = "https://data.cdc.gov/resource/i2ek-k3pa.json";

        private ApplicationDbContext context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            this.context = context;
        }

        

        public async Task<IActionResult> LoadDataFromApi()
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                context.CompanyData.RemoveRange(context.CompanyData);
                context.ProductData.RemoveRange(context.ProductData);
                context.ComplaintData.RemoveRange(context.ComplaintData);
                await context.SaveChangesAsync();

                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.GetAsync(Api);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "API request error");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var ComplaintDataDtos = await client.GetFromJsonAsync<ComplaintApiResponse>("https://www.consumerfinance.gov/data-research/consumer-complaints/search/api/v1/");
                System.Diagnostics.Debug.WriteLine("Load Consumer Complaint Data");
                System.Diagnostics.Debug.WriteLine(ComplaintDataDtos);

                if (ComplaintDataDtos.Hits.ComplaintRecords == null )
                {
                    TempData["ErrorMessage"] = "No Consumer Complaint data found in the API response.";
                    return RedirectToAction("Index");
                }

                foreach(var dto in ComplaintDataDtos.Hits.ComplaintRecords)
                {
                    if (string.IsNullOrWhiteSpace(dto.Source.CompanyName))
                    {
                        dto.Source.CompanyName = "Unknown";
                    }
                    var company = context.CompanyData.FirstOrDefault(s => s.CompanyName == dto.Source.CompanyName);
                    if (company == null)
                    {
                        company = new Company { CompanyName = dto.Source.CompanyName };
                        context.CompanyData.Add(company);
                        context.SaveChanges();
                    }
                    if (string.IsNullOrWhiteSpace(dto.Source.ProductName))
                    {
                        dto.Source.ProductName = "Unknown"; // Default value for CityName if it's null or empty
                    }
                    var product = context.ProductData.FirstOrDefault(c => c.ProductName == dto.Source.ProductName && c.CompanyID == company.CompanyID)
                               ?? context.ProductData.Add(new Product { ProductName = dto.Source.ProductName, CompanyID = company.CompanyID }).Entity;
                    context.SaveChanges();

                    var ComplaintData = context.ComplaintData.FirstOrDefault(h => h.ComplaintID == dto.Source.complaint_id)
                                     ?? new Complaint();

                    if (string.IsNullOrWhiteSpace(dto.Source.Issue))
                    {
                        dto.Source.Issue = "Unknown"; // Default value for CityName if it's null or empty
                    }
                    // Map the DTO to your model properties
                    
                    ComplaintData.ProductID = product.ProductID;
                    ComplaintData.IssueDescription = dto.Source.Issue;
                    ComplaintData.DateReceived = dto.Source.DateReceived;
                    
                    context.ComplaintData.Add(ComplaintData);
                    context.SaveChanges();

                }
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Consumer Complaint data Loaded successfully.";
            }
            catch (Exception ex) {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred while loading data from the API.");
                TempData["ErrorMessage"] = "An error occurred while processing the data.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Search(){
            var complaintViewModelList = await context.ComplaintData
                                           .Include(c => c.Product)
                                           .ThenInclude(p => p.Company)
                                           .Select(c => new ComplaintViewModel
                                           {
                                               ComplaintID = c.ComplaintID, // Ensure you select the ComplaintID
                                               DateReceived = c.DateReceived,
                                               ProductName = c.Product.ProductName,
                                               IssueDescription = c.IssueDescription,
                                               CompanyName = c.Product.Company.CompanyName
                                           })
                                            .ToListAsync();
            return View(complaintViewModelList); 
        }

        public async Task<IActionResult> SearchCompany(string companyInput) {
            System.Diagnostics.Debug.WriteLine(companyInput);
            var complaints = await context.ComplaintData
                            .Include(c => c.Product)
                            .ThenInclude(p => p.Company)
                            .Where(c => c.IssueDescription.Contains(companyInput)) // Adjusted the Where clause to filter by company name
                            .Select(c => new ComplaintViewModel
                            {
                                ComplaintID = c.ComplaintID,
                                DateReceived = c.DateReceived,
                                ProductName = c.Product.ProductName,
                                IssueDescription = c.IssueDescription,
                                CompanyName = c.Product.Company.CompanyName
                            })
                            .ToListAsync();

            // Pass the search results to the view
            return View(complaints);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            Complaint p = context.ComplaintData.Find(id);
            return View("SearchCompanySingle", p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Complaint p)
        {
            if (p == null || p.ComplaintID == 0) return NotFound();
            context.ComplaintData.Update(p);
            context.SaveChanges();
            return RedirectToAction("Search"); ;
        }

        public ActionResult Delete(int? id)
        {
            System.Diagnostics.Debug.WriteLine("Complaint Data Delete");
            System.Diagnostics.Debug.WriteLine(id);
            Complaint data = context.ComplaintData.Find(id);
            context.ComplaintData.Remove(data);
            context.SaveChanges();
            return RedirectToAction("Search");
        }

        public ActionResult Add()
        {
            // Create the ViewModel
            var viewModel = new ComplaintAddViewModel
            {
                ProductList = new SelectList(context.ProductData.Select(p => new
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName
                }).ToList(), "ProductID", "ProductName"),
                CompanyList = new SelectList(context.CompanyData.Select(c => new
                {
                    CompanyID = c.CompanyID,
                    CompanyName = c.CompanyName
                }).ToList(), "CompanyID", "CompanyName")
            };

            // Pass the ViewModel to the View
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(ComplaintAddViewModel viewModel)
        {
            Complaint newComplaint = new Complaint
            {
                ProductID = viewModel.ProductID,
                IssueDescription = viewModel.IssueDescription,
                DateReceived = viewModel.DateReceived
            };

            context.ComplaintData.Add(newComplaint);
            context.SaveChanges();
            TempData["SuccessMessage"] = "Consumer Complaint Record Inserted successfully.";
            return RedirectToAction("Search");
        }

        public IActionResult Graph()
        {
            var complaintsByProduct = context.ComplaintData
                                     .Include(c => c.Product)
                                     .GroupBy(c => c.Product.ProductName)
                                     .Select(group => new
                                     {
                                         ProductName = group.Key,
                                         Count = group.Count()
                                     }).ToList();

            // Group and count complaints by Company Name
            var complaintsByCompany = context.ComplaintData
                                    .Include(c => c.Product)
                                    .ThenInclude(p => p.Company)
                                    .GroupBy(c => c.Product.Company.CompanyName)
                                    .Select(group => new
                                    {
                                        CompanyName = group.Key,
                                        Count = group.Count()
                                    }).ToList();

            ViewBag.ComplaintsByProductJson = JsonSerializer.Serialize(complaintsByProduct.Select(c => new { c.ProductName, c.Count }));
            ViewBag.ComplaintsByCompanyJson = JsonSerializer.Serialize(complaintsByCompany.Select(c => new { c.CompanyName, c.Count }));

            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}