using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace zLib.Data {
	public interface IEntity
	{

		[BsonRepresentation(BsonType.ObjectId)]
		string Id { get; set; }
		[BsonExtraElements]
		BsonDocument ExtraElements { get; set; }
	}
}
