using BaseWebApi.Application.DTOs.Items;
using ProtoBuf;
using ProtoBuf.Grpc.Configuration;

namespace BaseWebApi.Application.Grpc;

/// <summary>
/// Code-first gRPC contract. .proto files are auto-generated via SchemaGenerator + protobuf-net.BuildTools.
/// </summary>
[Service]
public interface IItemGrpcService
{
    Task<ItemDto> CreateItem(CreateItemRequestDto request);

    Task<ItemDto> GetItem(GetItemRequestDto request);

    Task<ListItemsResponseDto> ListItems(ListItemsRequestDto request);

    Task<ItemDto> UpdateItem(UpdateItemGrpcRequestDto request);

    Task DeleteItem(DeleteItemRequestDto request);
}

[ProtoContract]
public sealed record UpdateItemGrpcRequestDto
{
    [ProtoMember(1)]
    public Guid Id { get; init; }

    [ProtoMember(2, IsRequired = true)]
    public UpdateItemRequestDto Body { get; init; } = new();
}
