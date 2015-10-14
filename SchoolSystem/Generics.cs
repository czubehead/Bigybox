using System.Collections.Generic;

namespace BakalariAPI
{
	/// <summary>
	/// Special collection for storing classes that derive from SubjectCount
	/// All children s' parents will be set to hosting subject
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SubjectChildren<T> : List<T> where T : SubjectChild
	{
		/// <summary>
		/// Subject which are contained grades related to.
		/// </summary>
		public int ParentId { get; set; }

		public SubjectChildren(int parentId)
		{
			ParentId = parentId;
		}
		public SubjectChildren(Subject parent)
		{
			ParentId = parent.Id;
		}
		#region functions

		public new void Add(T item)
		{
			if (item != null)
			{
				item.ParentId = ParentId;
				base.Add(item);
			}
		}

		public void Add(params T[] items)
		{
			foreach (var item in items)
			{
				Add(item);
			}
		}
		#endregion
	}

	/// <summary>
	/// base for classes that are children of Subject.
	/// Storing in SubjectChildred highly recommented
	/// </summary>
	public class SubjectChild
	{
		public int ParentId { get; set; }
	}
}
