using System;
using AccountingServer.BLL;

namespace AccountingServer.Shell.Serializer;

internal class SerializerFactory
{
    private readonly Client m_Client;
    private readonly Identity m_Identity;

    public SerializerFactory(Client client, Identity id)
    {
        m_Client = client;
        m_Identity = id;
    }

    /// <summary>
    ///     从表示器代号寻找表示器
    /// </summary>
    /// <param name="spec">表示器代号</param>
    /// <returns>表示器</returns>
    public IEntitiesSerializer GetSerializer(string spec = null)
    {
        if (string.IsNullOrWhiteSpace(spec))
            return new TrivialEntitiesSerializer(
                AlternativeSerializer.Compose(
                    Create<DiscountSerializer>(),
                    Create<AbbrSerializer>(),
                    Create<CSharpSerializer>()));

        return spec.Initial() switch
            {
                "abbr" => new TrivialEntitiesSerializer(Create<AbbrSerializer>()),
                "csharp" => new TrivialEntitiesSerializer(Create<CSharpSerializer>()),
                "discount" => new TrivialEntitiesSerializer(Create<DiscountSerializer>()),
                "expr" => new TrivialEntitiesSerializer(Create<ExprSerializer>()),
                "json" => new JsonSerializer(),
                "csv" => new CsvSerializer(spec.Rest()),
                _ => throw new ArgumentException("表示器未知", nameof(spec)),
            };
    }

    private T Create<T>() where T : IEntitySerializer, new()
    {
        var serializer = new T();
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (serializer is IClientDependable cd)
            cd.Client = m_Client;
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (serializer is IIdentityDependable id)
            id.Identity = m_Identity;
        return serializer;
    }
}
