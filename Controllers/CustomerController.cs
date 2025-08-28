// Controllers/CustomersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/customers")]
    [Produces("application/json")]
    [Authorize] // Require authentication
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Create a new customer profile (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CustomerResponseDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerService.CreateCustomerAsync(dto);
            if (customer == null)
                return BadRequest(new { message = "Failed to create customer." });

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CustomerListDto>>> GetAllCustomers()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerResponseDto>> GetCustomer(Guid id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            return Ok(customer);
        }

        /// <summary>
        /// Update customer profile (Admin only)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CustomerResponseDto>> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _customerService.CustomerExistsAsync(id))
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var updatedCustomer = await _customerService.UpdateCustomerAsync(id, dto);
            if (updatedCustomer == null)
                return BadRequest(new { message = "Failed to update customer." });

            return Ok(updatedCustomer);
        }

        /// <summary>
        /// Deactivate customer (soft delete) - Admin only
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteCustomer(Guid id)
        {
            if (!await _customerService.CustomerExistsAsync(id))
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var deleted = await _customerService.DeleteCustomerAsync(id);
            if (!deleted)
                return BadRequest(new { message = "Failed to delete customer." });

            return NoContent();
        }

        /// <summary>
        /// Get customer's purchase history
        /// </summary>
        [HttpGet("{id:guid}/sales")]
        [ProducesResponseType(typeof(IEnumerable<SaleListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<SaleListDto>>> GetCustomerSalesHistory(Guid id)
        {
            if (!await _customerService.CustomerExistsAsync(id))
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var salesHistory = await _customerService.GetCustomerSalesHistoryAsync(id);
            return Ok(salesHistory);
        }
    }
}