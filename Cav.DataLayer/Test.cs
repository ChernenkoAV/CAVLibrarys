using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mfc.CommonTypes.Generic
{
    public class TestRow : DataRow
    {
        internal TestRow(System.Data.DataRowBuilder rb)
            : base(rb)
        {
            this.table = ((TestTable)(this.Table));
        }

        private TestTable table;

        public int? ID
        {
            get
            {
                return this[table.ID] as int?;
            }
            set
            {
                this[table.ID] = (Object)value ?? DBNull.Value;
            }
        }
    }

    public class TestTable : TypedTableBase<TestRow>
    {
        public TestTable()
        {
            this.TableName = "TestTable";
            this.BeginInit();
            this.ID = this.Columns.Add("ID", typeof(int));
            this.EndInit();
        }
        public int Count
        {
            get
            {
                return this.Rows.Count;
            }
        }

        public TestRow this[int index]
        {
            get
            {
                return ((TestRow)(this.Rows[index]));
            }
        }

        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new TestRow(builder);
        }        
        protected override Type GetRowType()
        {
            return typeof(TestRow);
        }

        public DataColumn ID { get; private set; }

        public TestRow NewTestRow()
        {
            return (TestRow)this.NewRow();
        }
        public void AddTestRow(TestRow row)
        {
            this.Rows.Add(row);
        }
    }
}
