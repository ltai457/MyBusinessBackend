using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
   [ApiController]
   [Route("api/v1/customers")]
   [Produces("application/json")]
   [Authorize] // All endpoints require authentication
   public class CustomersController : ControllerBase
   {
       private readonly ICustomerService _customerService;

       public CustomersController(ICustomerService customerService)
       {
           _customerService = customerService;
       }

       [HttpPost]
       [Authorize(Roles = "Admin,Staff")] // Admin or Staff can create customers
       public async Task<ActionResult<CustomerResponseDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
       {
           if (!ModelState.IsValid) return BadRequest(ModelState);

           var customer = await _customerService.CreateCustomerAsync(dto);
           if (customer == null)
               return BadRequest(new { message = "Failed to create customer." });

           return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
       }

       [HttpGet]
       public async Task<ActionResult<IEnumerable<CustomerListDto>>> GetAllCustomers()
       {
           var customers = await _customerService.GetAllCustomersAsync();
           return Ok(customers);
       }

       [HttpGet("{id:guid}")]
       public async Task<ActionResult<CustomerResponseDto>> GetCustomer(Guid id)
       {
           var customer = await _customerService.GetCustomerByIdAsync(id);
           if (customer == null)
               return NotFound(new { message = $"Customer with ID {id} not found." });

           return Ok(customer);
       }

       [HttpPut("{id:guid}")]
       [Authorize(Roles = "Admin,Staff")] // Admin or Staff can update customers
       public async Task<ActionResult<CustomerResponseDto>> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto dto)
       {
           if (!ModelState.IsValid) return BadRequest(ModelState);

           if (!await _customerService.CustomerExistsAsync(id))
               return NotFound(new { message = $"Customer with ID {id} not found." });

           var updated = await _customerService.UpdateCustomerAsync(id, dto);
           if (updated == null)
               return BadRequest(new { message = "Failed to update customer." });

           return Ok(updated);
       }

       [HttpDelete("{id:guid}")]
       [Authorize(Roles = "Admin")] // Only Admin can delete customers
       public async Task<IActionResult> DeleteCustomer(Guid id)
       {
           if (!await _customerService.CustomerExistsAsync(id))
               return NotFound(new { message = $"Customer with ID {id} not found." });

           var deleted = await _customerService.DeleteCustomerAsync(id);
           if (!deleted)
               return BadRequest(new { message = "Failed to delete customer." });

           return NoContent();
       }

       [HttpGet("{id:guid}/sales")]
       public async Task<ActionResult<IEnumerable<SaleListDto>>> GetCustomerSalesHistory(Guid id)
       {
           if (!await _customerService.CustomerExistsAsync(id))
               return NotFound(new { message = $"Customer with ID {id} not found." });

           var sales = await _customerService.GetCustomerSalesHistoryAsync(id);
           return Ok(sales);
       }
   }
}