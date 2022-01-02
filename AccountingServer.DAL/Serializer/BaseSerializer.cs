/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AccountingServer.DAL.Serializer;

/// <summary>
///     序列化封装
/// </summary>
/// <typeparam name="T">被序列化的对象类型</typeparam>
internal abstract class BaseSerializer<T> : IBsonSerializer<T>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) =>
        Serialize(context, args, (T)value);

    public Type ValueType => typeof(T);

    public T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context.Reader);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value) =>
        Serialize(context.Writer, value);

    public abstract T Deserialize(IBsonReader bsonReader);

    public abstract void Serialize(IBsonWriter bsonWriter, T voucher);
}

/// <summary>
///     带id的序列化封装
/// </summary>
/// <typeparam name="T">被序列化的对象类型</typeparam>
/// <typeparam name="TId">_id字段存储类型</typeparam>
internal abstract class BaseSerializer<T, TId> : BaseSerializer<T>, IBsonIdProvider, IIdGenerator
{
    public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
    {
        var entity = (T)document;
        id = GetId(entity);
        idNominalType = typeof(TId);
        idGenerator = this;
        return true;
    }

    public void SetDocumentId(object document, object id) => SetId((T)document, (TId)id);

    public object GenerateId(object container, object document)
    {
        var id = MakeId((IMongoCollection<T>)container, (T)document);
        SetId((T)document, id);
        return id;
    }

    public bool IsEmpty(object id) => IsNull((TId)id);


    public abstract TId GetId(T entity);

    protected abstract void SetId(T entity, TId id);

    protected abstract bool IsNull(TId id);

    protected abstract TId MakeId(IMongoCollection<T> container, T entity);

    public bool FillId(IMongoCollection<T> container, T entity)
    {
        if (!IsNull(GetId(entity)))
            return false;

        SetId(entity, MakeId(container, entity));
        return true;
    }
}
