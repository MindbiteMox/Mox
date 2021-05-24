using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Images
{
    public static class Extensions
    {
        private static Type GetEntityType(this Core.Data.IDbContext context, Type baseType, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException($"Must be set to the full name of a type inherited from {baseType.FullName}", "ImageTypeFullName");
            }

            var type = context.Model.FindEntityType(fullName);
            if (type == null)
            {
                throw new Exception($"{fullName} is not registered in your dbContext. The following types are available: \n\n{string.Join("\n", context.Model.GetEntityTypes().Where(x => x.ClrType != null && baseType.IsAssignableFrom(x.ClrType)).Select(x => x.Name))}");
            }
            return type.ClrType;
        }

        public static Type GetEntityType(this Core.Data.IDbContext context, ViewModels.EditorTemplates.MultiImage viewModel)
        {
            return context.GetEntityType(typeof(Data.Models.Image), viewModel.ImageTypeFullName);
        }

        public static Type GetEntityType(this Core.Data.IDbContext context, ViewModels.EditorTemplates.MultiFile viewModel)
        {
            return context.GetEntityType(typeof(Data.Models.File), viewModel.FileTypeFullName);
        }

        public static Type GetEntityType(this Core.Data.IDbContext context, ViewModels.EditorTemplates.SingleImage viewModel)
        {
            return context.GetEntityType(typeof(Data.Models.Image), viewModel.ImageTypeFullName);
        }
    }
}
