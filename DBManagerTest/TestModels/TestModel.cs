using Database.DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManagerTest.TestModels
{
    [Table("DBManagerTest")]
    public class TestModel
    {
        [NotUpdate]
        [PrimaryKey]
        [NotInsert]
        public int Id { get; set; }
        public string StringValue { get; set; }
        public bool BoolValue { get; set; }
        public bool? BoolNullableValue { get; set; }
        public Guid GuidValue { get; set; }
        public Guid? GuidNullableValue { get; set; }
        public DateTime? DateTimeNullableValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public long? LongValue { get; set; }
    }
}
