using BaseWebApi.Application.DTOs.Items;
using BaseWebApi.Application.Grpc;
using BaseWebApi.Application.Services;
using ApplicationException = BaseWebApi.Shared.Exceptions.ApplicationException;

namespace BaseWebApi.Api.Grpc.Services;

/// <summary>
/// Code-first gRPC implementation sharing Application DTOs/services with REST.
/// </summary>
public sealed class ItemGrpcService : IItemGrpcService
{
    private readonly IItemAppService _items;

    public ItemGrpcService(IItemAppService items)
    {
        _items = items;
    }

    public async Task<ItemDto> CreateItem(CreateItemRequestDto request)
    {
        var result = await _items.CreateAsync(request);
        return result.IsSuccess
            ? result.Value
            : throw new ApplicationException(result.Error.Message, result.Error.Code);
    }

    public async Task<ItemDto> GetItem(GetItemRequestDto request)
    {
        var result = await _items.GetAsync(request.Id);
        return result.IsSuccess
            ? result.Value
            : throw new ApplicationException(result.Error.Message, result.Error.Code);
    }

    public async Task<ListItemsResponseDto> ListItems(ListItemsRequestDto request)
    {
        var result = await _items.ListAsync(request);
        if (result.IsFailure)
        {
            throw new ApplicationException(result.Error.Message, result.Error.Code);
        }

        return new ListItemsResponseDto
        {
            Items = result.Value.Items.ToList(),
            Page = result.Value.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages
        };
    }

    public async Task<ItemDto> UpdateItem(UpdateItemGrpcRequestDto request)
    {
        var result = await _items.UpdateAsync(request.Id, request.Body);
        return result.IsSuccess
            ? result.Value
            : throw new ApplicationException(result.Error.Message, result.Error.Code);
    }

    public async Task DeleteItem(DeleteItemRequestDto request)
    {
        var result = await _items.DeleteAsync(request.Id);
        if (result.IsFailure)
        {
            throw new ApplicationException(result.Error.Message, result.Error.Code);
        }
    }
}
