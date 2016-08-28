using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     余额表条目反序列化器（从MapReduce的结果中反序列化）
    /// </summary>
    internal class BalanceSerializer : IBsonSerializer<Balance>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            throw new InvalidOperationException();
        }

        public Type ValueType => typeof(Balance);

        public Balance Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context.Reader);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Balance value)
        {
            throw new InvalidOperationException();
        }

        public static Balance Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var balance = bsonReader.ReadDocument(
                                                  "_id",
                                                  ref read,
                                                  bR =>
                                                  {
                                                      bR.ReadStartDocument();
                                                      var bal = new Balance
                                                                    {
                                                                        Date = bR.ReadDateTime("date", ref read),
                                                                        Title = (int?)bR.ReadDouble("title", ref read),
                                                                        SubTitle =
                                                                            (int?)bR.ReadDouble("subtitle", ref read),
                                                                        Content = bR.ReadString("content", ref read),
                                                                        Remark = bR.ReadString("remark", ref read)
                                                                    };
                                                      bR.ReadEndDocument();
                                                      return bal;
                                                  });
            // ReSharper disable once PossibleInvalidOperationException
            balance.Fund = bsonReader.ReadDouble("value", ref read).Value;
            bsonReader.ReadEndDocument();

            return balance;
        }
    }
}
