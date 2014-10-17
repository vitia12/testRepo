using System.Collections.Generic;
using System.Web.Mvc;

namespace ElkRiv.Web.PrivateLabel.Code.Helpers
{
	public class SelectListHelper
	{
		public static void InsertSelectOption(IList<SelectListItem> list)
		{
			list.Insert(0, new SelectListItem { Text = "--Select--", Value = "0" });
		}

		public static void InsertSelectOption(IList<SelectListItem> list, string optionText)
		{
			list.Isert(0, new SelectListItem { Text = optionText, Value = "0" });
		}

	}}
}
dfgdfsgd