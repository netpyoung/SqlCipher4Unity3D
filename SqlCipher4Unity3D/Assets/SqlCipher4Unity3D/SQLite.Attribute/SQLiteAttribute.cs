using System;

namespace SQLite.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
	public class TableAttribute : Attribute
	{
		public string Name { get; set; }

		/// <summary>
		/// Flag whether to create the table without rowid (see https://sqlite.org/withoutrowid.html)
		///
		/// The default is <c>false</c> so that sqlite adds an implicit <c>rowid</c> to every table created.
		/// </summary>
		public bool WithoutRowId { get; set; }

		public TableAttribute(string name)
		{
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class ColumnAttribute : Attribute
	{
		public string Name { get; set; }

		public ColumnAttribute(string name)
		{
			Name = name;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class AutoIncrementAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class IndexedAttribute : Attribute
	{
		public string Name { get; set; }
		public int Order { get; set; }
		public virtual bool Unique { get; set; }

		public IndexedAttribute()
		{
		}

		public IndexedAttribute(string name, int order)
		{
			Name = name;
			Order = order;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class IgnoreAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class UniqueAttribute : IndexedAttribute
	{
		public override bool Unique
		{
			get { return true; }
			set { /* throw?  */ }
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class MaxLengthAttribute : Attribute
	{
		public int Value { get; private set; }

		public MaxLengthAttribute(int length)
		{
			Value = length;
		}
	}

	public sealed class PreserveAttribute : System.Attribute
	{
		public bool AllMembers;
		public bool Conditional;
	}

	/// <summary>
	/// Select the collating sequence to use on a column.
	/// "BINARY", "NOCASE", and "RTRIM" are supported.
	/// "BINARY" is the default.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class CollationAttribute : Attribute
	{
		public string Value { get; private set; }

		public CollationAttribute(string collation)
		{
			Value = collation;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Enum)]
	public class StoreAsTextAttribute : Attribute
	{
	}

}