using System;

namespace ItelexNewsServer.Data
{
	class SqlIdAttribute : Attribute
	{
	}

	class SqlStringAttribute : Attribute
	{
		public int Length { get; set; }
	}

	class SqlUInt64StrAttribute : Attribute
	{
	}

	/// <summary>
	/// 32 bit integer
	/// </summary>
	class SqlIntAttribute : Attribute
	{
	}

	/// <summary>
	/// 16 bit integer
	/// </summary>
	class SqlSmallIntAttribute : Attribute
	{
	}

	/// <summary>
	/// 8 bit integer
	/// </summary>
	class SqlTinyIntAttribute : Attribute
	{
	}

	class SqlBoolAttribute : Attribute
	{
	}

	class SqlDateAttribute : Attribute
	{
	}

	class SqlMemoAttribute : Attribute
	{
	}

}
