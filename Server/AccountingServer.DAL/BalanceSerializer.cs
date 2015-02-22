using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class BalanceSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options)
        {
            return Deserialize(bsonReader);
        }

        public static Balance Deserialize(BsonReader bsonReader)
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
            balance.Fund = bsonReader.ReadDouble("value", ref read).Value;
            bsonReader.ReadEndDocument();

            return balance;
        }
    }
}
