using System;
using System.IO;
using System.Web.Mvc;

namespace ElkRiv.Web.PrivateLabel.Code.Helpers
{
    public static class RenderingHelper
    {
        public static string RenderViewToString(ControllerContext context, string viewName, object model)
        {
            if (String.IsNullOrEmpty(viewName))
                viewName = context.RouteData.GetRequiredString("action");

            var viewData = new ViewDataDictionary(model);

            using (var sw = new StringWriter())
            {
                //Get the view and setup the context
                var viewResult = ViewEngines.Engines.FindPartialView(context, viewName);
                var viewContext = new ViewContext(context, viewResult.View, viewData, new TempDataDictionary(), sw);

                viewContext.ViewBag.StoreFront = context.Controller.ViewBag.StoreFront;

                //Copy the current model state
                foreach (var item in context.Controller.ViewData.ModelState)
                {
                    viewContext.ViewData.ModelState.Add(item);
                }

                //Render the view to a string
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}
