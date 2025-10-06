using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.Common.Models;
using OilTrading.Api.Common.Utilities;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractMatchingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContractMatchingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("available-purchases")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailablePurchases()
        {
            try
            {
                var purchases = await _context.PurchaseContracts
                    .Include(p => p.Product)
                    .Include(p => p.TradingPartner)
                    .Where(p => p.MatchedQuantity < p.ContractQuantity.Value)
                    .Select(p => new
                    {
                        p.Id,
                        ContractNumber = p.ContractNumber.Value,
                        ContractQuantity = p.ContractQuantity.Value,
                        p.MatchedQuantity,
                        AvailableQuantity = p.ContractQuantity.Value - p.MatchedQuantity,
                        ProductName = p.Product.Name,
                        TradingPartnerName = p.TradingPartner.Name
                    })
                    .ToListAsync();

                return Ok(purchases);
            }
            catch (Exception ex)
            {
                return this.CreateErrorResponse(
                    ErrorCodes.DatabaseError,
                    "Failed to retrieve available purchase contracts",
                    StatusCodes.Status500InternalServerError,
                    ex.Message
                );
            }
        }

        [HttpGet("unmatched-sales")]
        public async Task<ActionResult<IEnumerable<object>>> GetUnmatchedSales()
        {
            var sales = await _context.SalesContracts
                .Include(s => s.Product)
                .Include(s => s.TradingPartner)
                .Where(s => !_context.ContractMatchings.Any(cm => cm.SalesContractId == s.Id))
                .Select(s => new
                {
                    s.Id,
                    ContractNumber = s.ContractNumber.Value,
                    ContractQuantity = s.ContractQuantity.Value,
                    ProductName = s.Product.Name,
                    TradingPartnerName = s.TradingPartner.Name
                })
                .ToListAsync();

            return Ok(sales);
        }

        [HttpPost("match")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(StandardErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(StandardErrorResponse), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(StandardErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> CreateMatch([FromBody] CreateMatchingRequest request)
        {
            if (request == null)
            {
                return this.CreateValidationErrorResponse(new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "Request body is required" }
                });
            }

            try
            {
                // Validate the request
                var purchaseContract = await _context.PurchaseContracts.FindAsync(request.PurchaseContractId);
                if (purchaseContract == null)
                {
                    return this.CreateNotFoundResponse("PurchaseContract", request.PurchaseContractId);
                }

                var salesContract = await _context.SalesContracts.FindAsync(request.SalesContractId);
                if (salesContract == null)
                {
                    return this.CreateNotFoundResponse("SalesContract", request.SalesContractId);
                }

                if (purchaseContract.ProductId != salesContract.ProductId)
                {
                    return this.CreateBusinessRuleErrorResponse(
                        ErrorCodes.InvalidBusinessOperation,
                        "Contracts must be for the same product to be matched",
                        new { 
                            PurchaseProductId = purchaseContract.ProductId,
                            SalesProductId = salesContract.ProductId 
                        }
                    );
                }

                var availableQuantity = purchaseContract.ContractQuantity.Value - purchaseContract.MatchedQuantity;
                if (request.Quantity > availableQuantity)
                {
                    return this.CreateBusinessRuleErrorResponse(
                        ErrorCodes.InsufficientQuantity,
                        "Requested quantity exceeds available quantity for matching",
                        new { 
                            RequestedQuantity = request.Quantity,
                            AvailableQuantity = availableQuantity,
                            ContractQuantity = purchaseContract.ContractQuantity.Value,
                            AlreadyMatched = purchaseContract.MatchedQuantity
                        }
                    );
                }

                if (request.Quantity <= 0)
                {
                    return this.CreateValidationErrorResponse(new Dictionary<string, string[]>
                    {
                        [nameof(request.Quantity)] = new[] { "Quantity must be greater than zero" }
                    });
                }

                // Create the matching
                var matching = new ContractMatching(
                    purchaseContractId: request.PurchaseContractId,
                    salesContractId: request.SalesContractId,
                    matchedQuantity: request.Quantity,
                    matchedBy: request.MatchedBy ?? "System",
                    notes: request.Notes);

                _context.ContractMatchings.Add(matching);

                // Update purchase contract matched quantity
                purchaseContract.UpdateMatchedQuantity(purchaseContract.MatchedQuantity + request.Quantity);
                _context.PurchaseContracts.Update(purchaseContract);

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Contract matching created successfully",
                    matchingId = matching.Id,
                    matchedQuantity = request.Quantity,
                    remainingAvailable = availableQuantity - request.Quantity
                });
            }
            catch (BusinessRuleException ex)
            {
                return this.CreateBusinessRuleErrorResponse(ex.ErrorCode, ex.Message, ex.Details);
            }
            catch (ValidationException ex)
            {
                return this.CreateValidationErrorResponse(ex.Errors, ex.Message);
            }
            catch (Exception ex)
            {
                return this.CreateErrorResponse(
                    ErrorCodes.InternalServerError,
                    "An error occurred while creating the contract matching",
                    StatusCodes.Status500InternalServerError,
                    ex.Message
                );
            }
        }

        [HttpGet("purchase/{purchaseId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPurchaseMatchings(Guid purchaseId)
        {
            var matchings = await _context.ContractMatchings
                .Include(cm => cm.SalesContract)
                .ThenInclude(sc => sc.TradingPartner)
                .Where(cm => cm.PurchaseContractId == purchaseId)
                .Select(cm => new
                {
                    cm.Id,
                    cm.MatchedQuantity,
                    cm.MatchedDate,
                    cm.Notes,
                    SalesContractNumber = cm.SalesContract.ContractNumber.Value,
                    SalesContractQuantity = cm.SalesContract.ContractQuantity.Value,
                    SalesTradingPartner = cm.SalesContract.TradingPartner.Name
                })
                .ToListAsync();

            return Ok(matchings);
        }

        [HttpGet("enhanced-net-position")]
        public async Task<ActionResult<object>> GetEnhancedNetPosition()
        {
            // Get all products with their purchase and sales data
            var positions = await _context.Products
                .Select(p => new
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductType = p.Type,
                    TotalPurchased = _context.PurchaseContracts
                        .Where(pc => pc.ProductId == p.Id)
                        .Sum(pc => (decimal?)pc.ContractQuantity.Value) ?? 0,
                    TotalSold = _context.SalesContracts
                        .Where(sc => sc.ProductId == p.Id)
                        .Sum(sc => (decimal?)sc.ContractQuantity.Value) ?? 0,
                    TotalMatched = _context.ContractMatchings
                        .Where(cm => cm.PurchaseContract.ProductId == p.Id)
                        .Sum(cm => (decimal?)cm.MatchedQuantity) ?? 0
                })
                .Where(p => p.TotalPurchased > 0 || p.TotalSold > 0)
                .ToListAsync();

            var result = positions.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.ProductType,
                p.TotalPurchased,
                p.TotalSold,
                p.TotalMatched,
                NetPosition = p.TotalPurchased - p.TotalSold,
                NaturalHedge = p.TotalMatched,
                NetExposure = (p.TotalPurchased - p.TotalSold) - p.TotalMatched,
                HedgeRatio = p.TotalPurchased > 0 ? (p.TotalMatched / p.TotalPurchased) * 100 : 0
            }).ToList();

            return Ok(result);
        }

        [HttpPost("bulk-insert-test-data")]
        public async Task<ActionResult> BulkInsertTestData()
        {
            try
            {
                // Insert test users
                var userDataList = new[]
                {
                    new { Name = "Zhang Wei", Email = "zhang.wei@trader.com", Role = UserRole.Trader },
                    new { Name = "Li Ming", Email = "li.ming@trader.com", Role = UserRole.Trader },
                    new { Name = "Wang Fang", Email = "wang.fang@manager.com", Role = UserRole.Administrator }
                };

                foreach (var userData in userDataList)
                {
                    if (!await _context.Users.AnyAsync(u => u.Email == userData.Email))
                    {
                        var user = new User
                        {
                            Name = userData.Name,
                            Email = userData.Email,
                            Role = userData.Role,
                            PasswordHash = "dummy_hash_for_test",
                            IsActive = true
                        };
                        _context.Users.Add(user);
                    }
                }

                // Insert test products
                var productDataList = new[]
                {
                    new { Name = "Brent Crude", Type = "CrudeOil", ProductType = ProductType.CrudeOil, Description = "Brent Crude Oil Futures", Code = "BRENT" },
                    new { Name = "Fuel Oil", Type = "RefinedProducts", ProductType = ProductType.RefinedProducts, Description = "High Sulfur Fuel Oil", Code = "FO380" }
                };

                foreach (var productData in productDataList)
                {
                    if (!await _context.Products.AnyAsync(p => p.Code == productData.Code))
                    {
                        var product = new Product
                        {
                            Name = productData.Name,
                            Code = productData.Code,
                            Type = productData.ProductType,
                            ProductType = productData.ProductType,
                            Description = productData.Description,
                            IsActive = true
                        };
                        _context.Products.Add(product);
                    }
                }

                // Insert test trading partners
                var partnerDataList = new[]
                {
                    new { Name = "Shell Trading International", Type = TradingPartnerType.Supplier, ContactInfo = "trading@shell.com", Code = "SHELL" },
                    new { Name = "BP Oil Marketing", Type = TradingPartnerType.Customer, ContactInfo = "marketing@bp.com", Code = "BP" },
                    new { Name = "ExxonMobil Supply", Type = TradingPartnerType.Supplier, ContactInfo = "supply@exxonmobil.com", Code = "EXXON" },
                    new { Name = "Total Energies Trading", Type = TradingPartnerType.Customer, ContactInfo = "trading@totalenergies.com", Code = "TOTAL" },
                    new { Name = "Saudi Aramco Trading", Type = TradingPartnerType.Supplier, ContactInfo = "trading@aramco.com", Code = "ARAMCO" },
                    new { Name = "Sinopec Fuel Oil", Type = TradingPartnerType.Customer, ContactInfo = "fueloil@sinopec.com", Code = "SINOPEC" }
                };

                foreach (var partnerData in partnerDataList)
                {
                    if (!await _context.TradingPartners.AnyAsync(tp => tp.Code == partnerData.Code))
                    {
                        var partner = new TradingPartner
                        {
                            Name = partnerData.Name,
                            Code = partnerData.Code,
                            Type = partnerData.Type,
                            ContactInfo = partnerData.ContactInfo,
                            IsActive = true
                        };
                        _context.TradingPartners.Add(partner);
                    }
                }

                await _context.SaveChangesAsync();

                // Get saved entities to use their IDs
                var traders = await _context.Users.Where(u => u.Role == UserRole.Trader).ToListAsync();
                var products = await _context.Products.ToListAsync();
                var partners = await _context.TradingPartners.ToListAsync();

                if (traders.Any() && products.Any() && partners.Any())
                {
                    // Create test purchase contracts using proper constructors
                    var purchaseContractsData = new[]
                    {
                        new { ContractNumber = "TEST-PO-2025-001", PartnerCode = "SHELL", ProductCode = "BRENT", TraderEmail = "zhang.wei@trader.com", Quantity = 50000m },
                        new { ContractNumber = "TEST-PO-2025-002", PartnerCode = "ARAMCO", ProductCode = "BRENT", TraderEmail = "zhang.wei@trader.com", Quantity = 30000m },
                        new { ContractNumber = "TEST-PO-2025-003", PartnerCode = "EXXON", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 80000m },
                        new { ContractNumber = "TEST-PO-2025-004", PartnerCode = "SHELL", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 60000m }
                    };

                    foreach (var data in purchaseContractsData)
                    {
                        var trader = traders.FirstOrDefault(t => t.Email == data.TraderEmail);
                        var product = products.FirstOrDefault(p => p.Code == data.ProductCode);
                        var partner = partners.FirstOrDefault(p => p.Code == data.PartnerCode);

                        if (trader != null && product != null && partner != null && 
                            !await _context.PurchaseContracts.AnyAsync(pc => pc.ContractNumber.Value == data.ContractNumber))
                        {
                            var purchase = new PurchaseContract(
                                contractNumber: ContractNumber.Parse(data.ContractNumber),
                                contractType: ContractType.CARGO,
                                tradingPartnerId: partner.Id,
                                productId: product.Id,
                                traderId: trader.Id,
                                contractQuantity: Quantity.MetricTons(data.Quantity));
                            
                            _context.PurchaseContracts.Add(purchase);
                        }
                    }
                }

                // Create test sales contracts using proper constructors  
                if (traders.Any() && products.Any() && partners.Any())
                {
                    var salesContractsData = new[]
                    {
                        new { ContractNumber = "TEST-SO-2025-001", PartnerCode = "BP", ProductCode = "BRENT", TraderEmail = "zhang.wei@trader.com", Quantity = 15000m },
                        new { ContractNumber = "TEST-SO-2025-002", PartnerCode = "TOTAL", ProductCode = "BRENT", TraderEmail = "zhang.wei@trader.com", Quantity = 20000m },
                        new { ContractNumber = "TEST-SO-2025-003", PartnerCode = "SINOPEC", ProductCode = "BRENT", TraderEmail = "zhang.wei@trader.com", Quantity = 12000m },
                        new { ContractNumber = "TEST-SO-2025-004", PartnerCode = "BP", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 25000m },
                        new { ContractNumber = "TEST-SO-2025-005", PartnerCode = "TOTAL", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 30000m },
                        new { ContractNumber = "TEST-SO-2025-006", PartnerCode = "SINOPEC", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 18000m },
                        new { ContractNumber = "TEST-SO-2025-007", PartnerCode = "BP", ProductCode = "FO380", TraderEmail = "li.ming@trader.com", Quantity = 15000m }
                    };

                    foreach (var data in salesContractsData)
                    {
                        var trader = traders.FirstOrDefault(t => t.Email == data.TraderEmail);
                        var product = products.FirstOrDefault(p => p.Code == data.ProductCode);
                        var partner = partners.FirstOrDefault(p => p.Code == data.PartnerCode);

                        if (trader != null && product != null && partner != null &&
                            !await _context.SalesContracts.AnyAsync(sc => sc.ContractNumber.Value == data.ContractNumber))
                        {
                            var sale = new SalesContract(
                                contractNumber: ContractNumber.Parse(data.ContractNumber),
                                contractType: ContractType.CARGO,
                                tradingPartnerId: partner.Id,
                                productId: product.Id,
                                traderId: trader.Id,
                                contractQuantity: Quantity.MetricTons(data.Quantity));
                            
                            _context.SalesContracts.Add(sale);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Test data inserted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}