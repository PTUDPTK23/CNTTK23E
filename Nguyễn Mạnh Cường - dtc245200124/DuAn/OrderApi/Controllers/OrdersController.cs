using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Infrastructure;
using Shared.Contracts;

namespace OrderApi.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrdersDbContext db, RabbitMqPublisher publisher) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest("CustomerName is required.");
        if (request.Products.Count == 0 || request.Products.Any(p => string.IsNullOrWhiteSpace(p.Name) || p.Quantity <= 0))
            return BadRequest("Products must contain a name and a positive quantity.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName.Trim(),
            Products = request.Products,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        publisher.Publish(new ProcessOrderMessage(order.Id));

        return Created($"/orders/{order.Id}", order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Order>>> List()
        => await db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> Get(Guid id)
        => await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) is { } order
            ? Ok(order)
            : NotFound();
}
