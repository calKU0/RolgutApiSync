using RolgutXmlFromApi.Models;
using System.Collections.Generic;

namespace RolgutXmlFromApi.Helpers
{
    public static class CategoriesHelper
    {
        public static string GetCategoryFullPath(ProductCategory cat, Dictionary<int, ProductCategory> allCategories)
        {
            var path = new List<string>();
            var current = cat;
            while (current != null)
            {
                path.Insert(0, current.Name);
                current = allCategories.ContainsKey(current.ParentID)
                    ? allCategories[current.ParentID]
                    : null;
            }
            return string.Join("/", path);
        }

        public static string GetCategoryFullPath(Application app, Dictionary<int, Application> allApplications)
        {
            var path = new List<string>();
            var current = app;
            while (current != null)
            {
                path.Insert(0, current.Name);
                current = allApplications.ContainsKey(current.ParentID)
                    ? allApplications[current.ParentID]
                    : null;
            }
            return string.Join("/", path);
        }
    }
}