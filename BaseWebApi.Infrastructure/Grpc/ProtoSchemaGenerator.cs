using BaseWebApi.Application.Grpc;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace BaseWebApi.Infrastructure.Grpc;

/// <summary>
/// Generates .proto schemas from code-first protobuf-net contracts at build/runtime.
/// Used by MSBuild target GenerateProtos and optional startup export.
/// </summary>
public static class ProtoSchemaGenerator
{
    public static string GenerateItemServiceProto()
    {
        var generator = new SchemaGenerator
        {
            ProtoSyntax = ProtoSyntax.Proto3
        };

        return generator.GetSchema<IItemGrpcService>();
    }

    public static void WriteProtos(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var proto = GenerateItemServiceProto();
        var path = Path.Combine(outputDirectory, "item_service.proto");
        File.WriteAllText(path, proto);

        // TODO: Register additional [Service] interfaces here as modules are added.
    }
}
