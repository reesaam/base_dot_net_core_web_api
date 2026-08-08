using BaseWebApi.Application.DTOs.Items;
using BaseWebApi.Application.Services;
using BaseWebApi.Shared.Helpers;
using BaseWebApi.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseWebApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ItemsController : ControllerBase
{
    private readonly IItemAppService _items;

    public ItemsController(IItemAppService items)
    {
        _items = items;
    }

    /// <summary>GET /api/items</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginationResult<ItemDto>>> GetItems(
        [FromQuery] ListItemsRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _items.ListAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>GET /api/items/{id}</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ItemDto>> GetItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _items.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>POST /api/items</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ItemDto>> CreateItem(
        [FromBody] CreateItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _items.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return MapError(result.Error);
        }

        return CreatedAtAction(nameof(GetItem), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>POST /api/items/upload — multipart/form-data (id + file)</summary>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ItemDto>> Upload(
        [FromForm] Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        FileHelpers.ValidateUpload(file);
        await using var stream = file.OpenReadStream();
        var result = await _items.UploadAttachmentAsync(
            id,
            file.FileName,
            file.ContentType,
            stream,
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>PUT /api/items/{id}</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ItemDto>> UpdateItem(
        Guid id,
        [FromBody] UpdateItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _items.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
    }

    /// <summary>DELETE /api/items/{id}</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _items.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : MapError(result.Error);
    }

    private ActionResult MapError(Error error) =>
        error.Code switch
        {
            "not_found" => NotFound(error),
            "validation.error" => BadRequest(error),
            "unauthorized" => Unauthorized(error),
            "forbidden" => StatusCode(StatusCodes.Status403Forbidden, error),
            "conflict" => Conflict(error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, error)
        };
}
